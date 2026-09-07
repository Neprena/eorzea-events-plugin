using EorzeaEventsPlugin.Api;

namespace EorzeaEventsPlugin;

/// <summary>
/// Registre local des personnages croisés, à la manière du carnet de Total RP 3.
///
/// Trois déclencheurs. Deux sont bornés au relevé des disponibilités, donc à
/// une fiche visible : l'aperçu à portée de vue (la plaque de nom passe dans la
/// table d'objets) et le geste « Note privée » du menu contextuel. Le troisième
/// est borné à ce que le serveur a réellement servi : l'ouverture d'une fiche,
/// d'où qu'elle vienne (liste, amis, auteur de session), n'est retenue qu'une
/// fois la fiche reçue, jamais sur un refus. Recevoir une réplique dans le chat
/// n'est pas voir quelqu'un : la substitution de nom n'enregistre rien. Et l'on
/// ne se rencontre pas soi-même : ses propres personnages sont exclus partout.
///
/// Tout vit dans la configuration Dalamud et n'en sort jamais. L'écriture sur
/// disque est différée : les aperçus arrivent à chaque frame, et sauvegarder
/// la configuration à ce rythme userait le disque pour rien.
///
/// Toutes les méthodes s'appellent depuis le thread de jeu (plaques de nom,
/// interface, boucle de mise à jour) : aucun verrou n'est nécessaire.
/// </summary>
internal static class EncounterRegistry
{
    public const int MaxEntries = 300;

    private static readonly TimeSpan SightingThrottle = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan VisitGap         = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SaveInterval     = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Dernier aperçu par identifiant, en mémoire seulement. Tenu même quand le
    /// registre est coupé : « Autour de moi » s'en sert pour savoir qui est
    /// réellement à portée, indépendamment de ce qu'on mémorise.
    /// </summary>
    private static readonly Dictionary<string, DateTime> _lastSightingAt = [];

    private static bool     _dirty;
    private static DateTime _lastSaveAt = DateTime.MinValue;

    /// <summary>Le personnage a-t-il été aperçu à portée de vue dans la fenêtre donnée ?</summary>
    public static bool SightedWithin(string? characterId, TimeSpan window) =>
        characterId is { Length: > 0 } id
        && _lastSightingAt.TryGetValue(id, out var at)
        && DateTime.UtcNow - at <= window;

    /// <summary>
    /// Le personnage est-il l'un des miens ? Un registre de rencontres ne se
    /// rencontre pas soi-même : ni en croisant sa propre plaque, ni en ouvrant
    /// sa propre fiche depuis « Autour de moi » ou une session, ni par le menu
    /// contextuel sur soi. Deux repères, parce qu'aucun n'est complet seul :
    /// l'identifiant serveur, connu dès qu'une fiche a été synchronisée, et le
    /// couple nom + monde des personnages liés.
    /// </summary>
    public static bool IsMine(string characterId, string name, string world)
    {
        var config = Plugin.Config;

        foreach (var cached in config.RpProfiles.Values)
        {
            if (cached.CharacterId is { Length: > 0 } id
                && string.Equals(id, characterId, StringComparison.Ordinal))
                return true;
        }

        foreach (var token in config.CharacterTokens)
        {
            if (string.Equals(token.CharacterName, name, StringComparison.Ordinal)
                && string.Equals(token.WorldName, world, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Un joueur du relevé vient de passer dans la table d'objets.
    ///
    /// Limité à un traitement par personnage et par minute : la plaque de nom
    /// repasse ici à chaque frame.
    /// </summary>
    public static void RecordSighting(RpAvailabilityEntryDto entry, string? myZone)
    {
        if (entry.Profile?.CharacterId is not { Length: > 0 } id) return;
        if (IsMine(id, entry.CharacterName, entry.Server)) return;

        var now = DateTime.UtcNow;
        if (_lastSightingAt.TryGetValue(id, out var last) && now - last < SightingThrottle) return;
        _lastSightingAt[id] = now;

        if (!Plugin.Config.EncountersEnabled) return;

        var e = GetOrCreate(id, entry.CharacterName, entry.Server, now);
        Refresh(e, entry);

        // Une visite est une présence continue : trente minutes sans aperçu, et
        // le prochain compte pour une nouvelle rencontre.
        if (e.LastSeenAt is not { } seen || now - seen > VisitGap) e.Visits++;

        e.LastSeenAt = now;
        if (myZone is { Length: > 0 }) e.LastZone = myZone;

        _dirty = true;
    }

    /// <summary>
    /// Geste « Note privée » du menu contextuel sur un personnage du relevé :
    /// l'entrée du registre est créée si elle manque, pour que la page ait
    /// quelque chose à annoter. Aucune fiche n'est ouverte ici, donc pas de
    /// « fiche consultée » ; si le personnage a été aperçu à portée de vue dans
    /// les cinq dernières minutes, c'est un aperçu qui est daté, sinon la seule
    /// date connue reste celle de la rencontre.
    /// </summary>
    public static void RecordNoteGesture(string characterId, string name, string world,
                                         RpAvailabilityEntryDto entry)
    {
        if (entry.Profile?.CharacterId is not { Length: > 0 } id
            || !string.Equals(id, characterId, StringComparison.Ordinal)) return;
        if (IsMine(characterId, name, world)) return;
        if (!Plugin.Config.EncountersEnabled) return;

        var now = DateTime.UtcNow;
        var e   = GetOrCreate(characterId, name, world, now);
        Refresh(e, entry);

        if (SightedWithin(characterId, TimeSpan.FromMinutes(5)))
        {
            e.LastSeenAt = now;
            if (Plugin.CurrentZone is { Length: > 0 } zone) e.LastZone = zone;
        }

        _dirty = true;
        FlushIfDue(now, force: true);
    }

    /// <summary>
    /// La fiche d'un personnage vient d'être servie et affichée, d'où que ce soit.
    ///
    /// Appelée après la réponse du serveur et non à l'ouverture de la fenêtre :
    /// une fiche refusée (privée, personnage inconnu) n'est pas une rencontre,
    /// et le registre ne retient que ce qu'on a réellement vu.
    /// </summary>
    public static void RecordProfileOpened(string characterId, string name, string world, RpProfileDto profile)
    {
        if (!Plugin.Config.EncountersEnabled) return;
        if (string.IsNullOrEmpty(characterId)) return;
        if (IsMine(characterId, name, world)) return;

        var now = DateTime.UtcNow;
        var e   = GetOrCreate(characterId, name, world, now);
        Refresh(e, profile);
        e.LastOpenedAt = now;

        _dirty = true;
        FlushIfDue(now, force: true);
    }

    public static RpEncounter? Get(string? characterId) =>
        characterId is { Length: > 0 } id && Plugin.Config.Encounters.TryGetValue(id, out var e)
            ? e
            : null;

    /// <summary>Ma note privée sur ce personnage, ou null.</summary>
    public static string? NoteFor(string? characterId) =>
        Get(characterId)?.Note is { Length: > 0 } note ? note : null;

    public static void SetNote(string characterId, string? note)
    {
        if (Get(characterId) is not { } e) return;

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        e.Note          = trimmed;
        e.NoteUpdatedAt = trimmed == null ? null : DateTime.UtcNow;

        _dirty = true;
        FlushIfDue(DateTime.UtcNow, force: true);
    }

    public static void Forget(string characterId)
    {
        if (!Plugin.Config.Encounters.Remove(characterId)) return;
        _dirty = true;
        FlushIfDue(DateTime.UtcNow, force: true);
    }

    public static void Clear()
    {
        if (Plugin.Config.Encounters.Count == 0) return;
        Plugin.Config.Encounters.Clear();
        _dirty = true;
        FlushIfDue(DateTime.UtcNow, force: true);
    }

    /// <summary>Clé de tri : la plus récente des dates connues.</summary>
    public static DateTime SortKey(RpEncounter e)
    {
        var key = e.FirstMetAt;
        if (e.LastSeenAt   is { } seen   && seen   > key) key = seen;
        if (e.LastOpenedAt is { } opened && opened > key) key = opened;
        return key;
    }

    /// <summary>
    /// Écrit la configuration si quelque chose a changé, au plus une fois par
    /// minute, ou tout de suite quand on l'exige (geste du joueur, déconnexion,
    /// déchargement du plugin).
    /// </summary>
    public static void FlushIfDue(DateTime now, bool force = false)
    {
        if (!_dirty) return;
        if (!force && now - _lastSaveAt < SaveInterval) return;

        _dirty      = false;
        _lastSaveAt = now;
        Plugin.Config.Save();
    }

    private static RpEncounter GetOrCreate(string characterId, string name, string world, DateTime now)
    {
        var encounters = Plugin.Config.Encounters;
        if (encounters.TryGetValue(characterId, out var existing)) return existing;

        var created = new RpEncounter
        {
            CharacterId = characterId,
            Name        = name,
            World       = world,
            FirstMetAt  = now,
        };
        encounters[characterId] = created;
        Evict(encounters);
        return created;
    }

    private static void Refresh(RpEncounter e, RpAvailabilityEntryDto entry)
    {
        e.Name  = entry.CharacterName;
        e.World = entry.Server;
        if (entry.Profile is { } p) Refresh(e, p);
    }

    /// <summary>
    /// Reporte ce que le relevé sait de plus récent. Le portrait n'est repris
    /// que s'il est renseigné : le relevé allégé, fenêtre fermée, ne le
    /// transporte pas, et ne doit pas effacer une valeur connue.
    /// </summary>
    private static void Refresh(RpEncounter e, RpProfileDto p)
    {
        // Une fiche retenue arrive vidée : elle ne doit pas effacer ce qu'on savait.
        if (p.NsfwWithheld) return;

        e.RpName      = p.RpName?.Trim() is { Length: > 0 } rp ? rp : null;
        e.AccentColor = p.AccentColor;
        if (p.PortraitUrl is { Length: > 0 } portrait) e.PortraitUrl = portrait;
    }

    /// <summary>
    /// Au-delà du plafond, les entrées sans note partent par ancienneté. Une
    /// entrée annotée n'est jamais évincée : c'est ce que le joueur a écrit.
    /// </summary>
    private static void Evict(Dictionary<string, RpEncounter> encounters)
    {
        if (encounters.Count <= MaxEntries) return;

        var removable = encounters.Values
            .Where(e => string.IsNullOrEmpty(e.Note))
            .OrderBy(SortKey)
            .Take(encounters.Count - MaxEntries)
            .Select(e => e.CharacterId)
            .ToList();

        foreach (var id in removable) encounters.Remove(id);
    }
}

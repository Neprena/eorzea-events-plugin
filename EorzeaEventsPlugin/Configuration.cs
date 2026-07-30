using Dalamud.Configuration;

namespace EorzeaEventsPlugin;

public enum PluginLanguage { Auto, French, English }

/// <summary>
/// Token API lié à un personnage FFXIV spécifique (workflow web-link).
/// Le token est de la forme `ec_*`. Le plugin sélectionne automatiquement
/// le bon token selon le perso connecté in-game.
/// </summary>
[Serializable]
public class CharacterTokenEntry
{
    public string CharacterName { get; set; } = string.Empty;
    public int    WorldId       { get; set; }
    public string WorldName     { get; set; } = string.Empty;
    public string Token         { get; set; } = string.Empty;
    public DateTime LinkedAt    { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ContentId Dalamud du personnage (identité stable, survit au rename).
    /// 0 = entrée legacy liée avant cette fonctionnalité (backfillée à la volée).
    /// </summary>
    public ulong ContentId { get; set; }
}

/// <summary>
/// Copie locale de la fiche RP d'un personnage.
///
/// Sert à afficher la fiche sans attendre le réseau, et à la renvoyer telle
/// quelle quand seul un champ change. Les listes sont stockées en JSON, comme
/// côté serveur, pour éviter une conversion à chaque échange.
/// </summary>
[Serializable]
public class RpProfileCache
{
    public string  RpLevel       { get; set; } = "casual";
    public string  ApproachMode  { get; set; } = "come_to_me";
    public string? ContactMode   { get; set; }
    public string? SessionLength { get; set; }
    public string  Languages     { get; set; } = "[\"fr\"]";
    public string? Themes        { get; set; }
    public string? AvoidThemes   { get; set; }

    public string? RpName       { get; set; }
    public string? Nickname     { get; set; }
    public string? Pronouns     { get; set; }
    public string? Race         { get; set; }
    public string? Age          { get; set; }
    public string? Origin       { get; set; }
    public string? Occupation   { get; set; }
    public string? Appearance   { get; set; }
    public string? Personality  { get; set; }
    public string? Background   { get; set; }
    public string? Hooks        { get; set; }
    public string? CurrentQuest { get; set; }
    public string? Limits       { get; set; }
    public string? Availability { get; set; }
    public string? ExternalUrl  { get; set; }

    public string? PortraitUrl { get; set; }
    public string? Height      { get; set; }
    public string? Build       { get; set; }
    public string? Marks       { get; set; }
    public string? Voice       { get; set; }
    public string? FreeCompany { get; set; }
    public string? Allegiance  { get; set; }
    public string? Deity       { get; set; }
    public string? Quote       { get; set; }

    public bool Nsfw     { get; set; }
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Reste de la confidentialité, mis en cache comme le reste de la fiche.
    ///
    /// Ces trois-là manquaient, si bien qu'une fiche reconstituée depuis le cache
    /// affichait « page web active, indexation coupée, sections aux défauts »
    /// quels qu'aient été les réglages réels. L'écran mentait, et un
    /// enregistrement dans cet état les aurait imposés au serveur. Le null
    /// distingue « jamais synchronisé » de « réglé ainsi ».
    /// </summary>
    public bool?   WebPageEnabled    { get; set; }
    public bool?   SearchIndexable   { get; set; }
    public string? SectionVisibility { get; set; }

    /// <summary>Identifiant serveur du personnage, pour l'aperçu et les liens.</summary>
    public string? CharacterId { get; set; }

    public string? ThemeSongUrl { get; set; }

    /// <summary>Dernière synchronisation réussie avec le serveur.</summary>
    public DateTime FetchedAt { get; set; } = DateTime.MinValue;
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 4;

    /// <summary>
    /// Token API legacy (User.apiToken, préfixe ee_). Conservé pour la transition.
    /// Sera retiré dans une release future une fois tous les utilisateurs migrés.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Tokens API par personnage (préfixe ec_). Le plugin sélectionne automatiquement
    /// l'entrée correspondant au personnage actuellement connecté in-game.
    /// </summary>
    public List<CharacterTokenEntry> CharacterTokens { get; set; } = [];

    /// <summary>
    /// Trouve le token attaché au personnage donné (clé naturelle name+worldId).
    /// Retourne null si aucun token n'est lié.
    /// </summary>
    public CharacterTokenEntry? FindCharacterToken(string characterName, int worldId)
    {
        return CharacterTokens.Find(c =>
            string.Equals(c.CharacterName, characterName, StringComparison.Ordinal)
            && c.WorldId == worldId);
    }

    /// <summary>
    /// Trouve le token par ContentId (identité stable qui survit au rename).
    /// Retourne null si contentId vaut 0 ou si aucune entrée ne correspond.
    /// </summary>
    public CharacterTokenEntry? FindCharacterTokenByContentId(ulong contentId)
    {
        if (contentId == 0) return null;
        return CharacterTokens.Find(c => c.ContentId == contentId);
    }

    /// <summary>
    /// Sélectionne le meilleur token disponible pour le perso donné :
    /// 1. Token de personnage si lié.
    /// 2. Sinon, token legacy (ApiToken).
    /// 3. Sinon, chaîne vide.
    /// </summary>
    public string ResolveTokenForCharacter(string? characterName, int? worldId)
    {
        if (!string.IsNullOrEmpty(characterName) && worldId.HasValue)
        {
            var entry = FindCharacterToken(characterName, worldId.Value);
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Token))
                return entry.Token;
        }
        return ApiToken ?? string.Empty;
    }

    /// <summary>Identifiant anonyme unique généré automatiquement pour les heartbeats de présence.</summary>
    public string ClientId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// URL de base de l'API (sans slash final).
    /// La valeur par défaut dépend du build : Release → prod, Debug → localhost.
    /// Toujours modifiable via Config → Dev section (DEBUG uniquement).
    /// </summary>
    public string BaseUrl { get; set; } =
#if DEBUG
        "http://localhost:3000";
#else
        "https://eorzea.events";
#endif

    /// <summary>ID de la session RP en cours (null si aucune).</summary>
    public string? ActiveSessionId { get; set; }

    /// <summary>Alerte écran (toast natif FFXIV) pour les nouvelles sessions RP Live.</summary>
    public bool NotifyRpLiveScreen { get; set; } = true;

    /// <summary>Notifier quand une nouvelle session RP Live démarre (toast Dalamud, coin de l'écran).</summary>
    public bool NotifyRpLive { get; set; } = true;

    /// <summary>Annoncer les nouvelles sessions RP dans le chat du jeu.</summary>
    public bool NotifyRpLiveChat { get; set; } = true;

    /// <summary>Limiter les notifications au monde courant du joueur.</summary>
    public bool NotifyMyWorld { get; set; } = true;

    /// <summary>Limiter les notifications de RP ouverts à la langue du plugin.</summary>
    public bool NotifyRpLanguageFilter { get; set; } = true;

    /// <summary>Proposer de mettre à jour l'emplacement après un changement de zone ou un TP.</summary>
    public bool AlertOnZoneChange { get; set; } = true;

    /// <summary>Proposer de terminer la session quand le tag RP est retiré.</summary>
    public bool AlertOnRpTagRemoved { get; set; } = true;

    /// <summary>Proposer de prolonger ou d'arrêter la session quand elle est sur le point d'expirer.</summary>
    public bool AlertOnSessionExpiring { get; set; } = true;

    /// <summary>Mettre à jour automatiquement la position de la session RP active toutes les 5 min (sans propager au fil Discord).</summary>
    public bool AutoRefreshPosition { get; set; } = true;

    /// <summary>Proposer de démarrer une session quand le tag RP est activé sans session en cours.</summary>
    public bool SuggestSessionOnRpTag { get; set; } = true;

    /// <summary>Notifier (toast) quand une nouvelle session RP démarre dans la zone courante du joueur.</summary>
    public bool NotifyNearbyZone { get; set; } = true;

    /// <summary>Notifier quand un événement communautaire démarre via notification Dalamud.</summary>
    public bool NotifyEventStartDalamud { get; set; } = true;

    /// <summary>Notifier quand un événement communautaire démarre via message chat.</summary>
    public bool NotifyEventStartChat { get; set; } = true;

    /// <summary>Afficher l'entrée "RP" dans la barre de statut du serveur.</summary>
    public bool ShowDtrRp { get; set; } = true;

    /// <summary>Afficher l'entrée "Events" dans la barre de statut du serveur.</summary>
    public bool ShowDtrEvents { get; set; } = true;

    /// <summary>Afficher l'entrée de disponibilité RP (♦) dans la barre de statut du serveur.</summary>
    public bool ShowDtrRpAvail { get; set; } = true;

    /// <summary>Langue de l'interface du plugin (Auto = détection depuis le client FFXIV).</summary>
    public PluginLanguage Language { get; set; } = PluginLanguage.Auto;

    /// <summary>
    /// Proposer le bouton « Y aller » quand Lifestream est installé. Reste sans
    /// effet en son absence : le bouton ne s'affiche pas pour autant.
    /// </summary>
    public bool EnableLifestreamTravel { get; set; } = true;

    /// <summary>IDs d'événements masqués localement.</summary>
    public List<string> HiddenEventIds { get; set; } = [];

    /// <summary>IDs d'établissements masqués localement.</summary>
    public List<string> HiddenEstablishmentIds { get; set; } = [];

    // ─── Profil RP ───────────────────────────────────────────────────────────

    /// <summary>True si l'utilisateur a vu ou ignoré le wizard de migration vers les tokens de personnage.</summary>
    public bool MigrationNoticeSeen { get; set; } = false;

    /// <summary>L'annonce de la fonctionnalité "Profil RP & Disponibilité" a été vue.</summary>
    public bool RpAnnouncementSeen { get; set; } = false;

    /// <summary>Le wizard de profil RP a été complété au moins une fois.</summary>
    public bool RpProfileSetupDone { get; set; } = false;

    /// <summary>Afficher le marqueur ♦ sur les nameplates des joueurs disponibles pour du RP.</summary>
    public bool ShowRpAvailableIndicator { get; set; } = true;

    // ─── Nouveautés ──────────────────────────────────────────────────────────

    /// <summary>
    /// Dernière version du plugin dont les nouveautés ont été affichées.
    /// null = jamais, ce qui vaut pour toute installation antérieure à cette
    /// fonctionnalité : la fenêtre s'ouvrira donc une fois après la mise à jour.
    /// </summary>
    public string? LastSeenVersion { get; set; }

    /// <summary>Ouvrir la fenêtre des nouveautés au premier lancement après une mise à jour.</summary>
    public bool AutoOpenWhatsNew { get; set; } = true;

    // ─── Ancien état, commun à tout le compte ────────────────────────────────
    //
    // Conservé pour la migration vers la version 4. Les nouveaux écrans passent
    // par les dictionnaires par personnage ci-dessous.

    public string? RpProfileLevel         { get; set; }
    public string? RpProfileApproachMode  { get; set; }
    public string? RpProfileLanguages     { get; set; }
    public string? RpProfileContactMode   { get; set; }
    public string? RpProfileSessionLength { get; set; }
    public string? RpProfileThemes        { get; set; }
    public bool    RpAvailabilityActive   { get; set; } = false;

    // ─── État par personnage ──────────────────────────────────────────────────
    //
    // Un rôliste joue plusieurs personnages : leur fiche et leur disponibilité
    // sont distinctes. La clé est « Nom@MondeId ».

    /// <summary>Cache de fiche, pour éviter un appel réseau à l'ouverture.</summary>
    public Dictionary<string, RpProfileCache> RpProfiles { get; set; } = [];

    /// <summary>Disponibilité déclarée, par personnage.</summary>
    public Dictionary<string, bool> RpAvailability { get; set; } = [];

    /// <summary>Clé d'un personnage dans les dictionnaires ci-dessus.</summary>
    public static string CharacterKey(string name, int worldId) => $"{name}@{worldId}";

    /// <summary>Fiche du personnage donné, ou null si elle n'est pas connue.</summary>
    public RpProfileCache? FindProfile(string? name, int? worldId)
    {
        if (string.IsNullOrEmpty(name) || worldId is not { } world) return null;
        return RpProfiles.TryGetValue(CharacterKey(name, world), out var profile) ? profile : null;
    }

    /// <summary>Disponibilité du personnage donné.</summary>
    public bool IsAvailable(string? name, int? worldId)
    {
        if (string.IsNullOrEmpty(name) || worldId is not { } world) return false;
        return RpAvailability.TryGetValue(CharacterKey(name, world), out var value) && value;
    }

    public void SetAvailable(string name, int worldId, bool available)
    {
        RpAvailability[CharacterKey(name, worldId)] = available;
        Save();
    }

    /// <summary>Proposer de se mettre indisponible à chaque reconnexion si disponible.</summary>
    public bool RpAskOnLogin { get; set; } = false;

    /// <summary>
    /// Reporte l'ancien état commun au compte sur chaque personnage lié.
    ///
    /// Sans cela, un joueur qui met le plugin à jour verrait sa fiche et sa
    /// disponibilité repartir de zéro. La migration reprend le raisonnement de
    /// la migration serveur : ce que l'on avait pour le compte valait pour tous
    /// ses personnages.
    /// </summary>
    public void MigrateToPerCharacter()
    {
        if (Version >= 4) return;

        var hadProfile = !string.IsNullOrEmpty(RpProfileLevel)
                         && !string.IsNullOrEmpty(RpProfileApproachMode);

        foreach (var character in CharacterTokens)
        {
            var key = CharacterKey(character.CharacterName, character.WorldId);

            if (hadProfile && !RpProfiles.ContainsKey(key))
            {
                RpProfiles[key] = new RpProfileCache
                {
                    RpLevel       = RpProfileLevel!,
                    ApproachMode  = RpProfileApproachMode!,
                    ContactMode   = RpProfileContactMode,
                    SessionLength = RpProfileSessionLength,
                    Languages     = RpProfileLanguages ?? "[\"fr\"]",
                    Themes        = RpProfileThemes,
                };
            }

            if (RpAvailabilityActive && !RpAvailability.ContainsKey(key))
                RpAvailability[key] = true;
        }

        Version = 4;
        Save();
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

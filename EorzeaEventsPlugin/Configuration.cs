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
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

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

    // Cache local du profil (évite un appel API au démarrage)
    public string? RpProfileLevel         { get; set; }
    public string? RpProfileApproachMode  { get; set; }
    public string? RpProfileLanguages     { get; set; }
    public string? RpProfileContactMode   { get; set; }
    public string? RpProfileSessionLength { get; set; }
    public string? RpProfileThemes        { get; set; }

    // État de la disponibilité locale (permanent, sans expiration)
    public bool RpAvailabilityActive { get; set; } = false;

    /// <summary>Proposer de se mettre indisponible à chaque reconnexion si disponible.</summary>
    public bool RpAskOnLogin { get; set; } = false;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

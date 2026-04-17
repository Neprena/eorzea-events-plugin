using Dalamud.Configuration;

namespace EorzeaEventsPlugin;

public enum PluginLanguage { Auto, French, English }

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    /// <summary>Token API généré depuis le dashboard Eorzea Events.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Identifiant anonyme unique généré automatiquement pour les heartbeats de présence.</summary>
    public string ClientId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>URL de base de l'API (sans slash final).</summary>
    public string BaseUrl { get; set; } = "https://eorzea.events";

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

    /// <summary>Limiter les notifications de RP sauvages à la langue du plugin.</summary>
    public bool NotifyRpLanguageFilter { get; set; } = true;

    /// <summary>Proposer de mettre à jour l'emplacement après un changement de zone ou un TP.</summary>
    public bool AlertOnZoneChange { get; set; } = true;

    /// <summary>Proposer de terminer la session quand le tag RP est retiré.</summary>
    public bool AlertOnRpTagRemoved { get; set; } = true;

    /// <summary>Proposer de prolonger ou d'arrêter la session quand elle est sur le point d'expirer.</summary>
    public bool AlertOnSessionExpiring { get; set; } = true;

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

    /// <summary>Langue de l'interface du plugin (Auto = détection depuis le client FFXIV).</summary>
    public PluginLanguage Language { get; set; } = PluginLanguage.Auto;

    /// <summary>IDs d'événements masqués localement.</summary>
    public List<string> HiddenEventIds { get; set; } = [];

    /// <summary>IDs d'établissements masqués localement.</summary>
    public List<string> HiddenEstablishmentIds { get; set; } = [];

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

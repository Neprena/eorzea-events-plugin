using Dalamud.Configuration;

namespace EorzeaEventsPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Token API généré depuis le dashboard Eorzea Events.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>URL de base de l'API (sans slash final).</summary>
    public string BaseUrl { get; set; } = "https://eorzea.events";

    /// <summary>ID de la session RP en cours (null si aucune).</summary>
    public string? ActiveSessionId { get; set; }

    /// <summary>Notifier quand une nouvelle session RP Live démarre (toast).</summary>
    public bool NotifyRpLive  { get; set; } = true;

    /// <summary>Annoncer les nouvelles sessions RP dans le chat du jeu.</summary>
    public bool NotifyRpLiveChat { get; set; } = true;

    /// <summary>Limiter les notifications au monde courant du joueur.</summary>
    public bool NotifyMyWorld { get; set; } = true;

    /// <summary>Proposer de mettre à jour l'emplacement après un changement de zone ou un TP.</summary>
    public bool AlertOnZoneChange { get; set; } = true;

    /// <summary>Proposer de terminer la session quand le tag RP est retiré.</summary>
    public bool AlertOnRpTagRemoved { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

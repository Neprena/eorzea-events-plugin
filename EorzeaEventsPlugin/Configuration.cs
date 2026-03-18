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

    /// <summary>Notifier quand une nouvelle session RP Live démarre.</summary>
    public bool NotifyRpLive  { get; set; } = true;

    /// <summary>Limiter les notifications au monde courant du joueur.</summary>
    public bool NotifyMyWorld { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

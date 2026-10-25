using Dalamud.Interface;
using EorzeaEventsPlugin.Api;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Ce que le plugin dit quand le site ne répond plus, partout dans les mêmes
/// termes.
///
/// Chaque écran réagissait à sa façon à une maintenance : « HTTP 503 » brut à
/// la création d'une session, « le serveur a refusé la requête » au couplage,
/// une liste vidée ailleurs, ou rien du tout. L'état vient désormais d'un seul
/// endroit, <see cref="ApiClient.State"/>, et se dit avec les mêmes mots.
/// </summary>
internal static class ServerNotice
{
    /// <summary>Bandeau tant que le site est en panne. Ne dessine rien sinon.</summary>
    public static void Draw()
    {
        var l = Plugin.L;
        var (icon, title, body) = Plugin.Api.State switch
        {
            ServerState.Maintenance => (FontAwesomeIcon.Wrench, l.ServerMaintenanceTitle, l.ServerMaintenanceBody),
            ServerState.Unreachable => (Icons.Warning,          l.ServerUnreachableTitle, l.ServerUnreachableBody),
            _                       => (default(FontAwesomeIcon), null, null),
        };
        if (title == null) return;

        Feedback.Alert(Theme.Idle, icon, title, body);
        Layout.Spacer(Theme.GapS);
    }

    /// <summary>
    /// Message d'échec d'un geste : celui de la panne quand le site ne répond
    /// plus, <paramref name="fallback"/> sinon. La panne prime parce qu'elle dit
    /// quoi faire (attendre, ou vérifier sa connexion), là où « erreur lors de
    /// l'enregistrement » laisse chercher.
    /// </summary>
    public static string Explain(string fallback) => Plugin.Api.State switch
    {
        ServerState.Maintenance => Plugin.L.ServerMaintenanceShort,
        ServerState.Unreachable => Plugin.L.ServerUnreachableShort,
        _                       => fallback,
    };

    /// <summary>
    /// Même chose pour un échec levé en exception : le refus rédigé par le
    /// serveur se montre tel quel, tout le reste (« HTTP 503 », message réseau
    /// en anglais) cède la place à <see cref="Explain(string)"/>.
    /// </summary>
    public static string Explain(Exception ex, string fallback) =>
        ex is ServerMessageException ? ex.Message : Explain(fallback);
}

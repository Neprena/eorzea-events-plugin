using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>
/// Bandeau de pied de fenêtre : état de la liaison au compte et nombre de
/// joueurs connectés.
///
/// Le compteur est récupéré en tâche de fond, au plus une fois par minute.
/// </summary>
internal static class StatusBar
{
    private static int      _online;
    private static DateTime _lastFetch = DateTime.MinValue;

    public static void Draw()
    {
        Refresh();

        var height = Theme.S(Theme.StatusBarHeight);
        var origin = ImGui.GetCursorScreenPos();
        var width  = ImGui.GetContentRegionAvail().X;
        var end    = new Vector2(origin.X + width, origin.Y + height);
        var dl     = ImGui.GetWindowDrawList();

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Theme.BgSidebar),
            Theme.S(Theme.RadiusWindow), ImDrawFlags.RoundCornersBottom);
        dl.AddLine(origin, new Vector2(end.X, origin.Y),
            ImGui.GetColorU32(Theme.BorderSoft), 1f);

        using var font = Fonts.PushSmall();

        var mid = origin.Y + height * 0.5f;

        // État de la liaison, à gauche.
        var linked = Plugin.Api.HasToken && Plugin.Api.IsTokenValid;
        dl.AddCircleFilled(new Vector2(origin.X + Theme.S(Theme.PadWindowX), mid),
            Theme.S(3.5f), ImGui.GetColorU32(linked ? Theme.Online : Theme.TextFaint));

        // Compteur de joueurs, à droite.
        if (_online > 0)
        {
            var text = string.Format(Plugin.L.PlayersOnline, _online);
            var size = ImGui.CalcTextSize(text);
            dl.AddText(new Vector2(end.X - size.X - Theme.S(Theme.PadWindowX), mid - size.Y * 0.5f),
                ImGui.GetColorU32(Theme.TextFaint), text);
        }

        ImGui.Dummy(new Vector2(width, height));
    }

    private static void Refresh()
    {
        if ((DateTime.UtcNow - _lastFetch).TotalSeconds <= 60) return;

        _lastFetch = DateTime.UtcNow;
        _ = Task.Run(async () => _online = await Plugin.Api.GetOnlineCountAsync());
    }
}

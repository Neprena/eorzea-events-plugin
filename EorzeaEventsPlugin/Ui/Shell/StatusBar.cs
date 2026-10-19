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

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Theme.Alpha(Theme.BgSidebar, AppShell.ChromeOpacity(0.10f))),
            Theme.S(Theme.RadiusWindow), ImDrawFlags.RoundCornersBottom);
        dl.AddLine(origin, new Vector2(end.X, origin.Y),
            ImGui.GetColorU32(Theme.BorderSoft), 1f);

        // Corps de texte et non petit, comme dans Linkpearl : en bas d'une
        // grande fenêtre, l'état se lisait mal en 12 px.
        var mid = origin.Y + height * 0.5f;

        // État de la liaison, à gauche.
        var linked = Plugin.Api.HasToken && !Plugin.Api.IsTokenRevoked;
        dl.AddCircleFilled(new Vector2(origin.X + Theme.S(Theme.PadWindowX), mid),
            Theme.S(4.5f), ImGui.GetColorU32(linked ? Theme.Online : Theme.TextFaint));

        // Nommer l'état en toutes lettres : un point n'a jamais rien dit à
        // personne, et le plugin sert pour de bon sans compte. L'état prend la
        // couleur du point, le détail reste discret.
        var state  = linked ? Plugin.L.StatusLinked : Plugin.L.GuestMode;
        var detail = linked ? LinkedCharacter() : null;
        var x      = origin.X + Theme.S(Theme.PadWindowX + 12f);

        var stateSize = ImGui.CalcTextSize(state);
        dl.AddText(new Vector2(x, mid - stateSize.Y * 0.5f),
            ImGui.GetColorU32(linked ? Theme.Online : Theme.TextMuted), state);

        if (detail != null)
            dl.AddText(new Vector2(x + stateSize.X, mid - stateSize.Y * 0.5f),
                ImGui.GetColorU32(Theme.TextFaint), $" · {detail}");

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

    /// <summary>« Nom (Monde) » du personnage lié qui est connecté, ou null hors jeu.</summary>
    private static string? LinkedCharacter()
    {
        if (Plugin.CurrentCharacter is not { } character) return null;

        var world = Plugin.Config.FindCharacterToken(character.Name, character.WorldId)?.WorldName;
        return world is { Length: > 0 } ? $"{character.Name} ({world})" : character.Name;
    }

    private static void Refresh()
    {
        if ((DateTime.UtcNow - _lastFetch).TotalSeconds <= 60) return;

        _lastFetch = DateTime.UtcNow;
        _ = Task.Run(async () => _online = await Plugin.Api.GetOnlineCountAsync());
    }
}

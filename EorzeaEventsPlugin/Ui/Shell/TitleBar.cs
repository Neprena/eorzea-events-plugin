using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>
/// Barre de titre maison, en remplacement de celle de Dalamud.
///
/// Elle assume le déplacement de la fenêtre, que <c>NoTitleBar</c> supprime.
/// Le redimensionnement, lui, reste natif : la fenêtre ne pose pas
/// <c>NoResize</c>, ImGui gère donc les bords et la poignée.
/// </summary>
internal static class TitleBar
{
    /// <summary>Dessine la barre. Retourne vrai si la fermeture est demandée.</summary>
    public static bool Draw(float width)
    {
        var height = Theme.S(Theme.TitleBarHeight);
        var origin = ImGui.GetCursorScreenPos();
        var end    = new Vector2(origin.X + width, origin.Y + height);
        var dl     = ImGui.GetWindowDrawList();

        // Bandeau d'accent, dans le ton foncé. Le turquoise vif porterait mal
        // du texte blanc : son contraste tombe sous 2:1, alors que le ton foncé
        // reste au-dessus du seuil de lisibilité pour un titre.
        // Le rayon doit être celui de la fenêtre, sinon le fond déborde aux angles.
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Theme.AccentActive),
            Theme.S(Theme.RadiusWindow), ImDrawFlags.RoundCornersTop);

        // Dégradé vers le turquoise vif : un aplat uni sur toute la largeur est
        // plus plat qu'un bandeau qui capte la lumière sur la gauche.
        var glow  = ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.42f));
        var clear = ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0f));
        dl.AddRectFilledMultiColor(origin, end, glow, clear, clear, glow);

        var buttons    = ImGui.GetFrameHeight() * 2f + Theme.S(Theme.GapM) * 3f;
        var closeAsked = false;

        // ── Zone de déplacement ───────────────────────────────────────────────
        ImGui.SetCursorScreenPos(origin);
        ImGui.InvisibleButton("##titledrag", new Vector2(Math.Max(1f, width - buttons), height));
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            ImGui.SetWindowPos(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta);

        // ── Marque ────────────────────────────────────────────────────────────
        using (Fonts.PushH2())
        {
            const string text = "Eorzea Events";
            var size = ImGui.CalcTextSize(text);
            dl.AddText(origin + new Vector2(Theme.S(Theme.PadWindowX), (height - size.Y) * 0.5f),
                ImGui.GetColorU32(Theme.Text), text);
        }

        // ── Actions ───────────────────────────────────────────────────────────
        var side = ImGui.GetFrameHeight();
        ImGui.SetCursorScreenPos(new Vector2(end.X - buttons + Theme.S(Theme.GapM),
                                             origin.Y + (height - side) * 0.5f));

        // Les boutons héritent du contraste calculé pour le bandeau.
        using (ImRaii.PushColor(ImGuiCol.Text, Theme.Text))
        {
            if (Btn.Icon(Icons.External, "shell_site", BtnTone.Ghost, Plugin.L.ViewOnline))
                OpenSite();

            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            if (Btn.Icon(Icons.Close, "shell_close", BtnTone.Ghost))
                closeAsked = true;
        }

        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
        return closeAsked;
    }

    private static void OpenSite() =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl) { UseShellExecute = true });
}

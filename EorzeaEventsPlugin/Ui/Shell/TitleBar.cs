using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
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

        // Boutons carrés calés sur la hauteur de la barre, et non sur la hauteur
        // d'un cadre ImGui : celle-ci dépend de la police et du padding, si bien
        // que le fond ne tombait jamais au centre du bandeau. Marge identique en
        // haut, en bas et à droite.
        var margin = MathF.Round(Theme.S(Theme.GapS));
        var side   = MathF.Round(height - margin * 2f);
        var gap    = MathF.Round(Theme.S(Theme.GapXs));
        var buttons = side * 2f + gap + margin;

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
        var top   = MathF.Round(origin.Y + margin);
        var right = MathF.Round(end.X - margin);

        if (IconButton(dl, new Vector2(right - side * 2f - gap, top), side,
                       Icons.External, "shell_site", Plugin.L.ViewOnline))
            OpenSite();

        if (IconButton(dl, new Vector2(right - side, top), side,
                       Icons.Close, "shell_close"))
            closeAsked = true;

        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
        return closeAsked;
    }

    /// <summary>
    /// Bouton d'icône dessiné à la main.
    ///
    /// <c>ImGui.Button</c> dimensionne son fond à partir du cadre courant, donc
    /// de la police et du padding : sur un bandeau de hauteur fixe, il ne tombait
    /// pas au centre. Ici le carré est posé aux coordonnées voulues, et le glyphe
    /// centré sur son encombrement réel.
    /// </summary>
    private static bool IconButton(ImDrawListPtr dl, Vector2 position, float side,
                                   FontAwesomeIcon icon, string id, string? tooltip = null)
    {
        ImGui.SetCursorScreenPos(position);
        var clicked = ImGui.InvisibleButton($"##{id}", new Vector2(side, side));
        var hovered = ImGui.IsItemHovered();

        if (hovered)
        {
            dl.AddRectFilled(position, position + new Vector2(side, side),
                             ImGui.GetColorU32(Theme.Alpha(Theme.Text, 0.16f)),
                             Theme.S(Theme.RadiusFrame));
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        var glyph = icon.S();
        var size  = ImGui.CalcTextSize(glyph);
        dl.AddText(new Vector2(MathF.Round(position.X + (side - size.X) * 0.5f),
                               MathF.Round(position.Y + (side - size.Y) * 0.5f)),
                   ImGui.GetColorU32(Theme.Text), glyph);

        if (tooltip != null && hovered) Feedback.Tooltip(tooltip);

        return clicked;
    }

    private static void OpenSite() =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl) { UseShellExecute = true });
}

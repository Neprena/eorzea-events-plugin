using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

internal enum ChipTone
{
    Neutral,
    Accent,
    Gold,
    Success,
    Warning,
    Danger,
}

/// <summary>
/// Pastilles d'information : serveur, quartier, langue, thème, statut.
///
/// Les fonds sont des mélanges opaques et non des voiles translucides : sur un
/// fond sombre, un voile perd sa teinte et toutes les pastilles finissent par
/// se ressembler.
/// </summary>
internal static class Chip
{
    /// <summary>
    /// Rembourrage commun à toutes les pastilles. Regroupé ici plutôt que recopié
    /// dans chaque fonction : <see cref="Measure"/> et <see cref="Height"/>
    /// doivent annoncer exactement l'encombrement que <see cref="Draw"/> produit.
    /// </summary>
    private static Vector2 Padding => new(Theme.S(Theme.GapM), Theme.S(3f));

    /// <summary>Affiche une pastille. Enchaîner avec <c>ImGui.SameLine</c>.</summary>
    /// <param name="alignToFrame">
    /// Centre la pastille sur la hauteur d'un widget encadré. À activer quand
    /// elle suit un <c>SameLine</c> après un bouton, un champ ou un en-tête
    /// repliable : la pastille est plus basse qu'eux et se retrouverait sinon
    /// collée en haut de la ligne, à buter contre le texte voisin.
    /// </param>
    public static void Draw(string text, ChipTone tone = ChipTone.Neutral,
                            FontAwesomeIcon? icon = null, string? tooltip = null,
                            bool alignToFrame = false)
    {
        using var font = Fonts.PushSmall();

        var caption = Compose(text, icon);
        var padding = Padding;
        var size    = ImGui.CalcTextSize(caption) + padding * 2f;

        // Le décalage se pose avant de lire la position : le rectangle est peint
        // à la main et ne suivrait pas le curseur après coup.
        if (alignToFrame)
        {
            var slack = ImGui.GetFrameHeight() - size.Y;
            if (slack > 0f) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slack * 0.5f);
        }

        var origin  = ImGui.GetCursorScreenPos();
        var dl      = ImGui.GetWindowDrawList();

        var (background, foreground) = Palette(tone);

        dl.AddRectFilled(origin, origin + size,
            ImGui.GetColorU32(background), Theme.S(Theme.RadiusPill));
        dl.AddText(origin + padding, ImGui.GetColorU32(foreground), caption);

        ImGui.Dummy(size);

        if (tooltip != null) Feedback.TooltipOnHover(tooltip);
    }

    /// <summary>
    /// Pastille dont la teinte vient de la donnée elle-même, par exemple la
    /// couleur propre d'une catégorie d'établissement. La couleur est mélangée
    /// à la surface plutôt qu'appliquée telle quelle : en aplat, des teintes
    /// choisies pour un fond clair deviennent criardes sur fond sombre.
    /// </summary>
    public static void Colored(string text, Vector4 tint,
                               FontAwesomeIcon? icon = null, string? tooltip = null)
    {
        using var font = Fonts.PushSmall();

        var caption    = Compose(text, icon);
        var padding    = Padding;
        var size       = ImGui.CalcTextSize(caption) + padding * 2f;
        var origin     = ImGui.GetCursorScreenPos();
        var dl         = ImGui.GetWindowDrawList();
        var background = Theme.Mix(Theme.BgRaised, tint, 0.28f);
        var border     = Theme.Alpha(Theme.EnsureReadable(tint), 0.55f);
        var rounding   = Theme.S(Theme.RadiusPill);

        dl.AddRectFilled(origin, origin + size, ImGui.GetColorU32(background), rounding);
        dl.AddRect(origin, origin + size, ImGui.GetColorU32(border), rounding);
        dl.AddText(origin + padding, ImGui.GetColorU32(Theme.TextOn(background)), caption);

        ImGui.Dummy(size);

        if (tooltip != null) Feedback.TooltipOnHover(tooltip);
    }

    /// <summary>
    /// Suite de pastilles qui passe à la ligne quand la largeur disponible est
    /// dépassée, plutôt que de déborder du panneau.
    /// </summary>
    public static void Row(params (string Text, ChipTone Tone)[] chips)
    {
        var limit   = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        var spacing = Theme.S(Theme.GapXs);
        var first   = true;

        foreach (var (text, tone) in chips)
        {
            if (!first && ImGui.GetCursorPosX() + spacing + Measure(text) <= limit)
                ImGui.SameLine(0f, spacing);

            Draw(text, tone);
            first = false;
        }
    }

    /// <summary>Largeur qu'occuperait la pastille.</summary>
    public static float Measure(string text, FontAwesomeIcon? icon = null)
    {
        using var font = Fonts.PushSmall();
        return ImGui.CalcTextSize(Compose(text, icon)).X + Padding.X * 2f;
    }

    /// <summary>
    /// Hauteur qu'occupe une pastille. Indépendante du libellé, toutes étant
    /// écrites dans la même police : les appelants qui doivent réserver une
    /// hauteur avant de connaître le texte n'ont donc rien à passer.
    /// </summary>
    public static float Height()
    {
        using var font = Fonts.PushSmall();
        return ImGui.GetTextLineHeight() + Padding.Y * 2f;
    }

    private static string Compose(string text, FontAwesomeIcon? icon)
    {
        var safe = Glyphs.Safe(text);
        return icon is { } value ? $"{value.S()}  {safe}" : safe;
    }

    private static (Vector4 Background, Vector4 Foreground) Palette(ChipTone tone)
    {
        var background = tone switch
        {
            ChipTone.Accent  => Theme.Mix(Theme.BgRaised, Theme.Accent, 0.32f),
            ChipTone.Gold    => Theme.Mix(Theme.BgRaised, Theme.Gold,   0.32f),
            ChipTone.Success => Theme.Mix(Theme.BgRaised, Theme.Online, 0.32f),
            ChipTone.Warning => Theme.Mix(Theme.BgRaised, Theme.Idle,   0.32f),
            ChipTone.Danger  => Theme.Mix(Theme.BgRaised, Theme.Danger, 0.32f),
            _                => Theme.BgRaised,
        };

        return (background, Theme.TextOn(background));
    }
}

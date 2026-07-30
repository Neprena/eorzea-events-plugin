using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

internal enum BtnTone
{
    /// <summary>Action principale de l'écran. Un seul par vue.</summary>
    Primary,

    /// <summary>Action courante, surface neutre surélevée.</summary>
    Secondary,

    /// <summary>Action discrète : pas de fond, sauf au survol.</summary>
    Ghost,

    /// <summary>Action destructrice.</summary>
    Danger,

    /// <summary>Action de confirmation.</summary>
    Success,
}

internal enum BtnSize
{
    Small,
    Medium,

    /// <summary>Occupe toute la largeur disponible.</summary>
    Block,
}

/// <summary>
/// Boutons.
///
/// La couleur du libellé est déduite de la luminance du fond, ce qui garantit
/// la lisibilité même sur un accent clair et permet de changer la palette sans
/// repasser sur chaque bouton.
/// </summary>
internal static class Btn
{
    /// <summary>
    /// Dessine un bouton. Passer <paramref name="id"/> lorsque plusieurs boutons
    /// partagent le même libellé dans une même fenêtre.
    /// </summary>
    public static bool Draw(string label,
                            BtnTone tone = BtnTone.Secondary,
                            BtnSize size = BtnSize.Medium,
                            FontAwesomeIcon? icon = null,
                            bool disabled = false,
                            string? tooltip = null,
                            string? id = null)
    {
        var (normal, hovered, active) = Palette(tone);
        var caption = Compose(label, icon);

        using var color = ImRaii.PushColor(ImGuiCol.Button, normal)
                                .Push(ImGuiCol.ButtonHovered, hovered)
                                .Push(ImGuiCol.ButtonActive,  active)
                                .Push(ImGuiCol.Text,          Theme.TextOn(normal));
        // Le contour des champs de saisie ne doit pas déborder sur les boutons,
        // qui se distinguent déjà par leur fond.
        using var flat = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 0f);

        bool clicked;
        using (ImRaii.Disabled(disabled))
            clicked = ImGui.Button($"{caption}##{id ?? label}", Dimensions(size));

        // Hors du scope désactivé : un widget désactivé ne remonte pas le survol.
        if (tooltip != null) Feedback.TooltipOnHover(tooltip);

        return clicked && !disabled;
    }

    /// <summary>Bouton réduit à une icône, carré.</summary>
    public static bool Icon(FontAwesomeIcon icon, string id,
                            BtnTone tone = BtnTone.Ghost,
                            string? tooltip = null,
                            bool disabled = false)
    {
        var (normal, hovered, active) = Palette(tone);
        var side = ImGui.GetFrameHeight();

        using var color = ImRaii.PushColor(ImGuiCol.Button, normal)
                                .Push(ImGuiCol.ButtonHovered, hovered)
                                .Push(ImGuiCol.ButtonActive,  active)
                                .Push(ImGuiCol.Text,          Theme.TextOn(normal));
        // Le contour des champs de saisie ne doit pas déborder sur les boutons,
        // qui se distinguent déjà par leur fond.
        using var flat = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 0f);

        bool clicked;
        using (ImRaii.Disabled(disabled))
            clicked = ImGui.Button($"{icon.S()}##{id}", new Vector2(side, side));

        if (tooltip != null) Feedback.TooltipOnHover(tooltip);

        return clicked && !disabled;
    }

    /// <summary>Largeur qu'occuperait le bouton, pour centrer ou aligner à droite.</summary>
    public static float Measure(string label, FontAwesomeIcon? icon = null) =>
        ImGui.CalcTextSize(Compose(label, icon)).X + ImGui.GetStyle().FramePadding.X * 2f;

    private static string Compose(string label, FontAwesomeIcon? icon) =>
        icon is { } value ? $"{value.S()}  {label}" : label;

    private static Vector2 Dimensions(BtnSize size) => size switch
    {
        BtnSize.Block => new Vector2(-1f, 0f),
        BtnSize.Small => new Vector2(Theme.S(92f), 0f),
        _             => Vector2.Zero, // largeur ajustée au contenu
    };

    private static (Vector4 Normal, Vector4 Hovered, Vector4 Active) Palette(BtnTone tone) => tone switch
    {
        BtnTone.Primary => (Theme.Accent, Theme.AccentHover, Theme.AccentActive),
        BtnTone.Danger  => (Theme.Danger, Theme.DangerHover, Theme.Mix(Theme.Danger, Theme.BgBase, 0.3f)),
        BtnTone.Success => (Theme.Online, Theme.Mix(Theme.Online, Theme.Text, 0.2f),
                            Theme.Mix(Theme.Online, Theme.BgBase, 0.3f)),
        BtnTone.Ghost   => (Vector4.Zero, Theme.Mix(Theme.BgRaised, Theme.Accent, 0.18f), Theme.BgHover),
        _               => (Theme.BgRaised, Theme.Mix(Theme.BgHover, Theme.Accent, 0.15f), Theme.BgSurface),
    };
}

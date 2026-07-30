using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Façade de transition vers <c>Ui.Components</c>.
///
/// Les signatures sont conservées à l'identique pour que les fenêtres existantes
/// bénéficient des nouveaux rendus sans être modifiées. Ce fichier disparaît une
/// fois toutes les fenêtres migrées vers les composants.
/// </summary>
internal static class UiPrimitives
{
    // Les appelants historiques ne fournissent pas d'identifiant de carte. Un
    // compteur remis à zéro à chaque frame en fabrique un, stable tant que
    // l'ordre de dessin l'est, ce qui suffit au cache de hauteur.
    private static int _frame = -1;
    private static int _sequence;

    private static string NextCardId()
    {
        var frame = ImGui.GetFrameCount();
        if (frame != _frame)
        {
            _frame    = frame;
            _sequence = 0;
        }

        return $"##legacycard{_sequence++}";
    }

    // ── Card ──────────────────────────────────────────────────────────────────

    public static void DrawCard(Action content, Vector4? bgColor = null)
    {
        using var card = Card.Begin(NextCardId(), CardTone.Flat,
                                    interactive: false, background: bgColor);
        content();
    }

    public static void DrawCardWithBanner(IDalamudTextureWrap? bannerWrap, Action content,
                                          Vector4? bgColor = null)
    {
        using var card = Card.Begin(NextCardId(), CardTone.Interactive,
                                    background: bgColor, banner: bannerWrap);
        content();
    }

    // ── Chip ──────────────────────────────────────────────────────────────────

    public static void DrawChip(string text, Vector4? bgColor = null) =>
        Chip.Draw(text, ToneOf(bgColor));

    private static ChipTone ToneOf(Vector4? background)
    {
        if (background is not { } color) return ChipTone.Neutral;
        if (color == UiStyle.ChipBgOpen)   return ChipTone.Success;
        if (color == UiStyle.ChipBgSoon)   return ChipTone.Accent;
        if (color == UiStyle.ChipBgLater)  return ChipTone.Warning;
        if (color == UiStyle.ChipBgAccent) return ChipTone.Accent;
        return ChipTone.Neutral;
    }

    // ── Icône inline ──────────────────────────────────────────────────────────

    /// <summary>
    /// Affiche une icône. Toujours préférer cette surcharge à celle qui prend
    /// une chaîne : les points de code FontAwesome sont invisibles dans un
    /// éditeur et se perdent au moindre accident d'encodage.
    /// </summary>
    public static void DrawIcon(FontAwesomeIcon icon, Vector4? color = null) =>
        Text.Icon(icon, color);

    public static void DrawIcon(string glyph, Vector4? color = null)
    {
        using var _ = Plugin.PluginInterface.UiBuilder.IconFontHandle.Push();
        ImGui.TextColored(color ?? UiStyle.TextMuted, glyph);
    }

    // ── Alerte ────────────────────────────────────────────────────────────────

    public static void DrawAlert(Vector4 color, string title, string desc, Action buttons) =>
        Feedback.Alert(color, IconFor(color), title, desc, buttons);

    /// <summary>Déduit le pictogramme de la couleur d'état passée par l'appelant.</summary>
    private static FontAwesomeIcon IconFor(Vector4 color)
    {
        if (color == Theme.Online) return Icons.Check;
        if (color == Theme.Danger) return Icons.Warning;
        if (color == Theme.Accent) return Icons.Info;
        return Icons.Warning;
    }

    // ── Bouton coloré ─────────────────────────────────────────────────────────

    public static bool ColorButton(string label, Vector2 size,
        Vector4 normal, Vector4 hovered, Vector4 active)
    {
        using var color = Dalamud.Interface.Utility.Raii.ImRaii
            .PushColor(ImGuiCol.Button, normal)
            .Push(ImGuiCol.ButtonHovered, hovered)
            .Push(ImGuiCol.ButtonActive,  active)
            // Le libellé s'adapte à la clarté du fond : sur un accent vif, du
            // texte clair serait illisible.
            .Push(ImGuiCol.Text,          Theme.TextOn(normal));

        return ImGui.Button(label, size);
    }
}

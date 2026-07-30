using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Contrôles de saisie : interrupteurs, champs de texte et barre de recherche.
/// </summary>
internal static class Inputs
{
    /// <summary>Position animée de chaque interrupteur, indexée par identifiant ImGui.</summary>
    private static readonly Dictionary<uint, float> ToggleAnimation = [];

    // ─── Interrupteur ─────────────────────────────────────────────────────────

    /// <summary>
    /// Interrupteur à glissière, en remplacement de la case à cocher par défaut.
    /// L'animation respecte le réglage d'accessibilité de Dalamud.
    /// </summary>
    public static bool Toggle(string id, ref bool value, string? tooltip = null, bool disabled = false)
    {
        var frame  = ImGui.GetFrameHeight();
        var height = frame * 0.82f;
        var width  = height * 1.85f;
        var radius = height * 0.5f;

        var origin = ImGui.GetCursorScreenPos();
        var dl     = ImGui.GetWindowDrawList();

        var changed = false;
        using (ImRaii.Disabled(disabled))
        {
            ImGui.InvisibleButton(id, new Vector2(width, frame));
            if (ImGui.IsItemClicked() && !disabled)
            {
                value   = !value;
                changed = true;
            }
        }

        var hovered = ImGui.IsItemHovered();
        var key     = ImGui.GetID(id);

        var target = value ? 1f : 0f;
        if (Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion)
        {
            ToggleAnimation[key] = target;
        }
        else
        {
            var current = ToggleAnimation.TryGetValue(key, out var stored) ? stored : target;
            var step    = Math.Clamp(ImGui.GetIO().DeltaTime * 12f, 0f, 1f);
            ToggleAnimation[key] = current + (target - current) * step;
        }

        var t     = ToggleAnimation[key];
        var top   = origin.Y + (frame - height) * 0.5f;
        var min   = new Vector2(origin.X, top);
        var max   = new Vector2(origin.X + width, top + height);
        var track = Theme.Mix(hovered ? Theme.BgHover : Theme.BgSunken, Theme.Accent, t);
        var alpha = disabled ? 0.45f : 1f;

        dl.AddRectFilled(min, max, ImGui.GetColorU32(Theme.Alpha(track, alpha)), radius);
        dl.AddRect(min, max, ImGui.GetColorU32(Theme.Alpha(Theme.Border, alpha)), radius);

        var knobX = min.X + radius + t * (width - height);
        dl.AddCircleFilled(new Vector2(knobX, top + radius), radius - Theme.S(2.5f),
            ImGui.GetColorU32(Theme.Alpha(value ? Theme.TextOnLight : Theme.TextMuted, alpha)));

        if (tooltip != null) Feedback.TooltipOnHover(tooltip);

        return changed;
    }

    /// <summary>
    /// Ligne de réglage : libellé et description à gauche, interrupteur aligné
    /// à droite.
    /// </summary>
    public static bool ToggleRow(string label, ref bool value,
                                 string? description = null,
                                 FontAwesomeIcon? icon = null,
                                 bool disabled = false)
    {
        var toggleWidth = ImGui.GetFrameHeight() * 0.82f * 1.85f;

        ImGui.BeginGroup();

        if (icon is { } glyph)
        {
            Text.Icon(glyph, Theme.TextMuted);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
        }

        ImGui.TextColored(disabled ? Theme.TextFaint : Theme.Text, label);

        if (description != null)
        {
            using var font = Fonts.PushSmall();
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X
                                  - toggleWidth - Theme.S(Theme.GapL));
            ImGui.TextColored(Theme.TextFaint, description);
            ImGui.PopTextWrapPos();
        }

        ImGui.EndGroup();

        ImGui.SameLine();
        Layout.RightAlign(toggleWidth);

        return Toggle($"##toggle_{label}", ref value, disabled: disabled);
    }

    // ─── Champ de texte ───────────────────────────────────────────────────────

    /// <summary>
    /// Champ de texte avec libellé au-dessus, indication de saisie, compteur de
    /// caractères et message d'erreur.
    /// </summary>
    public static bool Field(string id, string label, ref string text, int maxLength,
                            string? placeholder = null, string? help = null,
                            string? error = null, bool showCounter = false,
                            bool multiline = false, float height = 90f)
    {
        if (!string.IsNullOrEmpty(label))
        {
            ImGui.TextColored(Theme.TextMuted, label);
            if (help != null)
            {
                ImGui.SameLine(0f, Theme.S(Theme.GapS));
                Feedback.Help(help);
            }
        }

        var invalid = !string.IsNullOrEmpty(error);
        using var frame = ImRaii.PushColor(ImGuiCol.Border,
            invalid ? Theme.Danger : Theme.BorderSoft);

        var origin = ImGui.GetCursorScreenPos();
        ImGui.SetNextItemWidth(Card.FullWidth);

        // Deux appels distincts plutôt qu'un ternaire : les surcharges du binding
        // ne s'unifient pas dans une expression conditionnelle.
        bool changed;
        if (multiline)
            changed = ImGui.InputTextMultiline(id, ref text, maxLength, new Vector2(Card.FullWidth, Theme.S(height)));
        else
            changed = ImGui.InputText(id, ref text, maxLength);

        // Indication de saisie dessinée à la main : ImGui n'en propose pas.
        if (string.IsNullOrEmpty(text) && placeholder != null && !ImGui.IsItemActive())
        {
            var padding = ImGui.GetStyle().FramePadding;
            ImGui.GetWindowDrawList().AddText(origin + padding,
                ImGui.GetColorU32(Theme.TextFaint), placeholder);
        }

        if (!invalid && !showCounter) return changed;

        using (Fonts.PushSmall())
        {
            if (invalid)
            {
                ImGui.TextColored(Theme.Danger, $"{Icons.Warning.S()}  {error}");
                if (showCounter) ImGui.SameLine();
            }

            if (showCounter)
            {
                var used    = text.Length;
                var tone    = used >= maxLength ? Theme.Danger
                            : used > maxLength * 0.9f ? Theme.Idle
                            : Theme.TextFaint;
                var counter = $"{used} / {maxLength}";
                Layout.RightAlign(ImGui.CalcTextSize(counter).X);
                ImGui.TextColored(tone, counter);
            }
        }

        return changed;
    }

    /// <summary>
    /// Liste déroulante avec libellé au-dessus.
    ///
    /// <c>ImGui.Combo</c> place son libellé à droite du widget : sur une
    /// largeur pleine, il sort du cadre. Le libellé est donc dessiné à part et
    /// l'identifiant du widget masqué.
    /// </summary>
    public static bool Select(string id, string label, ref int index, string[] options)
    {
        if (!string.IsNullOrEmpty(label))
            ImGui.TextColored(Theme.TextMuted, label);

        ImGui.SetNextItemWidth(Card.FullWidth);
        return ImGui.Combo(id, ref index, options, options.Length);
    }

    // ─── Recherche ────────────────────────────────────────────────────────────

    /// <summary>
    /// Barre de recherche : loupe intégrée, croix d'effacement, validation à la
    /// touche Entrée. Retourne vrai quand une recherche doit être lancée.
    /// </summary>
    public static bool SearchBar(string id, ref string query, string placeholder, float width = -1f)
    {
        var iconWidth  = ImGui.CalcTextSize(Icons.Search.S()).X;
        var padding    = ImGui.GetStyle().FramePadding;
        var origin     = ImGui.GetCursorScreenPos();
        var clearWidth = string.IsNullOrEmpty(query) ? 0f : ImGui.GetFrameHeight();
        var gap        = clearWidth > 0f ? Theme.S(Theme.GapXs) : 0f;

        ImGui.SetNextItemWidth(width < 0f
            ? ImGui.GetContentRegionAvail().X - Card.RightInset - clearWidth - gap
            : width);

        bool submitted;
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding,
                   new Vector2(padding.X + iconWidth + Theme.S(Theme.GapS), padding.Y)))
        {
            submitted = ImGui.InputText(id, ref query, 100, ImGuiInputTextFlags.EnterReturnsTrue);
        }

        var dl = ImGui.GetWindowDrawList();
        dl.AddText(origin + padding, ImGui.GetColorU32(Theme.TextFaint), Icons.Search.S());

        if (string.IsNullOrEmpty(query) && !ImGui.IsItemActive())
        {
            dl.AddText(origin + new Vector2(padding.X + iconWidth + Theme.S(Theme.GapS), padding.Y),
                ImGui.GetColorU32(Theme.TextFaint), placeholder);
        }

        if (clearWidth > 0f)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            if (Btn.Icon(Icons.Close, $"{id}_clear", BtnTone.Ghost))
            {
                query     = string.Empty;
                submitted = true;
            }
        }

        return submitted;
    }
}

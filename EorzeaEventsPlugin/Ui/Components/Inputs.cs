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
        dl.AddRect(min, max, ImGui.GetColorU32(Theme.Faded(Theme.Border, alpha)), radius);

        var knobX = min.X + radius + t * (width - height);
        dl.AddCircleFilled(new Vector2(knobX, top + radius), radius - Theme.S(2.5f),
            ImGui.GetColorU32(Theme.Alpha(value ? Theme.TextOnLight : Theme.TextMuted, alpha)));

        if (tooltip != null) Feedback.TooltipOnHover(tooltip);

        return changed;
    }

    /// <summary>
    /// Curseur de valeur entière, avec la valeur courante à droite.
    ///
    /// Dessiné plutôt que laissé au style d'origine d'ImGui : c'était le seul
    /// contrôle du plugin à ne ressembler à rien d'autre, une barre pleine sur
    /// fond noir au milieu de cartes et d'interrupteurs.
    ///
    /// La place de la valeur est réservée d'après le plus grand libellé possible
    /// et non d'après la valeur courante, sans quoi la piste changerait de
    /// longueur en glissant, et le curseur fuirait sous le doigt.
    /// </summary>
    public static bool Slider(string id, ref int value, int min, int max,
                              string? suffix = null, float width = -1f)
    {
        var frame = ImGui.GetFrameHeight();
        var total = width > 0f ? width : ImGui.GetContentRegionAvail().X;

        string Label(int v) => suffix is null ? v.ToString() : $"{v} {suffix}";

        var gap      = Theme.S(Theme.GapS);
        var valueW   = ImGui.CalcTextSize(Label(max)).X;
        var trackW   = Math.Max(total - valueW - gap, Theme.S(40f));
        var thickness = Theme.S(5f);
        var knob      = frame * 0.24f;

        var origin = ImGui.GetCursorScreenPos();
        var dl     = ImGui.GetWindowDrawList();

        ImGui.InvisibleButton(id, new Vector2(trackW, frame));
        var hovered = ImGui.IsItemHovered();
        var held    = ImGui.IsItemActive();

        var left  = origin.X + knob;
        var right = origin.X + trackW - knob;
        var span  = Math.Max(right - left, 1f);

        var changed = false;
        if (held && max > min)
        {
            var t    = Math.Clamp((ImGui.GetIO().MousePos.X - left) / span, 0f, 1f);
            var next = min + (int)MathF.Round(t * (max - min));
            if (next != value) { value = next; changed = true; }
        }

        var ratio = max > min ? (value - min) / (float)(max - min) : 0f;
        var midY  = origin.Y + frame * 0.5f;
        var cx    = left + ratio * span;
        var half  = thickness * 0.5f;

        dl.AddRectFilled(new Vector2(origin.X, midY - half),
                         new Vector2(origin.X + trackW, midY + half),
                         ImGui.GetColorU32(Theme.BgSunken), half);
        dl.AddRectFilled(new Vector2(origin.X, midY - half), new Vector2(cx, midY + half),
                         ImGui.GetColorU32(Theme.Accent), half);

        dl.AddCircleFilled(new Vector2(cx, midY), knob,
                           ImGui.GetColorU32(hovered || held ? Theme.Text : Theme.TextOnLight));
        dl.AddCircle(new Vector2(cx, midY), knob, ImGui.GetColorU32(Theme.Border));

        ImGui.SameLine(0f, gap);
        ImGui.AlignTextToFramePadding();
        Text.Body(Label(value), Theme.Text);

        return changed;
    }

    /// <summary>
    /// Pastille de filtre : une option parmi d'autres, le choix actif en
    /// cristal, comme l'entrée active de la colonne. L'or reste à l'action
    /// principale de l'écran : un filtre choisi n'en est pas une.
    /// </summary>
    public static bool Pill(string id, string label, bool active, string? tooltip = null)
    {
        var padX   = Theme.S(11f);
        var height = MathF.Round(ImGui.GetTextLineHeight() + Theme.S(8f));
        var size   = new Vector2(ImGui.CalcTextSize(label).X + padX * 2f, height);
        var min    = ImGui.GetCursorScreenPos();
        var max    = min + size;

        var clicked = ImGui.InvisibleButton(id, size);
        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var dl = ImGui.GetWindowDrawList();
        var r  = height * 0.5f;

        var light = Motion.Approach(ImGui.GetID($"{id}_light"), hovered ? 1f : 0f, Motion.Fast);
        var fill  = active ? Theme.Alpha(Theme.Accent, 0.16f + 0.06f * light)
                           : Theme.Alpha(Theme.Text, 0.06f * light);

        dl.AddRectFilled(min, max, ImGui.GetColorU32(fill), r);
        dl.AddRect(min, max, ImGui.GetColorU32(active ? Theme.Alpha(Theme.Accent, 0.75f)
                                                      : Theme.Alpha(Theme.Text, 0.14f)),
                   r, ImDrawFlags.None, 1f);

        var textSize = ImGui.CalcTextSize(label);
        dl.AddText(min + (size - textSize) * 0.5f, ImGui.GetColorU32(active ? Theme.Text : Theme.TextMuted), label);

        if (hovered && tooltip != null) Feedback.Tooltip(tooltip);

        return clicked;
    }

    /// <summary>Largeur d'une pastille, pour savoir si elle tient sur la ligne.</summary>
    public static float PillWidth(string label) => ImGui.CalcTextSize(label).X + Theme.S(22f);

    /// <summary>
    /// Ligne de réglage : interrupteur à gauche, libellé juste à côté,
    /// description dessous. Cliquer le libellé bascule aussi l'interrupteur.
    ///
    /// L'interrupteur était calé au bord droit, le libellé au bord gauche : sur
    /// une carte large, rien ne les reliait, et l'on passait à côté du réglage.
    /// </summary>
    /// <param name="below">
    /// Contenu posé sous la description, aligné sur le libellé : un bouton
    /// d'essai, par exemple. Il ne bascule pas l'interrupteur.
    /// </param>
    public static bool ToggleRow(string label, ref bool value,
                                 string? description = null,
                                 FontAwesomeIcon? icon = null,
                                 bool disabled = false,
                                 Action? below = null)
    {
        // La description entre dans l'identifiant : deux réglages peuvent porter
        // le même libellé (l'alerte écran des sessions et celle des événements),
        // et un identifiant partagé faisait trembler leurs deux curseurs.
        var top     = ImGui.GetCursorPosY();
        var changed = Toggle($"##toggle_{label}_{description}", ref value, disabled: disabled);

        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        ImGui.SetCursorPosY(top);

        var wrapAt = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Card.RightInset;

        ImGui.BeginGroup();
        ImGui.PushTextWrapPos(wrapAt);

        // Ligne du libellé à la hauteur d'un cadre, comme l'interrupteur.
        ImGui.AlignTextToFramePadding();
        if (icon is { } glyph)
        {
            Text.Icon(glyph, Theme.TextMuted);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
        }
        ImGui.TextColored(disabled ? Theme.TextFaint : Theme.Text, label);
        var clicked = ClickedText(disabled);

        // Taille du corps et gris lisible : en petit et pâle, on passait à côté.
        if (description != null)
        {
            ImGui.TextColored(disabled ? Theme.TextFaint : Theme.TextMuted, description);
            clicked |= ClickedText(disabled);
        }

        ImGui.PopTextWrapPos();

        if (below != null)
        {
            ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapXs)));
            below();
        }

        ImGui.EndGroup();

        if (clicked)
        {
            value   = !value;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Le texte qui vient d'être écrit a été cliqué. Seuls le libellé et la
    /// description basculent l'interrupteur, pas un bouton posé dessous.
    /// </summary>
    private static bool ClickedText(bool disabled)
    {
        if (disabled || !ImGui.IsItemHovered()) return false;
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        return ImGui.IsItemClicked();
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
            changed = TextArea.Draw(id, ref text, maxLength, new Vector2(Card.FullWidth, Theme.S(height)));
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

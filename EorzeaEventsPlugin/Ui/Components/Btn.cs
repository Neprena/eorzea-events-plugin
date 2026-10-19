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
                                .Push(ImGuiCol.Text,          TextFor(tone, normal));
        // Le contour des champs de saisie ne doit pas déborder sur les boutons,
        // qui se distinguent déjà par leur fond.
        using var flat = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 0f);

        bool clicked;
        using (ImRaii.Disabled(disabled))
            clicked = ImGui.Button($"{caption}##{id ?? label}", Dimensions(size, caption));

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
                                .Push(ImGuiCol.Text,          TextFor(tone, normal));
        // Le contour des champs de saisie ne doit pas déborder sur les boutons,
        // qui se distinguent déjà par leur fond.
        using var flat = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 0f);

        bool clicked;
        using (ImRaii.Disabled(disabled))
            clicked = ImGui.Button($"##{id}", new Vector2(side, side));

        var foreground = TextFor(tone, normal);
        DrawIconCentered(icon, ImGui.GetItemRectMin(), ImGui.GetItemRectMax(),
                         disabled ? Theme.Alpha(foreground, foreground.W * 0.5f) : foreground);

        if (tooltip != null) Feedback.TooltipOnHover(tooltip);

        return clicked && !disabled;
    }

    /// <summary>
    /// Dessine une icône centrée sur son dessin, et non sur sa largeur
    /// d'avance comme le fait ImGui pour un libellé de bouton : les glyphes
    /// FontAwesome n'occupent pas leur case de façon symétrique, et la
    /// corbeille paraissait décalée dans son carré.
    /// </summary>
    private static unsafe void DrawIconCentered(FontAwesomeIcon icon, Vector2 min, Vector2 max, Vector4 color)
    {
        var text   = icon.S();
        var font   = ImGui.GetFont();
        var glyph  = font.FindGlyph(text[0]);
        var center = (min + max) * 0.5f;

        // Glyphe introuvable : repli sur le centrage par la boîte du texte.
        if (glyph == null)
        {
            var size = ImGui.CalcTextSize(text);
            ImGui.GetWindowDrawList().AddText(center - size * 0.5f, ImGui.GetColorU32(color), text);
            return;
        }

        var scale = ImGui.GetFontSize() / font.FontSize;
        var left  = glyph->X0 * scale;
        var top   = glyph->Y0 * scale;
        var w     = (glyph->X1 - glyph->X0) * scale;
        var h     = (glyph->Y1 - glyph->Y0) * scale;

        var pos = new Vector2(MathF.Round(center.X - w * 0.5f - left),
                              MathF.Round(center.Y - h * 0.5f - top));
        ImGui.GetWindowDrawList().AddText(pos, ImGui.GetColorU32(color), text);
    }

    /// <summary>
    /// Progression de chaque bouton à maintenir, entre 0 et 1. -1 marque un
    /// geste abouti dont le bouton n'a pas encore été relâché : sans cette
    /// attente, garder le doigt appuyé relancerait l'action une seconde après.
    /// </summary>
    private static readonly Dictionary<uint, float> HoldProgress = [];

    /// <summary>Bouton à maintenir enfoncé sous la souris à la dernière image.</summary>
    private static readonly Dictionary<uint, bool> HoldPressed = [];

    /// <summary>
    /// Bouton destructeur qu'il faut maintenir enfoncé jusqu'à ce qu'il se
    /// remplisse. Rend vrai une seule fois, à l'instant où il est plein.
    ///
    /// Remplace le geste en deux clics, dont le second tombait souvent par
    /// réflexe : ici, lâcher avant la fin annule, et le remplissage dit
    /// d'avance combien de temps il reste.
    /// </summary>
    public static bool Hold(string label, string id,
                            FontAwesomeIcon? icon = null,
                            BtnSize size = BtnSize.Small,
                            float seconds = 1f,
                            bool disabled = false)
    {
        // Pendant qu'on maintient, le libellé devient « Maintenir… ». Le bouton
        // prend d'emblée la largeur du plus long des deux, sans quoi ses
        // voisins bougeraient sous la souris au moment d'appuyer.
        var caption = Compose(label, icon);
        var pressed = Compose(Plugin.L.HoldInProgress, icon);
        var dims    = HoldDimensions(caption, pressed, size);

        return HoldCore(caption, pressed, id, dims, seconds, disabled, null);
    }

    /// <summary>
    /// Largeur réelle d'un bouton à maintenir. À utiliser pour l'aligner :
    /// <see cref="Measure(string, FontAwesomeIcon?)"/> ne compte que son
    /// libellé, plus court que « Maintenir… », et le bouton calé à droite
    /// sortait de la carte de la différence.
    /// </summary>
    public static float MeasureHold(string label, FontAwesomeIcon? icon = null, BtnSize size = BtnSize.Small) =>
        HoldDimensions(Compose(label, icon), Compose(Plugin.L.HoldInProgress, icon), size).X;

    private static Vector2 HoldDimensions(string caption, string pressed, BtnSize size)
    {
        var widest = ImGui.CalcTextSize(pressed).X > ImGui.CalcTextSize(caption).X ? pressed : caption;
        var dims   = Dimensions(size, widest);
        if (dims.X == 0f) dims.X = Width(widest);
        return dims;
    }

    /// <summary>
    /// Variante carrée, réduite à une icône, pour une liste où un libellé
    /// prendrait trop de place. L'infobulle dit ce que fait le geste.
    /// </summary>
    public static bool HoldIcon(FontAwesomeIcon icon, string id, string tooltip,
                                float seconds = 1f, bool disabled = false)
    {
        var side = ImGui.GetFrameHeight();
        return HoldCore(icon.S(), icon.S(), id, new Vector2(side, side), seconds, disabled, tooltip, icon);
    }

    /// <param name="pressed">Libellé affiché pendant qu'on maintient le bouton.</param>
    /// <param name="iconOnly">Bouton carré : l'icône est dessinée centrée sur son dessin.</param>
    private static bool HoldCore(string caption, string pressed, string id, Vector2 dimensions,
                                 float seconds, bool disabled, string? tooltip,
                                 FontAwesomeIcon? iconOnly = null)
    {
        var key = ImGui.GetID($"hold_{id}");
        HoldProgress.TryGetValue(key, out var progress);

        // Enfoncé à l'image précédente : c'est elle qui décide du libellé,
        // l'état de celle-ci n'étant connu qu'après avoir posé le bouton.
        // L'identifiant ne dépend que de « id » (« ### ») : le libellé peut
        // changer sans que le bouton perde son état. Une fois le geste abouti,
        // le libellé d'origine revient même si l'on n'a pas encore lâché.
        HoldPressed.TryGetValue(key, out var wasPressed);
        var shown = !disabled && wasPressed && progress >= 0f ? pressed : caption;

        using (ImRaii.PushColor(ImGuiCol.Button,        Theme.Alpha(Theme.Danger, 0.08f))
                     .Push(ImGuiCol.ButtonHovered, Theme.Alpha(Theme.Danger, 0.16f))
                     .Push(ImGuiCol.ButtonActive,  Theme.Alpha(Theme.Danger, 0.16f))
                     .Push(ImGuiCol.Border,        Theme.Alpha(Theme.Danger, 0.50f))
                     .Push(ImGuiCol.Text,          Theme.DangerHover))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (ImRaii.Disabled(disabled))
            ImGui.Button(iconOnly != null ? $"###{id}" : $"{shown}###{id}", dimensions);

        // Glisser hors du bouton sans lâcher annule aussi : ImGui le garde
        // actif tant que le bouton de la souris reste enfoncé, où qu'il soit.
        var hovered = ImGui.IsItemHovered();
        var active  = !disabled && ImGui.IsItemActive();
        var held    = active && hovered;
        var min     = ImGui.GetItemRectMin();
        var max     = ImGui.GetItemRectMax();
        var delta   = ImGui.GetIO().DeltaTime;
        var fired   = false;

        if (held && progress >= 0f)
        {
            progress += delta / seconds;
            if (progress >= 1f)
            {
                fired    = true;
                progress = -1f;
            }
        }
        else if (!held && progress > 0f)
        {
            // Relâché ou quitté trop tôt : le remplissage se vide vite, plutôt
            // que de disparaître d'un coup, pour qu'on voie que le geste a été
            // annulé.
            progress = MathF.Max(0f, progress - delta * 4f);
        }
        else if (!active && progress < 0f)
        {
            // Le geste abouti ne se réarme qu'au relâchement : revenir sur le
            // bouton sans lâcher ne doit pas le relancer.
            progress = 0f;
        }

        HoldProgress[key] = progress;
        HoldPressed[key]  = held;

        if (progress > 0f)
        {
            var dl = ImGui.GetWindowDrawList();
            dl.PushClipRect(min, new Vector2(min.X + (max.X - min.X) * progress, max.Y), true);
            dl.AddRectFilled(min, max, ImGui.GetColorU32(Theme.Alpha(Theme.Danger, 0.38f)),
                             ImGui.GetStyle().FrameRounding);
            dl.PopClipRect();
        }

        if (iconOnly is { } glyph)
            DrawIconCentered(glyph, min, max,
                             disabled ? Theme.Alpha(Theme.DangerHover, 0.5f) : Theme.DangerHover);

        if (hovered && !held)
            Feedback.Tooltip(tooltip is null ? Plugin.L.HoldToConfirm : $"{tooltip}\n{Plugin.L.HoldToConfirm}");

        return fired;
    }

    /// <summary>Largeur qu'occuperait le bouton, pour centrer ou aligner à droite.</summary>
    public static float Measure(string label, FontAwesomeIcon? icon = null) =>
        Width(Compose(label, icon));

    /// <summary>
    /// Largeur qu'occuperait le bouton à cette taille, plancher compris : la
    /// petite taille en impose un, et le mesurer sans lui fait croire qu'une
    /// rangée tient sur une ligne alors qu'elle en déborde.
    /// </summary>
    public static float Measure(string label, BtnSize size, FontAwesomeIcon? icon = null)
    {
        var caption = Compose(label, icon);
        var width   = Dimensions(size, caption).X;
        return width > 0f ? width : Width(caption);
    }

    private static string Compose(string label, FontAwesomeIcon? icon) =>
        icon is { } value ? $"{value.S()}  {label}" : label;

    /// <summary>
    /// Encombrement d'un bouton.
    ///
    /// La petite taille donne une largeur régulière aux boutons courts, qui
    /// s'alignent ainsi les uns sous les autres, mais c'est un plancher et non
    /// une largeur imposée : un libellé plus long l'emporte. Fixée pour de bon,
    /// elle rognait « Voir la fiche » en « Voir la fich », et le libellé tronqué
    /// se lisait comme un bug d'affichage.
    /// </summary>
    private static Vector2 Dimensions(BtnSize size, string caption) => size switch
    {
        BtnSize.Block => new Vector2(-1f, 0f),
        BtnSize.Small => new Vector2(Math.Max(Theme.S(92f), Width(caption)), 0f),
        _             => Vector2.Zero, // largeur ajustée au contenu
    };

    /// <summary>Largeur du libellé, marges du cadre comprises.</summary>
    private static float Width(string caption) =>
        ImGui.CalcTextSize(caption).X + ImGui.GetStyle().FramePadding.X * 2f;

    /// <summary>
    /// Couleur du libellé : le brun profond sur l'or de l'action principale,
    /// sinon déduite de la luminance du fond.
    /// </summary>
    private static Vector4 TextFor(BtnTone tone, Vector4 background) =>
        tone == BtnTone.Primary ? Theme.TextOnAction : Theme.TextOn(background);

    private static (Vector4 Normal, Vector4 Hovered, Vector4 Active) Palette(BtnTone tone) => tone switch
    {
        // L'or est réservé à l'action principale : c'est le seul bouton de
        // l'écran à le porter.
        BtnTone.Primary => (Theme.Action, Theme.ActionHover, Theme.ActionActive),
        BtnTone.Danger  => (Theme.Danger, Theme.DangerHover, Theme.Mix(Theme.Danger, Theme.BgBase, 0.3f)),
        BtnTone.Success => (Theme.Online, Theme.Mix(Theme.Online, Theme.Text, 0.2f),
                            Theme.Mix(Theme.Online, Theme.BgBase, 0.3f)),
        BtnTone.Ghost   => (Vector4.Zero, Theme.Mix(Theme.BgRaised, Theme.Accent, 0.18f), Theme.BgHover),
        _               => (Theme.BgRaised, Theme.Mix(Theme.BgHover, Theme.Accent, 0.15f), Theme.BgSurface),
    };
}

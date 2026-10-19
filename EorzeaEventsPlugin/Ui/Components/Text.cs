using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Typographie. Chaque niveau encapsule sa police et sa couleur, pour que les
/// appelants n'aient jamais à gérer un push de police à la main.
///
/// Point important : <c>CalcTextSize</c> et les métriques de ligne dépendent de
/// la police courante. Toute mesure doit donc se faire à l'intérieur du scope
/// de police, ce que ces helpers garantissent.
/// </summary>
internal static class Text
{
    /// <summary>Titre de page, en H1. Or par défaut, comme les lettres du logo.</summary>
    public static void Title(string text, Vector4? color = null)
    {
        using var _ = Fonts.PushH1();
        ImGui.TextColored(color ?? Theme.TitleGold, Glyphs.Safe(text));
    }

    /// <summary>
    /// Nom d'un personnage en tête de sa fiche RP, dans la police de la marque :
    /// avec le logo, le seul usage de Cinzel.
    /// </summary>
    public static void Name(string text, Vector4? color = null)
    {
        using var _ = Fonts.PushTitle();
        ImGui.TextColored(color ?? Theme.TitleGold, Glyphs.Safe(text));
    }

    /// <summary>
    /// Titre d'une fiche (actualité, événement, lieu), en H1 replié sur la
    /// largeur utile : un titre long, coupé net au bord, perdait sa fin sans
    /// indice.
    /// </summary>
    public static void TitleWrapped(string text, Vector4? color = null)
    {
        using var _ = Fonts.PushH1();
        Wrap(text, color ?? Theme.TitleGold);
    }

    /// <summary>Titre d'une carte de liste, en H2 replié.</summary>
    public static void CardTitle(string text, Vector4? color = null)
    {
        using var _ = Fonts.PushH2();
        Wrap(text, color ?? Theme.Text);
    }

    private static void Wrap(string text, Vector4 color)
    {
        // Card ne crée pas de fenêtre enfant : le repli se calcule sur son bord
        // intérieur, comme dans WithIcon.
        var wrapAt = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Card.RightInset;
        ImGui.PushTextWrapPos(wrapAt);
        ImGui.TextColored(color, Glyphs.Safe(text));
        ImGui.PopTextWrapPos();
    }

    /// <summary>En-tête de section.</summary>
    public static void H2(string text, Vector4? color = null)
    {
        using var _ = Fonts.PushH2();
        ImGui.TextColored(color ?? Theme.Text, Glyphs.Safe(text));
    }

    public static void Body(string text, Vector4? color = null) =>
        ImGui.TextColored(color ?? Theme.Text, Glyphs.Safe(text));

    public static void Muted(string text) => ImGui.TextColored(Theme.TextMuted, Glyphs.Safe(text));

    public static void Faint(string text) => ImGui.TextColored(Theme.TextFaint, text);

    public static void Small(string text, Vector4? color = null)
    {
        using var _ = Fonts.PushSmall();
        ImGui.TextColored(color ?? Theme.TextMuted, Glyphs.Safe(text));
    }

    /// <summary>
    /// Explication, aide ou message sous un réglage : taille du corps, gris
    /// lisible, repliée sur la largeur utile. En petit (<see cref="Small"/>),
    /// ces textes passaient inaperçus ; le petit reste aux métadonnées (monde,
    /// compteur, version).
    /// </summary>
    public static void Hint(string text, Vector4? color = null)
    {
        var wrapAt = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Card.RightInset;
        ImGui.PushTextWrapPos(wrapAt);
        ImGui.TextColored(color ?? Theme.TextMuted, Glyphs.Safe(text));
        ImGui.PopTextWrapPos();
    }

    /// <summary>Texte à la ligne automatique sur la largeur disponible.</summary>
    public static void Wrapped(string text, Vector4? color = null)
    {
        ImGui.PushTextWrapPos(0f);
        ImGui.TextColored(color ?? Theme.Text, Glyphs.Safe(text));
        ImGui.PopTextWrapPos();
    }

    /// <summary>
    /// Icône suivie d'un libellé sur la même ligne. FontAwesome étant fusionné
    /// dans la police de corps, aucune bascule de police n'est nécessaire.
    /// </summary>
    /// <param name="wrap">
    /// Replie le texte plutôt que de le laisser filer. À activer dès que le
    /// contenu vient du jeu ou d'une saisie : un nom de zone ou une accroche
    /// longue serait sinon coupé net au bord de la carte, sans ellipse ni indice
    /// qu'il manque quelque chose.
    /// </param>
    public static void WithIcon(FontAwesomeIcon icon, string text,
                                Vector4? iconColor = null, Vector4? textColor = null,
                                bool wrap = false)
    {
        ImGui.TextColored(iconColor ?? Theme.TextMuted, icon.S());
        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        if (!wrap)
        {
            ImGui.TextColored(textColor ?? Theme.Text, Glyphs.Safe(text));
            return;
        }

        // Card ne crée pas de fenêtre enfant : il tient un RightInset que les
        // appelants retranchent eux-mêmes. Le point de repli se calcule donc sur
        // le bord intérieur de la carte et non sur celui de la fenêtre, sinon le
        // texte déborderait sur le padding.
        var wrapAt = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Card.RightInset;

        ImGui.PushTextWrapPos(wrapAt);
        ImGui.TextColored(textColor ?? Theme.Text, Glyphs.Safe(text));
        ImGui.PopTextWrapPos();
    }

    /// <summary>Icône seule.</summary>
    public static void Icon(FontAwesomeIcon icon, Vector4? color = null) =>
        ImGui.TextColored(color ?? Theme.TextMuted, icon.S());

    /// <summary>
    /// Texte barré, pour les événements annulés. ImGui n'a pas de style barré :
    /// la ligne est tracée à la main au milieu de la hauteur de texte.
    /// </summary>
    public static void Strikethrough(string text, Vector4? color = null)
    {
        var safe = Glyphs.Safe(text);
        var p0   = ImGui.GetCursorScreenPos();
        ImGui.TextColored(color ?? Theme.TextMuted, safe);

        var size = ImGui.CalcTextSize(safe);
        var y    = p0.Y + size.Y * 0.55f;
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(p0.X, y),
            new Vector2(p0.X + size.X, y),
            ImGui.GetColorU32(color ?? Theme.TextMuted),
            Theme.S(1f));
    }

    /// <summary>Lien cliquable, souligné au survol. Retourne true si cliqué.</summary>
    public static bool Link(string text, string? tooltip = null)
    {
        var p0 = ImGui.GetCursorScreenPos();
        ImGui.TextColored(Theme.Link, text);

        var hovered = ImGui.IsItemHovered();
        if (hovered)
        {
            var size = ImGui.CalcTextSize(text);
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(p0.X, p0.Y + size.Y),
                new Vector2(p0.X + size.X, p0.Y + size.Y),
                ImGui.GetColorU32(Theme.Link),
                Theme.S(1f));
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (tooltip != null) Feedback.Tooltip(tooltip);
        }

        return hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }
}

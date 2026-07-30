using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>
/// Classe de base des fenêtres du plugin : applique le thème avant que Dalamud
/// n'appelle <c>ImGui.Begin</c>, et le dépile après <c>ImGui.End</c>.
///
/// Pousser le style depuis <c>Draw()</c> serait sans effet sur le fond, les
/// arrondis et les bordures de la fenêtre : <c>Begin</c> a déjà été appelé.
///
/// Le style n'est jamais écrit dans <c>ImGui.GetStyle()</c>, qui est partagé
/// avec Dalamud et tous les autres plugins.
/// </summary>
public abstract class ThemedWindow : Window
{
    private readonly ThemeStack _stack = new();

    /// <summary>
    /// Portée de la police de corps, poussée pour toute la durée du rendu.
    /// Stockée en champ car <c>PreDraw</c> et <c>PostDraw</c> sont deux appels
    /// distincts. Vaut une portée vide si l'atlas n'est pas encore construit.
    /// </summary>
    private IDisposable? _fontScope;

    /// <summary>
    /// <c>true</c> pour une fenêtre à chrome custom (barre de titre maison) :
    /// le padding de fenêtre tombe à zéro pour que le shell peigne bord à bord.
    /// </summary>
    protected virtual bool Chromeless => false;

    /// <summary>
    /// Contraintes de taille exprimées en pixels logiques (à 100 % d'échelle).
    /// Elles sont remises à l'échelle Dalamud à chaque frame, ce que ne fait pas
    /// <see cref="Window.SizeConstraints"/> : une contrainte figée au
    /// constructeur tronque le contenu dès que l'utilisateur passe à 150 %.
    /// </summary>
    protected WindowSizeConstraints? LogicalSizeConstraints { get; set; }

    /// <summary>
    /// Opacité du fond de fenêtre. Légèrement translucide, avec le flou natif
    /// de Dalamud derrière : le décor du jeu transparaît sans nuire à la
    /// lisibilité du texte.
    /// </summary>
    protected virtual float BackgroundOpacity => 0.94f;

    protected ThemedWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, flags)
    {
        AllowBackgroundBlur = true;
    }

    public override void PreDraw()
    {
        // Avant les métriques : les contraintes et le layout se calculent avec
        // la police effectivement utilisée pour le rendu.
        _fontScope = Fonts.PushBody();

        if (LogicalSizeConstraints is { } logical)
        {
            var scale = ImGuiHelpers.GlobalScaleSafe;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = logical.MinimumSize * scale,
                MaximumSize = logical.MaximumSize * scale,
            };
        }

        _stack
            // ─── Arrondis ─────────────────────────────────────────────────────
            .Var(ImGuiStyleVar.WindowRounding,    Theme.S(Theme.RadiusWindow))
            .Var(ImGuiStyleVar.ChildRounding,     Theme.S(Theme.RadiusCard))
            .Var(ImGuiStyleVar.FrameRounding,     Theme.S(Theme.RadiusFrame))
            .Var(ImGuiStyleVar.PopupRounding,     Theme.S(Theme.RadiusCard))
            .Var(ImGuiStyleVar.ScrollbarRounding, Theme.S(8f))
            .Var(ImGuiStyleVar.GrabRounding,      Theme.S(Theme.RadiusFrame))
            .Var(ImGuiStyleVar.TabRounding,       Theme.S(Theme.RadiusFrame))

            // ─── Bordures ─────────────────────────────────────────────────────
            .Var(ImGuiStyleVar.WindowBorderSize, 1f)
            .Var(ImGuiStyleVar.ChildBorderSize,  0f)
            // Bordure sur les champs : sans elle, une saisie enfoncée sur fond
            // sombre n'a aucun contour et se confond avec le panneau.
            .Var(ImGuiStyleVar.FrameBorderSize,  1f)
            .Var(ImGuiStyleVar.PopupBorderSize,  1f)

            // ─── Espacements ──────────────────────────────────────────────────
            .Var(ImGuiStyleVar.WindowPadding, Chromeless
                                                  ? Vector2.Zero
                                                  : Theme.S(Theme.PadWindowX, Theme.PadWindowY))
            .Var(ImGuiStyleVar.FramePadding,     Theme.S(11f, 6f))
            .Var(ImGuiStyleVar.ItemSpacing,      Theme.S(Theme.GapM, 5f))
            .Var(ImGuiStyleVar.ItemInnerSpacing, Theme.S(Theme.GapS, 4f))
            .Var(ImGuiStyleVar.CellPadding,      Theme.S(Theme.GapM, Theme.GapS))
            .Var(ImGuiStyleVar.IndentSpacing,    Theme.S(18f))
            .Var(ImGuiStyleVar.ScrollbarSize,    Theme.S(10f))
            .Var(ImGuiStyleVar.GrabMinSize,      Theme.S(14f))

            // ─── Alignements ──────────────────────────────────────────────────
            .Var(ImGuiStyleVar.WindowTitleAlign,    new Vector2(0f,   0.5f))
            .Var(ImGuiStyleVar.ButtonTextAlign,     new Vector2(0.5f, 0.5f))
            .Var(ImGuiStyleVar.SelectableTextAlign, new Vector2(0f,   0.5f))

            // ─── Fonds et bordures ────────────────────────────────────────────
            // Seul le fond de fenêtre est translucide : les cartes et les
            // panneaux restent opaques, sans quoi le décor du jeu remonterait
            // derrière le texte et le rendrait illisible.
            .Color(ImGuiCol.WindowBg,     Theme.Alpha(Theme.BgBase, BackgroundOpacity))
            .Color(ImGuiCol.ChildBg,      Vector4.Zero)
            .Color(ImGuiCol.PopupBg,      Theme.BgSurface)
            .Color(ImGuiCol.Border,       Theme.BorderSoft)
            .Color(ImGuiCol.BorderShadow, Vector4.Zero)

            // ─── Texte ────────────────────────────────────────────────────────
            .Color(ImGuiCol.Text,           Theme.Text)
            .Color(ImGuiCol.TextDisabled,   Theme.TextMuted)
            .Color(ImGuiCol.TextSelectedBg, Theme.Alpha(Theme.Accent, 0.40f))

            // ─── Champs de saisie ─────────────────────────────────────────────
            .Color(ImGuiCol.FrameBg,        Theme.BgSunken)
            .Color(ImGuiCol.FrameBgHovered, Theme.BgSurface)
            .Color(ImGuiCol.FrameBgActive,  Theme.BgRaised)

            // ─── Barre de titre native (masquée sur le shell) ─────────────────
            .Color(ImGuiCol.TitleBg,          Theme.BgSidebar)
            .Color(ImGuiCol.TitleBgActive,    Theme.BgSidebar)
            .Color(ImGuiCol.TitleBgCollapsed, Theme.BgSidebar)
            .Color(ImGuiCol.MenuBarBg,        Theme.BgSidebar)

            // ─── Ascenseurs ───────────────────────────────────────────────────
            .Color(ImGuiCol.ScrollbarBg,          Vector4.Zero)
            .Color(ImGuiCol.ScrollbarGrab,        Theme.Alpha(Theme.BorderLight, 0.60f))
            .Color(ImGuiCol.ScrollbarGrabHovered, Theme.BorderLight)
            .Color(ImGuiCol.ScrollbarGrabActive,  Theme.TextMuted)

            // ─── Contrôles ────────────────────────────────────────────────────
            .Color(ImGuiCol.CheckMark,        Theme.Accent)
            .Color(ImGuiCol.SliderGrab,       Theme.Accent)
            .Color(ImGuiCol.SliderGrabActive, Theme.AccentHover)
            .Color(ImGuiCol.Button,           Theme.BgRaised)
            .Color(ImGuiCol.ButtonHovered,    Theme.BgHover)
            .Color(ImGuiCol.ButtonActive,     Theme.BgSurface)
            .Color(ImGuiCol.Header,           Theme.BgSurface)
            .Color(ImGuiCol.HeaderHovered,    Theme.BgRaised)
            .Color(ImGuiCol.HeaderActive,     Theme.BgHover)

            // ─── Séparateurs et poignées ──────────────────────────────────────
            .Color(ImGuiCol.Separator,         Theme.BorderSoft)
            .Color(ImGuiCol.SeparatorHovered,  Theme.BorderLight)
            .Color(ImGuiCol.SeparatorActive,   Theme.Accent)
            .Color(ImGuiCol.ResizeGrip,        Theme.Alpha(Theme.BorderLight, 0.35f))
            .Color(ImGuiCol.ResizeGripHovered, Theme.Alpha(Theme.Accent, 0.70f))
            .Color(ImGuiCol.ResizeGripActive,  Theme.Accent)

            // ─── Onglets (résiduels, le shell n'en a plus) ────────────────────
            .Color(ImGuiCol.Tab,        Theme.BgSunken)
            .Color(ImGuiCol.TabHovered, Theme.BgRaised)
            .Color(ImGuiCol.TabActive,  Theme.BgSurface)

            // ─── Tables ───────────────────────────────────────────────────────
            .Color(ImGuiCol.TableHeaderBg,     Theme.BgSurface)
            .Color(ImGuiCol.TableBorderStrong, Theme.Border)
            .Color(ImGuiCol.TableBorderLight,  Theme.Alpha(Theme.Border, 0.50f))
            .Color(ImGuiCol.TableRowBg,        Vector4.Zero)
            .Color(ImGuiCol.TableRowBgAlt,     Theme.Alpha(Theme.BgSurface, 0.5f))

            .Color(ImGuiCol.DragDropTarget, Theme.Gold);
    }

    public override void PostDraw()
    {
        // Ordre inverse du push.
        _stack.PopAll();
        _fontScope?.Dispose();
        _fontScope = null;
    }
}

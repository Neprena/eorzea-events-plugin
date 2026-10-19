using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
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
    /// Opacité du fond de fenêtre, réglable dans les paramètres. Translucide,
    /// avec le flou natif de Dalamud derrière : le décor du jeu transparaît sans
    /// nuire à la lisibilité du texte.
    /// </summary>
    protected virtual float BackgroundOpacity => Math.Clamp(Plugin.Config.WindowOpacity, 0.4f, 1f);

    /// <summary>
    /// Intensité du flou à 100 %, le plafond de Dalamud. Le défaut en prend
    /// un dixième : vu en jeu le 19 octobre, dès 8 ou 12 le décor se moyennait
    /// en une teinte sombre presque uniforme, et la fenêtre paraissait opaque.
    /// </summary>
    private const float MaxBlur = 14f;

    protected ThemedWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, flags | ImGuiWindowFlags.NoCollapse)
    {
        // Le flou de Dalamud est coupé : le plugin pose le sien, voir PaintNight.
        AllowBackgroundBlur = false;

        // Boutons de titre redessinés plutôt que natifs.
        //
        // ImGui pose le bouton de repli et la croix l'un contre l'autre, sans
        // espacement réglable : élargir ItemInnerSpacing n'écartait que le bouton
        // de menu, celui que Dalamud ajoute, d'où un rendu bancal. Dalamud espace
        // en revanche correctement les boutons qu'on lui fournit. On retire donc
        // le repli, dont personne ne se sert en jeu, et on redessine la
        // fermeture.
        ShowCloseButton = false;
        TitleBarButtons.Add(new TitleBarButton
        {
            Icon       = FontAwesomeIcon.Times,
            IconOffset = new Vector2(1.5f, 1f),
            Click      = _ => OnCloseButton(),
        });
    }

    /// <summary>
    /// Fermeture demandée depuis la barre de titre. Les fenêtres qui ferment
    /// autrement (chrome maison, lightbox) la neutralisent.
    /// </summary>
    protected virtual void OnCloseButton() => IsOpen = false;

    /// <summary>
    /// Peint la nuit, puis le contenu. Scellé : c'est ce qui garantit que toutes
    /// les fenêtres du plugin portent le même fond, sans que chacune ait à
    /// penser à l'appeler.
    /// </summary>
    public sealed override void Draw()
    {
        PaintNight();
        DrawContents();
    }

    /// <summary>Le contenu de la fenêtre, dessiné sur la nuit.</summary>
    protected abstract void DrawContents();

    /// <summary>
    /// Fond de la fenêtre. Le fond natif d'ImGui est transparent : c'est cette
    /// nuit, peinte au début de chaque image, qui porte l'opacité.
    /// </summary>
    private void PaintNight()
    {
        var position = ImGui.GetWindowPos();
        var size     = ImGui.GetWindowSize();
        var dl       = ImGui.GetWindowDrawList();

        // Sous la barre de titre native quand elle existe : elle est dessinée
        // avant le contenu dans la même liste, la recouvrir effacerait le titre.
        var titled = (Flags & ImGuiWindowFlags.NoTitleBar) == 0;
        var top    = titled ? ImGui.GetFrameHeight() : 0f;

        // ImGui a déjà dessiné l'ascenseur dans la même liste : la nuit ne couvre
        // pas sa colonne, sans quoi une fenêtre qui défile garde un ascenseur
        // invisible et son contenu semble simplement coupé. Repris de Linkpearl.
        var scrolls   = (Flags & ImGuiWindowFlags.NoScrollbar) == 0 && ImGui.GetScrollMaxY() > 0f;
        var scrollbar = scrolls ? ImGui.GetStyle().ScrollbarSize : 0f;
        var rounding  = Theme.S(Theme.RadiusWindow);

        // Le flou du décor derrière la fenêtre. Celui de Dalamud dépend du
        // style choisi dans /xlstyle, qui le met à zéro dans bien des styles
        // personnalisés : le plugin pose donc le sien, réglable dans les
        // paramètres. Inséré en tête de la liste, il passe sous tout le reste.
        // Seulement dans la fenêtre du jeu : hors d'elle, il n'y a rien à
        // flouter.
        var blur = Math.Clamp(Plugin.Config.WindowBlur, 0f, 1f);
        if (blur > 0f && ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID)
            ImGuiHelpers.PrependBlurBehind(dl, position, position + size, blur * MaxBlur, rounding,
                                           tintColor: Vector4.Zero, luminosityColor: Vector4.Zero,
                                           noiseOpacity: 0.03f);

        // Hors de la zone de contenu : la liste de la fenêtre est rognée en deçà
        // des marges, et le fond n'irait pas jusqu'au bord.
        dl.PushClipRect(position, position + size, false);
        Surface.NightBackground(dl, position + new Vector2(0f, top), position + size - new Vector2(scrollbar, 0f),
                                rounding, roundTop: !titled, BackgroundOpacity);
        dl.AddRect(position, position + size, ImGui.GetColorU32(Theme.BorderSoft), rounding, ImDrawFlags.None,
                   ImGui.GetStyle().WindowBorderSize);

        // ImGui a dessiné la poignée de redimensionnement dans Begin, sous la
        // nuit : on la retrace par-dessus, sans quoi rien ne dit plus qu'on peut
        // tirer le coin.
        if ((Flags & (ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) == 0)
        {
            var corner  = position + size - new Vector2(rounding * 0.3f);
            var grip    = Theme.S(13f);
            var hovered = ImGui.IsMouseHoveringRect(corner - new Vector2(grip, grip), corner + Vector2.One);
            dl.AddTriangleFilled(corner - new Vector2(grip, 0f), corner, corner - new Vector2(0f, grip),
                                 ImGui.GetColorU32(hovered ? Theme.Alpha(Theme.Accent, 0.70f)
                                                           : Theme.BorderLight));
        }

        dl.PopClipRect();
    }

    public override void PreDraw()
    {
        // Avant les métriques : les contraintes et le layout se calculent avec
        // la police effectivement utilisée pour le rendu.
        _fontScope = Fonts.PushBody();

        if (LogicalSizeConstraints is { } logical)
        {
            var scale = ImGuiHelpers.GlobalScale;
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
            // Sépare aussi les boutons de la barre de titre, ceux que Dalamud
            // dessine : c'est ce qui leur donne de l'air.
            .Var(ImGuiStyleVar.ItemInnerSpacing, Theme.S(Theme.GapL, 4f))
            .Var(ImGuiStyleVar.CellPadding,      Theme.S(Theme.GapM, Theme.GapS))
            .Var(ImGuiStyleVar.IndentSpacing,    Theme.S(18f))
            // Plus large que la barre visible : ImGui inset la poignée de
            // 3 px de chaque côté dans sa piste, qui reste transparente. On
            // obtient une barre fine de 6 px qui flotte, avec de l'air autour.
            .Var(ImGuiStyleVar.ScrollbarSize,    Theme.S(12f))
            .Var(ImGuiStyleVar.GrabMinSize,      Theme.S(14f))

            // ─── Alignements ──────────────────────────────────────────────────
            .Var(ImGuiStyleVar.WindowTitleAlign,    new Vector2(0f,   0.5f))
            .Var(ImGuiStyleVar.ButtonTextAlign,     new Vector2(0.5f, 0.5f))
            .Var(ImGuiStyleVar.SelectableTextAlign, new Vector2(0f,   0.5f))

            // ─── Fonds et bordures ────────────────────────────────────────────
            // Le fond natif s'efface presque : c'est la nuit, peinte au début de
            // Draw, qui porte l'opacité. Les cartes posées dessus sont
            // translucides à leur tour et laissent voir le halo ; elles ne
            // descendent jamais sous l'opacité de la fenêtre, puisqu'elles s'y
            // ajoutent.
            //
            // Presque, et pas tout à fait : Dalamud ignore les fenêtres dont ce
            // fond est entièrement transparent, et un réglage de flou qui
            // reviendrait par lui n'aurait alors aucun effet.
            .Color(ImGuiCol.WindowBg,     Theme.Alpha(Theme.BgBase, 0.02f))
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
            // Discrète au repos, plus claire au survol, cristal quand on la
            // tire : elle ne doit pas concurrencer les filets dorés des cartes.
            .Color(ImGuiCol.ScrollbarGrab,        Theme.Alpha(Theme.Text, 0.16f))
            .Color(ImGuiCol.ScrollbarGrabHovered, Theme.Alpha(Theme.Text, 0.32f))
            .Color(ImGuiCol.ScrollbarGrabActive,  Theme.Alpha(Theme.Accent, 0.80f))

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
            .Color(ImGuiCol.ResizeGrip,        Theme.Border)
            .Color(ImGuiCol.ResizeGripHovered, Theme.Alpha(Theme.Accent, 0.70f))
            .Color(ImGuiCol.ResizeGripActive,  Theme.Accent)

            // ─── Onglets (résiduels, le shell n'en a plus) ────────────────────
            .Color(ImGuiCol.Tab,        Theme.BgSunken)
            .Color(ImGuiCol.TabHovered, Theme.BgRaised)
            .Color(ImGuiCol.TabActive,  Theme.BgSurface)

            // ─── Tables ───────────────────────────────────────────────────────
            .Color(ImGuiCol.TableHeaderBg,     Theme.BgSurface)
            .Color(ImGuiCol.TableBorderStrong, Theme.Border)
            .Color(ImGuiCol.TableBorderLight,  Theme.BorderSoft)
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

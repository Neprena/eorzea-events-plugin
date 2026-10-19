using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>
/// Coque de la fenêtre principale : barre de titre maison, navigation latérale,
/// zone de contenu et barre de statut.
///
/// La fenêtre hôte doit être déclarée sans chrome (<c>NoTitleBar</c>) et sans
/// marge intérieure, le shell peignant lui-même bord à bord.
///
///     ┌────────────────────────────────────────┐
///     │ ◉ Eorzea Events               ↗  —  ✕  │  barre de titre, déplaçable
///     ├──────────┬─────────────────────────────┤
///     │ JOUER    │                             │
///     │ ico  RP  │  contenu de la page         │
///     │ ico  …  ┃│                             │  ┃ poignée de largeur
///     │ ⚙        │                             │
///     ├──────────┴─────────────────────────────┤
///     │ ● Compte lié · Nom          28 en ligne│
///     └────────────────────────────────────────┘
///
/// Réduite, la fenêtre ne garde que sa barre de titre, qui gagne alors
/// l'interrupteur de disponibilité et le compteur « autour de moi ».
/// </summary>
internal sealed class AppShell
{
    private readonly List<ShellPage> _pages;
    private string _activeId;

    public AppShell(IEnumerable<ShellPage> pages, string initialId)
    {
        _pages    = [.. pages];
        _activeId = initialId;
    }

    public string ActiveId => _activeId;

    /// <summary>
    /// Bandeaux posés en tête du contenu, au-dessus de la page active : alerte
    /// de l'équipe, actualité épinglée. Absent des écrans bloquants. Reçoit
    /// l'identifiant de la page active.
    /// </summary>
    public Action<string>? Banner { get; init; }

    public void Navigate(string pageId)
    {
        if (_pages.Exists(p => p.Id == pageId)) _activeId = pageId;
    }

    /// <summary>
    /// Dessine la fenêtre entière. <paramref name="fullScreen"/> permet aux
    /// écrans bloquants (version obsolète, jeton invalide) de court-circuiter
    /// la navigation tout en conservant la barre de titre.
    /// </summary>
    /// <param name="folded">
    /// La fenêtre est réduite à sa barre de titre, ou en train de l'être : seule
    /// la barre est dessinée, le reste n'aurait de toute façon pas la place.
    /// </param>
    public TitleAction Draw(bool folded = false, Action? fullScreen = null)
    {
        var available = ImGui.GetContentRegionAvail();

        var action = TitleBar.Draw(available.X, folded);

        var bodyHeight = available.Y
                         - Theme.S(Theme.TitleBarHeight)
                         - Theme.S(Theme.StatusBarHeight);

        // Pendant que la fenêtre se replie, la place manque : un enfant de
        // hauteur nulle ou négative serait pris par ImGui pour « tout le reste ».
        if (folded || bodyHeight < Theme.S(60f))
        {
            // Valide la position où la barre a laissé le curseur : ImGui refuse
            // qu'un SetCursorPos étende la fenêtre sans élément derrière.
            ImGui.Dummy(Vector2.Zero);
            return action;
        }

        if (fullScreen != null)
        {
            DrawContent("##shellfull", available.X, bodyHeight, fullScreen);
            StatusBar.Draw();
            return action;
        }

        var clicked = Sidebar.Draw(Visible(), _activeId, bodyHeight);
        if (clicked != null)
        {
            var target = _pages.Find(p => p.Id == clicked);
            if (target?.OnSelect != null) target.OnSelect();
            else _activeId = clicked;
        }

        ImGui.SameLine(0f, 0f);

        var contentWidth = available.X - Sidebar.CurrentWidth;
        var contentMin   = ImGui.GetCursorScreenPos();

        var page = Active();
        DrawContent("##shellcontent", contentWidth, bodyHeight, page.Draw,
                    Banner == null ? null : () => Banner(page.Id));

        // La notification se pose sur le contenu, puis le curseur revient où le
        // contenu l'a laissé : la barre d'état se place d'après lui.
        var after = ImGui.GetCursorScreenPos();
        Snackbar.Draw(contentMin, contentMin + new Vector2(contentWidth, bodyHeight));
        ImGui.SetCursorScreenPos(after);

        StatusBar.Draw();
        return action;
    }

    /// <summary>
    /// Opacité des barres et de la colonne : celle de la fenêtre, relevée d'un
    /// cran. Le cadre d'une vitre est toujours plus dense que la vitre.
    /// </summary>
    public static float ChromeOpacity(float lift) =>
        Math.Clamp(Plugin.Config.WindowOpacity + lift, 0.4f, 1f);

    /// <summary>
    /// Zone de contenu, marges comprises.
    ///
    /// Les marges sont obtenues en rétrécissant l'enfant plutôt qu'en poussant
    /// <c>WindowPadding</c> : ainsi la largeur disponible mesurée à l'intérieur
    /// est déjà la bonne, et les éléments qui demandent toute la largeur ne
    /// débordent pas sous la marge droite.
    /// </summary>
    private static void DrawContent(string id, float width, float height, Action body, Action? banner = null)
    {
        var padX = Theme.S(Theme.PadWindowX);
        var padY = Theme.S(Theme.PadWindowY);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padX);

        using var child = ImRaii.Child(id, new Vector2(width - padX * 2f, height), false,
                                       ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!child) return;

        ImGui.Dummy(new Vector2(0f, padY));
        banner?.Invoke();
        body();
    }

    private List<ShellPage> Visible() =>
        _pages.FindAll(p => p.Visible?.Invoke() ?? true);

    private ShellPage Active()
    {
        var page = _pages.Find(p => p.Id == _activeId);
        if (page != null) return page;

        // La page active a disparu (masquée par la configuration) : on retombe
        // sur la première disponible.
        var fallback = Visible();
        _activeId = fallback.Count > 0 ? fallback[0].Id : _pages[0].Id;
        return _pages.Find(p => p.Id == _activeId)!;
    }
}

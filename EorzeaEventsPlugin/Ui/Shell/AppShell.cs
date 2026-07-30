using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
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
///     │ Eorzea Events                    ↗  ✕  │  barre de titre, déplaçable
///     ├──────┬─────────────────────────────────┤
///     │ ico  │                                 │
///     │ ico  │  contenu de la page             │
///     │ ico  │                                 │
///     │ ⚙    │                                 │
///     ├──────┴─────────────────────────────────┤
///     │                    ● 28 en ligne       │
///     └────────────────────────────────────────┘
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

    public void Navigate(string pageId)
    {
        if (_pages.Exists(p => p.Id == pageId)) _activeId = pageId;
    }

    /// <summary>
    /// Dessine la fenêtre entière. <paramref name="fullScreen"/> permet aux
    /// écrans bloquants (version obsolète, jeton invalide) de court-circuiter
    /// la navigation tout en conservant la barre de titre.
    /// </summary>
    public void Draw(out bool closeRequested, Action? fullScreen = null)
    {
        var available = ImGui.GetContentRegionAvail();

        closeRequested = TitleBar.Draw(available.X);

        var bodyHeight = available.Y
                         - Theme.S(Theme.TitleBarHeight)
                         - Theme.S(Theme.StatusBarHeight);

        if (fullScreen != null)
        {
            DrawContent("##shellfull", available.X, bodyHeight, fullScreen);
            StatusBar.Draw();
            return;
        }

        var clicked = Sidebar.Draw(Visible(), _activeId, bodyHeight);
        if (clicked != null)
        {
            var target = _pages.Find(p => p.Id == clicked);
            if (target?.OnSelect != null) target.OnSelect();
            else _activeId = clicked;
        }

        ImGui.SameLine(0f, 0f);

        DrawContent("##shellcontent",
                    available.X - Theme.S(Theme.SidebarWidth),
                    bodyHeight,
                    Active().Draw);

        StatusBar.Draw();
    }

    /// <summary>
    /// Zone de contenu, marges comprises.
    ///
    /// Les marges sont obtenues en rétrécissant l'enfant plutôt qu'en poussant
    /// <c>WindowPadding</c> : ainsi la largeur disponible mesurée à l'intérieur
    /// est déjà la bonne, et les éléments qui demandent toute la largeur ne
    /// débordent pas sous la marge droite.
    /// </summary>
    private static void DrawContent(string id, float width, float height, Action body)
    {
        var padX = Theme.S(Theme.PadWindowX);
        var padY = Theme.S(Theme.PadWindowY);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padX);

        using var child = ImRaii.Child(id, new Vector2(width - padX * 2f, height), false,
                                       ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!child) return;

        ImGui.Dummy(new Vector2(0f, padY));
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

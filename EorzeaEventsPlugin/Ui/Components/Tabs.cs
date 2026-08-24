using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Barre d'onglets horizontale, pour découper une page trop longue.
///
/// Écrite plutôt que reprise de `ImGui.BeginTabBar` : les onglets natifs
/// ignorent la palette du plugin, et leur contenu doit être dessiné entre un
/// Begin et un End, ce qui obligerait à imbriquer la page dans la barre. Ici la
/// barre ne fait que rendre l'onglet actif, l'appelant garde la main sur ce
/// qu'il dessine ensuite.
/// </summary>
internal static class Tabs
{
    /// <summary>Un onglet : son identifiant stable, son libellé et son icône.</summary>
    internal readonly record struct Tab(string Id, string Label, FontAwesomeIcon Icon);

    /// <summary>
    /// Dessine la barre et renvoie l'identifiant de l'onglet actif.
    ///
    /// L'identifiant courant est passé et renvoyé plutôt que retenu ici : la
    /// barre est sans état, et la page qui l'utilise garde le sien, ce qui lui
    /// permet de changer d'onglet elle-même (au chargement, sur un lien).
    ///
    /// Un identifiant inconnu retombe sur le premier onglet, jamais sur un écran
    /// vide : une version future qui renommerait un onglet trouverait sinon la
    /// page blanche à la réouverture.
    /// </summary>
    public static string Draw(string id, IReadOnlyList<Tab> tabs, string current, Vector4 accent)
    {
        if (tabs.Count == 0) return current;

        var active = current;
        var known  = false;
        foreach (var tab in tabs)
            if (tab.Id == active) { known = true; break; }
        if (!known) active = tabs[0].Id;

        using var scope = ImRaii.PushId(id);

        // Onglets jointifs, comme les segments d'un même ruban : c'est ce qui les
        // distingue d'une rangée de boutons posés côte à côte.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing,
                                           new Vector2(Theme.S(2f), 0f))
                                .Push(ImGuiStyleVar.FrameRounding, Theme.S(Theme.RadiusFrame));

        for (var i = 0; i < tabs.Count; i++)
        {
            if (i > 0) ImGui.SameLine();

            var tab      = tabs[i];
            var selected = tab.Id == active;

            // L'onglet actif porte l'accent de la fiche, les autres restent
            // effacés : la couleur dit où l'on est, sans avoir à lire.
            using var color = selected
                ? ImRaii.PushColor(ImGuiCol.Button, Theme.Alpha(accent, 0.22f))
                        .Push(ImGuiCol.ButtonHovered, Theme.Alpha(accent, 0.30f))
                        .Push(ImGuiCol.ButtonActive, Theme.Alpha(accent, 0.38f))
                        .Push(ImGuiCol.Text, accent)
                : ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero)
                        .Push(ImGuiCol.ButtonHovered, Theme.Highlight)
                        .Push(ImGuiCol.ButtonActive, Theme.BgRaised)
                        .Push(ImGuiCol.Text, Theme.TextMuted);

            if (ImGui.Button($"{tab.Icon.S()}  {tab.Label}##tab_{tab.Id}"))
                active = tab.Id;
        }

        return active;
    }
}

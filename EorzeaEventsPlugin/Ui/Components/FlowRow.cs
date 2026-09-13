using Dalamud.Bindings.ImGui;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Rangée d'éléments qui revient à la ligne quand la place manque.
///
/// Le réflexe est de comparer l'abscisse du curseur à la limite avant d'appeler
/// <c>SameLine</c>. Il ne marche pas : après un élément, le curseur est déjà
/// revenu en début de ligne suivante, la comparaison répond toujours « ça
/// tient », et la rangée reste sur une seule ligne qui sort de la carte par la
/// droite. C'est le bord droit du dernier élément dessiné qui dit où l'on en
/// est, et il se lit en coordonnées écran.
/// </summary>
internal sealed class FlowRow
{
    private readonly float _gap;
    private readonly float _limit;
    private bool _started;

    /// <summary>
    /// À construire quand le curseur est au bord gauche de la rangée :
    /// <paramref name="width"/> est la largeur qu'elle a le droit d'occuper.
    /// </summary>
    public FlowRow(float gap, float width)
    {
        _gap   = gap;
        _limit = ImGui.GetCursorScreenPos().X + width;
    }

    /// <summary>Rangée occupant la largeur restante, marge de carte déduite.</summary>
    public static FlowRow Fill(float gap)
        => new(gap, ImGui.GetContentRegionAvail().X - Card.RightInset);

    /// <summary>
    /// Place l'élément suivant, large de <paramref name="width"/> : sur la ligne
    /// en cours s'il y tient, à la ligne suivante sinon. À appeler juste avant
    /// de le dessiner.
    /// </summary>
    public void Next(float width)
    {
        if (_started && ImGui.GetItemRectMax().X + _gap + width <= _limit)
            ImGui.SameLine(0f, _gap);

        _started = true;
    }

    /// <summary>
    /// Compte un élément déjà dessiné par ailleurs : la rangée est entamée, et
    /// le suivant peut se poser à côté de lui.
    /// </summary>
    public void Started() => _started = true;
}

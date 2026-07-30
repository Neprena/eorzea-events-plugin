using Dalamud.Interface;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>
/// Entrée de navigation du shell.
///
/// Le libellé et le badge sont des fonctions et non des valeurs : ils sont
/// évalués à chaque frame, pour que le changement de langue et les compteurs
/// se répercutent sans reconstruire la navigation.
/// </summary>
internal sealed class ShellPage
{
    public required string Id { get; init; }

    public required FontAwesomeIcon Icon { get; init; }

    /// <summary>Libellé affiché dans l'infobulle. Lu à chaque frame.</summary>
    public required Func<string> Label { get; init; }

    /// <summary>Contenu de la page.</summary>
    public required Action Draw { get; init; }

    /// <summary>
    /// Action déclenchée au clic, à la place d'un changement de page.
    /// Sert aux entrées qui ouvrent une fenêtre séparée : sans cela, leur
    /// contenu serait redessiné à chaque frame et rouvrirait la fenêtre en
    /// boucle, la rendant impossible à fermer.
    /// </summary>
    public Action? OnSelect { get; init; }

    /// <summary>Compteur affiché en pastille. Zéro masque la pastille.</summary>
    public Func<int>? Badge { get; init; }

    /// <summary>Page masquée dans la barre latérale quand ceci renvoie faux.</summary>
    public Func<bool>? Visible { get; init; }

    /// <summary>Ancrée en bas de la barre latérale, séparée des autres.</summary>
    public bool Pinned { get; init; }
}

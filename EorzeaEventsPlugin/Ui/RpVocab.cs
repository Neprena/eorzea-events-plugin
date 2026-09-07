namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Clés du vocabulaire RP, telles que le serveur les attend.
///
/// Elles recopient `src/lib/rp-vocabulary.ts` : une valeur inconnue fait
/// refuser tout l'enregistrement, pas seulement son champ. Les avoir en un seul
/// endroit évite qu'une liste soit corrigée et l'autre oubliée, ce qui est
/// exactement ce qui arrive quand une fenêtre et une page tiennent chacune la
/// leur.
///
/// Les libellés traduits vivent dans <see cref="Loc"/> et dans
/// <c>RpProfileView</c> ; ici il n'y a que des clés.
/// </summary>
internal static class RpVocab
{
    public static readonly string[] Levels     = ["beginner", "casual", "confirmed"];
    public static readonly string[] Approaches = ["come_to_me", "i_approach", "either"];

    /// <summary>Précédées d'une entrée vide : l'index 0 vaut « non précisé ».</summary>
    public static readonly string[] Races =
    [
        "", "hyur", "elezen", "lalafell", "miqote", "roegadyn", "aura",
        "hrothgar", "viera", "other",
    ];

    public static readonly string[] Themes =
    [
        "tavern", "adventure", "drama", "romance", "lore", "dark",
        "mystery", "intrigue", "combat", "craft", "slice_of_life", "politics",
    ];

    /// <summary>Plafond de RP_MAX_THEMES, appliqué aussi par le serveur.</summary>
    public const int MaxThemes = 6;
}

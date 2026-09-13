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

    /// <summary>
    /// Types de relation, dans l'ordre de RP_RELATION_KINDS.
    ///
    /// L'ordre compte : l'index choisi dans une liste sert de clé, et une liste
    /// qui diverge de celle du site enverrait « allié » là où l'utilisateur a lu
    /// « famille ».
    /// </summary>
    public static readonly string[] RelationKinds =
    [
        "ally", "friend", "family", "lover", "mentor", "student", "rival", "enemy", "other",
    ];

    /// <summary>
    /// Le type que porte la ligne d'en face, proposé par défaut à qui accepte.
    ///
    /// Recopie RP_RELATION_INVERSE : seuls mentor et élève se répondent, les
    /// autres se reflètent à l'identique. Ce n'est qu'une valeur de départ,
    /// modifiable avant d'accepter.
    /// </summary>
    public static string InverseRelationKind(string kind) => kind switch
    {
        "mentor"  => "student",
        "student" => "mentor",
        _         => Array.IndexOf(RelationKinds, kind) >= 0 ? kind : "other",
    };

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

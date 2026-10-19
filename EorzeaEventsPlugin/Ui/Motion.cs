using Dalamud.Bindings.ImGui;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Transitions douces, une valeur par élément.
///
/// Chaque élément animé (survol, repère de navigation, largeur de colonne)
/// garde sa valeur courante sous son identifiant ImGui, et la rapproche de sa
/// cible à chaque image, de façon exponentielle : la course ralentit en
/// arrivant, sans durée fixe à tenir ni état « en cours » à gérer. C'est ce qui
/// fait qu'une entrée de menu s'allume en fondu, et que le repère glisse d'une
/// entrée à l'autre au lieu d'y sauter.
///
/// Respecte le réglage « animations réduites » de Dalamud : la valeur saute
/// alors directement à sa cible.
/// </summary>
internal static class Motion
{
    /// <summary>Survols, interrupteurs : presque immédiat, mais perceptible.</summary>
    public const float Fast = 0.14f;

    /// <summary>Déplacements marqués : repère de navigation, repli d'une colonne.</summary>
    public const float Emphasised = 0.28f;

    /// <summary>
    /// Plafond du cache. Des identifiants naissent à chaque liste parcourue ;
    /// au-delà, on repart de zéro plutôt que de laisser le dictionnaire grossir
    /// toute la session. Le seul effet est une animation sautée.
    /// </summary>
    private const int MaxTracked = 512;

    private static readonly Dictionary<uint, float> Values = [];

    /// <summary>
    /// Rapproche la valeur de <paramref name="id"/> de <paramref name="target"/>.
    /// Au premier appel, la valeur part directement de sa cible : un élément qui
    /// apparaît ne doit pas s'animer depuis zéro.
    /// </summary>
    public static float Approach(uint id, float target, float duration)
    {
        if (!Values.TryGetValue(id, out var current) || Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion)
        {
            if (Values.Count >= MaxTracked) Values.Clear();
            Values[id] = target;
            return target;
        }

        if (MathF.Abs(target - current) < 0.001f)
        {
            Values[id] = target;
            return target;
        }

        // 1 - e^(-dt/τ) : indépendant de la cadence d'images. τ vaut un tiers
        // de la durée annoncée, si bien qu'à son terme il reste moins de 5 %.
        var step = duration <= 0f ? 1f : 1f - MathF.Exp(-ImGui.GetIO().DeltaTime / (duration * 0.35f));
        current = float.Lerp(current, target, Math.Clamp(step, 0f, 1f));
        Values[id] = current;
        return current;
    }

    /// <summary>Variante par libellé, résolu dans la pile d'identifiants courante.</summary>
    public static float Approach(string id, float target, float duration) =>
        Approach(ImGui.GetID(id), target, duration);

    /// <summary>Fixe une valeur sans transition, par exemple pendant qu'on tire une poignée.</summary>
    public static void Set(uint id, float value) => Values[id] = value;

    public static void Reset() => Values.Clear();
}

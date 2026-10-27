using System.Numerics;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Les couleurs que les thèmes saisonniers ont le droit de changer : la nuit,
/// ses deux halos, l'accent, l'action, l'or des titres et les filets.
///
/// Tout le reste de <see cref="Theme"/> est fixe. Les fonds opaques, le texte
/// et les statuts gardent leurs valeurs en toute saison : ce sont eux qui
/// portent la lisibilité, et un thème de quelques jours n'a pas à la remettre
/// en jeu.
/// </summary>
internal readonly record struct Palette(
    Vector4 NightTop,
    Vector4 NightBottom,
    Vector4 HaloCrystal,
    Vector4 HaloGold,
    Vector4 Accent,
    Vector4 AccentHover,
    Vector4 AccentActive,
    Vector4 Action,
    Vector4 ActionHover,
    Vector4 ActionActive,
    Vector4 TextOnAction,
    Vector4 TitleGold,
    Vector4 Border,
    Vector4 BorderSoft,
    Vector4 BorderLight,
    Vector4 Highlight);

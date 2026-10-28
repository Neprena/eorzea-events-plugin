using static EorzeaEventsPlugin.Ui.Theme;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Les palettes des thèmes, reprises des maquettes. Chacune garde la structure
/// de la Nuit dorée : une nuit, un halo au centre, une lueur plus large, un
/// accent pour l'état actif, une action pour le geste principal.
///
/// L'accent reste clair dans toutes les saisons : <see cref="Theme.TextOn"/>
/// pose alors du texte sombre dessus, comme sur le cristal.
/// </summary>
internal static class SeasonPalettes
{
    public static Palette For(Season season) => season switch
    {
        Season.NewYear      => NewYear,
        Season.Valentine    => Valentine,
        Season.Easter       => Easter,
        Season.Halloween    => Halloween,
        Season.Christmas    => Christmas,
        Season.LittleLadies => LittleLadies,
        Season.GoldSaucer   => GoldSaucer,
        Season.Moonfire     => Moonfire,
        Season.Rising       => Rising,
        _                   => GoldenNight,
    };

    /// <summary>
    /// Citrouille : halo orange au centre, lueur ambrée, accent et filets
    /// orangés sur une nuit prune. L'accent, plus clair, reste distinct de
    /// l'action, d'un orange plus profond.
    /// </summary>
    private static readonly Palette Halloween = new(
        NightTop:     Hex(0x21121F),
        NightBottom:  Hex(0x0E090D),
        HaloCrystal:  Hex(0xFF7A1A, 0.30f),
        HaloGold:     Hex(0xFFB347, 0.18f),
        Accent:       Hex(0xFFA552),
        AccentHover:  Hex(0xFFC085),
        AccentActive: Hex(0xE8842E),
        Action:       Hex(0xFF7A1A),
        ActionHover:  Hex(0xFF9440),
        ActionActive: Hex(0xE0650C),
        TextOnAction: Hex(0x2A1000),
        TitleGold:    Hex(0xFFB25C),
        Border:       Hex(0xFF8A2B, 0.28f),
        BorderSoft:   Hex(0xFF8A2B, 0.15f),
        BorderLight:  Hex(0xFF8A2B, 0.45f),
        Highlight:    Hex(0xFFB060, 0.26f));

    /// <summary>
    /// Les couleurs du sapin : nuit vert sombre, halo rouge, lueur et filets
    /// d'or, action rouge à texte clair, et l'accent givré pour l'état actif.
    ///
    /// Le survol fonce le rouge au lieu de l'éclaircir : tout rouge plus clair
    /// que le bouton passe sous 4,5:1 avec du texte clair. La nuit est réglée
    /// pour que le texte pâle y tienne ce même seuil.
    /// </summary>
    private static readonly Palette Christmas = new(
        NightTop:     Hex(0x102C26),
        NightBottom:  Hex(0x081210),
        HaloCrystal:  Hex(0xFF4F63, 0.24f),
        HaloGold:     Hex(0xFFC46B, 0.18f),
        Accent:       Hex(0x9FDFFF),
        AccentHover:  Hex(0xC2EBFF),
        AccentActive: Hex(0x74C6F0),
        Action:       Hex(0xD6293B),
        ActionHover:  Hex(0xC41F33),
        ActionActive: Hex(0xA81A2C),
        TextOnAction: Hex(0xFFF6EC),
        TitleGold:    Hex(0xFFD27A),
        Border:       Hex(0xFFC46B, 0.26f),
        BorderSoft:   Hex(0xFFC46B, 0.14f),
        BorderLight:  Hex(0xFFC46B, 0.42f),
        Highlight:    Hex(0xFFE3A3, 0.28f));

    /// <summary>Nuit lavande, halo menthe, lueur rose, action jaune poussin.</summary>
    private static readonly Palette Easter = new(
        NightTop:     Hex(0x1D1F40),
        NightBottom:  Hex(0x0E1022),
        HaloCrystal:  Hex(0x7FE3C4, 0.26f),
        HaloGold:     Hex(0xFFB3D1, 0.16f),
        Accent:       Hex(0x8EEBD0),
        AccentHover:  Hex(0xB4F3E0),
        AccentActive: Hex(0x63D3B4),
        Action:       Hex(0xF5D76E),
        ActionHover:  Hex(0xFAE48F),
        ActionActive: Hex(0xE4C24F),
        TextOnAction: Hex(0x2A2000),
        TitleGold:    Hex(0xF6DC8E),
        Border:       Hex(0xFFB3D1, 0.26f),
        BorderSoft:   Hex(0xFFB3D1, 0.14f),
        BorderLight:  Hex(0xFFB3D1, 0.42f),
        Highlight:    Hex(0xFFCDE1, 0.26f));

    /// <summary>Nuit d'encre, halo argenté, or plus présent.</summary>
    private static readonly Palette NewYear = GoldenNight with
    {
        NightTop     = Hex(0x15142E),
        NightBottom  = Hex(0x0B0B1A),
        HaloCrystal  = Hex(0xC8D2FF, 0.22f),
        HaloGold     = Hex(0xE9BE5C, 0.22f),
        Accent       = Hex(0xC9D4FF),
        AccentHover  = Hex(0xDEE4FF),
        AccentActive = Hex(0xA9B7F0),
        TitleGold    = Hex(0xF0CF78),
        Border       = Hex(0xE9BE5C, 0.30f),
        BorderSoft   = Hex(0xE9BE5C, 0.16f),
        BorderLight  = Hex(0xE9BE5C, 0.46f),
        Highlight    = Hex(0xFFE096, 0.30f),
    };

    /// <summary>
    /// La Valentione : nuit lie-de-vin, halo rose, lueur et titres or rose.
    /// L'accent, d'un rose pâle, reste distinct de l'action, plus soutenue.
    /// </summary>
    private static readonly Palette Valentine = new(
        NightTop:     Hex(0x2A1328),
        NightBottom:  Hex(0x120911),
        HaloCrystal:  Hex(0xFF5C8A, 0.26f),
        HaloGold:     Hex(0xFFC2A8, 0.14f),
        Accent:       Hex(0xFFB3CB),
        AccentHover:  Hex(0xFFCCDC),
        AccentActive: Hex(0xF28FB0),
        Action:       Hex(0xFF6B98),
        ActionHover:  Hex(0xFF85AA),
        ActionActive: Hex(0xE85483),
        TextOnAction: Hex(0x2A0714),
        TitleGold:    Hex(0xFFC9A3),
        Border:       Hex(0xFF7FA8, 0.26f),
        BorderSoft:   Hex(0xFF7FA8, 0.14f),
        BorderLight:  Hex(0xFF7FA8, 0.42f),
        Highlight:    Hex(0xFFC2D6, 0.26f));

    /// <summary>
    /// La fête des demoiselles : nuit prune, halo rose, lueur pêche. L'accent
    /// rose pâle marque l'état actif, l'action pêche le geste principal.
    /// </summary>
    private static readonly Palette LittleLadies = new(
        NightTop:     Hex(0x3A1A3E),
        NightBottom:  Hex(0x170A1B),
        HaloCrystal:  Hex(0xFF80B8, 0.30f),
        HaloGold:     Hex(0xFFBA96, 0.18f),
        Accent:       Hex(0xFF9EC7),
        AccentHover:  Hex(0xFFBEDA),
        AccentActive: Hex(0xF07FB0),
        Action:       Hex(0xFFB48C),
        ActionHover:  Hex(0xFFC6A6),
        ActionActive: Hex(0xF09A6E),
        TextOnAction: Hex(0x2E1206),
        TitleGold:    Hex(0xFFD3E4),
        Border:       Hex(0xFF9EC7, 0.28f),
        BorderSoft:   Hex(0xFF9EC7, 0.15f),
        BorderLight:  Hex(0xFF9EC7, 0.44f),
        Highlight:    Hex(0xFFD4E6, 0.28f));

    /// <summary>
    /// Le festival du Gold Saucer : nuit violette, halo néon magenta, or des
    /// jetons pour l'action et les titres.
    /// </summary>
    private static readonly Palette GoldSaucer = new(
        NightTop:     Hex(0x2A0E5E),
        NightBottom:  Hex(0x0E0628),
        HaloCrystal:  Hex(0xFF3CD2, 0.30f),
        HaloGold:     Hex(0xFFC93C, 0.20f),
        Accent:       Hex(0xFF7FE8),
        AccentHover:  Hex(0xFFA6EF),
        AccentActive: Hex(0xF05AD8),
        Action:       Hex(0xFFC93C),
        ActionHover:  Hex(0xFFD766),
        ActionActive: Hex(0xEBB21F),
        TextOnAction: Hex(0x2A1A00),
        TitleGold:    Hex(0xFFE27A),
        Border:       Hex(0xFFC93C, 0.30f),
        BorderSoft:   Hex(0xFFC93C, 0.16f),
        BorderLight:  Hex(0xFFC93C, 0.46f),
        Highlight:    Hex(0xFFE696, 0.30f));

    /// <summary>
    /// Les Feux de la Mort : nuit d'été, braise à l'horizon, accent turquoise
    /// du lagon, action orange des feux.
    /// </summary>
    private static readonly Palette Moonfire = new(
        NightTop:     Hex(0x0A1D3C),
        NightBottom:  Hex(0x071424),
        HaloCrystal:  Hex(0xFF6A2B, 0.30f),
        HaloGold:     Hex(0xFF408C, 0.15f),
        Accent:       Hex(0x4FE8D6),
        AccentHover:  Hex(0x82F0E3),
        AccentActive: Hex(0x2CCFBC),
        Action:       Hex(0xFF7A2E),
        ActionHover:  Hex(0xFF9452),
        ActionActive: Hex(0xE5641A),
        TextOnAction: Hex(0x2A0E00),
        TitleGold:    Hex(0xFFD08A),
        Border:       Hex(0xFF8A3D, 0.28f),
        BorderSoft:   Hex(0xFF8A3D, 0.15f),
        BorderLight:  Hex(0xFF8A3D, 0.44f),
        Highlight:    Hex(0xFFC88C, 0.28f));

    /// <summary>
    /// La fête de la Commémoration : nuit bleu profond, lueur du phénix au
    /// centre, halo de cristal autour, action or.
    /// </summary>
    private static readonly Palette Rising = new(
        NightTop:     Hex(0x0C1840),
        NightBottom:  Hex(0x060B1E),
        HaloCrystal:  Hex(0xFF9646, 0.28f),
        HaloGold:     Hex(0x6CB8FF, 0.22f),
        Accent:       Hex(0x8FD8FF),
        AccentHover:  Hex(0xB5E6FF),
        AccentActive: Hex(0x66C2F0),
        Action:       Hex(0xFFB547),
        ActionHover:  Hex(0xFFC56E),
        ActionActive: Hex(0xEB9E2A),
        TextOnAction: Hex(0x2A1600),
        TitleGold:    Hex(0xFFD98A),
        Border:       Hex(0x8FD8FF, 0.26f),
        BorderSoft:   Hex(0x8FD8FF, 0.14f),
        BorderLight:  Hex(0x8FD8FF, 0.42f),
        Highlight:    Hex(0xC8EBFF, 0.30f));
}

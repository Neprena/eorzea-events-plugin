using Dalamud.Interface.Utility;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Jetons de design du plugin : la « Nuit dorée ».
///
/// La palette vient du logo : la nuit bleu encre où se dresse la citadelle,
/// le cristal bleu qui marque ce qui est actif ou choisi, et l'or des lettres,
/// réservé à l'action principale d'un écran et aux titres. La structure, elle,
/// est celle de Linkpearl : fond de nuit en dégradé avec un halo, cartes
/// translucides, entrée active entourée de lumière.
///
/// Toute dimension en pixels doit passer par <see cref="S(float)"/> : sans ça
/// l'interface devient illisible dès que l'utilisateur monte l'échelle Dalamud.
/// </summary>
internal static class Theme
{
    // ─── Conversion ───────────────────────────────────────────────────────────

    /// <summary>Convertit un RGB hexadécimal (0xRRGGBB) en couleur ImGui.</summary>
    public static Vector4 Hex(uint rgb, float a = 1f) => new(
        ((rgb >> 16) & 0xFF) / 255f,
        ((rgb >>  8) & 0xFF) / 255f,
        ( rgb        & 0xFF) / 255f,
        a);

    // ─── Fonds ────────────────────────────────────────────────────────────────
    //
    // Échelle de profondeur, du plus enfoncé au plus surélevé. La règle qui rend
    // une interface sombre lisible : chaque niveau doit être distinct du
    // précédent, sinon les cartes disparaissent dans le fond et tout paraît plat.
    //
    //   BgSunken  <  BgSidebar  <  BgBase  <  BgSurface  <  BgRaised  <  BgHover
    //
    // BgSurface et au-delà sont opaques : ils servent à ce qui doit masquer le
    // décor (infobulles, menus, notifications). Les cartes posées sur la nuit
    // utilisent CardFill, translucide.

    public static readonly Vector4 BgSunken  = Hex(0x070915); // champs de saisie, zones creusées
    public static readonly Vector4 BgBase    = Hex(0x0B0E1C); // bas du fond de fenêtre
    public static readonly Vector4 BgSidebar = Hex(0x090C18); // barres et colonne, un cran sous le fond
    public static readonly Vector4 BgSurface = Hex(0x171C30); // surfaces opaques, infobulles
    public static readonly Vector4 BgRaised  = Hex(0x20263C); // surface survolée, boutons neutres
    public static readonly Vector4 BgHover   = Hex(0x2B3149); // survol d'un élément surélevé

    /// <summary>Conservé pour la façade UiStyle, le temps de la migration.</summary>
    public static readonly Vector4 BgDeep = BgSunken;

    /// <summary>Fond d'une carte posée sur la nuit : translucide, pour laisser voir le halo.</summary>
    public static readonly Vector4 CardFill      = Hex(0xFFFFFF, 0.045f);
    public static readonly Vector4 CardFillHover = Hex(0xFFFFFF, 0.08f);

    /// <summary>Haut du dégradé de fond.</summary>
    public static readonly Vector4 NightTop = Hex(0x141A33);

    /// <summary>Les deux halos du haut de fenêtre : le cristal au centre, l'or plus large et plus pâle.</summary>
    public static readonly Vector4 HaloCrystal = Hex(0x5096FF, 0.30f);
    public static readonly Vector4 HaloGold    = Hex(0xE9BE5C, 0.10f);

    /// <summary>Ombre portée sous les surfaces surélevées.</summary>
    public static readonly Vector4 Shadow = Hex(0x000000, 0.50f);

    // ─── Accents ──────────────────────────────────────────────────────────────
    //
    // Le cristal marque ce qui est actif ou choisi : entrée de navigation,
    // filtre, interrupteur. L'or ne sert qu'à l'action principale d'un écran :
    // répandu, il ne désignerait plus rien.

    public static readonly Vector4 Accent       = Hex(0x6CCBFF);
    public static readonly Vector4 AccentHover  = Hex(0x9ADCFF);
    public static readonly Vector4 AccentActive = Hex(0x3FA9E6);

    public static readonly Vector4 Action       = Hex(0xE9BE5C);
    public static readonly Vector4 ActionHover  = Hex(0xF4D27A);
    public static readonly Vector4 ActionActive = Hex(0xD9A63E);

    /// <summary>Texte posé sur l'or, un brun profond plutôt qu'un noir.</summary>
    public static readonly Vector4 TextOnAction = Hex(0x2A1A00);

    /// <summary>Or des titres de page et de la marque, celui des lettres du logo.</summary>
    public static readonly Vector4 TitleGold = Hex(0xE9C46A);

    /// <summary>
    /// Or historique. Inchangé à dessein : il sert aussi de couleur par défaut
    /// des emotes dans le chat, que le joueur a pu adopter.
    /// </summary>
    public static readonly Vector4 Gold      = Hex(0xE0B44C);
    public static readonly Vector4 GoldHover = Hex(0xF0CC72);

    // ─── Texte ────────────────────────────────────────────────────────────────
    //
    // Un ivoire chaud plutôt qu'un blanc bleuté : il s'accorde à l'or et reste
    // au-dessus de 4,5:1 jusqu'au plus pâle, même sur le haut du dégradé.

    public static readonly Vector4 Text      = Hex(0xF1ECE0);
    public static readonly Vector4 TextMuted = Hex(0xC9C0AA);
    public static readonly Vector4 TextFaint = Hex(0x958F82);
    public static readonly Vector4 Link      = Hex(0x8FD8FF);

    /// <summary>Texte posé sur une surface claire (cristal, chip vif).</summary>
    public static readonly Vector4 TextOnLight = Hex(0x12152A);

    // ─── Statuts ──────────────────────────────────────────────────────────────

    public static readonly Vector4 Online      = Hex(0x6FE0B0);

    /// <summary>Jaune et non orange : l'or est à l'action.</summary>
    public static readonly Vector4 Idle        = Hex(0xF5D06F);

    public static readonly Vector4 Danger      = Hex(0xFF6B7D);
    public static readonly Vector4 DangerHover = Hex(0xFF8A98);

    // ─── Bordures ─────────────────────────────────────────────────────────────
    //
    // Des filets d'or translucides, comme sur le site. Précalculés en opaque
    // sur le fond, ils faisaient des traits sombres dès que la surface était
    // translucide et le décor clair derrière : vu en jeu le 19 octobre, le
    // filet d'un titre de section barrait la carte des filtres.
    //
    // Pour les atténuer, multiplier leur opacité (Faded) plutôt que la
    // remplacer avec Alpha : celle-ci les rendrait plus opaques qu'à l'origine.

    public static readonly Vector4 Border      = Hex(0xE9BE5C, 0.24f);
    public static readonly Vector4 BorderSoft  = Hex(0xE9BE5C, 0.13f);
    public static readonly Vector4 BorderLight = Hex(0xE9BE5C, 0.40f);

    /// <summary>
    /// Liseré clair posé sur l'arête haute d'une surface. C'est ce qui donne
    /// l'impression que la carte capte la lumière et se détache du fond. Doré,
    /// comme les filets.
    /// </summary>
    public static readonly Vector4 Highlight = Hex(0xF2CB6B, 0.22f);

    // ─── Métriques (en pixels non scalés : toujours passer par S()) ───────────

    public const float RadiusWindow = 12f;
    public const float RadiusCard   = 12f;
    public const float RadiusFrame  =  8f;
    public const float RadiusPill   = 11f;

    /// <summary>Largeur de la colonne par défaut, et ses bornes quand on la tire.</summary>
    public const float SidebarWidth    = 196f;
    public const float SidebarMin      = 150f;
    public const float SidebarMax      = 260f;

    /// <summary>Largeur de la colonne repliée en icônes.</summary>
    public const float SidebarRail     = 76f;

    public const float SidebarItem     = 38f;
    public const float SidebarRailItem = 56f;
    public const float TitleBarHeight  = 40f;
    public const float StatusBarHeight = 30f;

    public const float PadWindowX = 16f;
    public const float PadWindowY = 14f;
    public const float CardPadX   = 14f;
    public const float CardPadY   = 11f;

    public const float GapXs =  3f;
    public const float GapS  =  5f;
    public const float GapM  =  8f;
    public const float GapL  = 12f;
    public const float GapXl = 22f;

    // ─── Échelle ──────────────────────────────────────────────────────────────

    /// <summary>Met une dimension à l'échelle de l'interface Dalamud.</summary>
    public static float S(float px) => px * ImGuiHelpers.GlobalScale;

    /// <summary>Met un couple de dimensions à l'échelle de l'interface Dalamud.</summary>
    public static Vector2 S(float x, float y) =>
        new(x * ImGuiHelpers.GlobalScale, y * ImGuiHelpers.GlobalScale);

    // ─── Utilitaires couleur ──────────────────────────────────────────────────

    public static Vector4 Alpha(Vector4 c, float a) => c with { W = a };

    /// <summary>Atténue une couleur en multipliant son opacité, sans la remplacer.</summary>
    public static Vector4 Faded(Vector4 c, float factor) => c with { W = c.W * factor };

    /// <summary>
    /// Convertit une couleur au format « #RRGGBB » venue de l'API. Renvoie null
    /// si la valeur est absente ou malformée, à charge de l'appelant de retomber
    /// sur la palette.
    /// </summary>
    public static Vector4? TryParseHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var text = value.AsSpan().Trim();
        if (text.Length > 0 && text[0] == '#') text = text[1..];
        if (text.Length != 6) return null;

        return uint.TryParse(text, System.Globalization.NumberStyles.HexNumber,
                             System.Globalization.CultureInfo.InvariantCulture, out var rgb)
            ? Hex(rgb)
            : null;
    }

    /// <summary>
    /// Remonte une couleur trop sombre pour rester lisible sur le fond de
    /// l'interface. Les couleurs saisies par les gérants d'établissement sont
    /// pensées pour un fond clair et peuvent être quasi noires.
    /// </summary>
    /// <param name="toward">Teinte vers laquelle éclaircir ; le texte de l'interface par défaut.</param>
    public static Vector4 EnsureReadable(Vector4 color, float minimum = 0.30f, Vector4? toward = null)
    {
        var luminance = Luminance(color);
        return luminance >= minimum
            ? color
            : Mix(color, toward ?? Text, (minimum - luminance) / Math.Max(minimum, 0.001f) * 0.8f);
    }

    public static Vector4 Mix(Vector4 a, Vector4 b, float t) => Vector4.Lerp(a, b, t);

    /// <summary>
    /// Luminance relative perçue (coefficients ITU-R BT.709). Le vert pèse dix
    /// fois plus que le bleu dans la perception, d'où l'écart des poids.
    /// </summary>
    public static float Luminance(Vector4 c) => 0.2126f * c.X + 0.7152f * c.Y + 0.0722f * c.Z;

    /// <summary>
    /// Couleur de texte lisible sur le fond donné. Le cristal et l'or étant
    /// clairs, du texte blanc dessus serait illisible.
    /// </summary>
    public static Vector4 TextOn(Vector4 background) =>
        Luminance(background) > 0.55f ? TextOnLight : Text;

    /// <summary>
    /// Couleur stable dérivée d'un nom, pour les pastilles d'initiales.
    /// Hash FNV-1a sur la teinte, saturation et valeur fixées pour rester
    /// lisible sur fond sombre.
    /// </summary>
    public static Vector4 FromName(string name)
    {
        var hash = 2166136261u;
        foreach (var ch in name)
        {
            hash ^= ch;
            hash *= 16777619u;
        }

        return FromHsv(hash % 360u / 360f, 0.45f, 0.58f);
    }

    /// <summary>
    /// Couleur à partir d'une teinte, d'une saturation et d'une valeur, chacune
    /// dans [0, 1]. Publique pour le dégradé de titre d'AnimatedText, qui balaie
    /// la roue des teintes à saturation constante.
    /// </summary>
    public static Vector4 FromHsv(float h, float s, float v)
    {
        var i = (int)MathF.Floor(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        return (i % 6) switch
        {
            0 => new Vector4(v, t, p, 1f),
            1 => new Vector4(q, v, p, 1f),
            2 => new Vector4(p, v, t, 1f),
            3 => new Vector4(p, q, v, 1f),
            4 => new Vector4(t, p, v, 1f),
            _ => new Vector4(v, p, q, 1f),
        };
    }
}

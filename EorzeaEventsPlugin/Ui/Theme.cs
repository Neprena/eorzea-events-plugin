using Dalamud.Interface.Utility;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Jetons de design du plugin. Miroir de <c>tailwind.config.ts</c> côté site,
/// pour que le plugin et le web partagent la même identité visuelle.
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
    //   BgSunken  <  BgBase  <  BgSurface  <  BgRaised  <  BgHover

    public static readonly Vector4 BgSunken  = Hex(0x1C1F26); // champs de saisie, zones creusées
    public static readonly Vector4 BgBase    = Hex(0x212530); // fond de fenêtre
    public static readonly Vector4 BgSidebar = Hex(0x252A35); // barre latérale, barre de titre
    public static readonly Vector4 BgSurface = Hex(0x2C313C); // cartes, panneaux
    public static readonly Vector4 BgRaised  = Hex(0x363C48); // carte survolée, boutons neutres
    public static readonly Vector4 BgHover   = Hex(0x424A58); // survol d'un élément surélevé

    // Conservés pour la façade UiStyle, le temps de la migration.
    public static readonly Vector4 BgDeep     = Hex(0x1C1F26);
    public static readonly Vector4 BgModifier = Hex(0x252A35);
    public static readonly Vector4 BgModHover = Hex(0x363C48);

    /// <summary>Ombre portée sous les surfaces surélevées.</summary>
    public static readonly Vector4 Shadow = Hex(0x000000, 0.50f);

    // ─── Accents ──────────────────────────────────────────────────────────────

    public static readonly Vector4 Accent       = Hex(0x22D3EE);
    public static readonly Vector4 AccentHover  = Hex(0x67E8F9);
    public static readonly Vector4 AccentActive = Hex(0x0891B2);

    /// <summary>Version assourdie de l'accent, pour les fonds et les voiles.</summary>
    public static readonly Vector4 AccentMuted = Hex(0x1B7F92);

    public static readonly Vector4 Gold      = Hex(0xE0B44C);
    public static readonly Vector4 GoldHover = Hex(0xF0CC72);

    // ─── Texte ────────────────────────────────────────────────────────────────

    public static readonly Vector4 Text      = Hex(0xE8EAEF);
    public static readonly Vector4 TextMuted = Hex(0xAEB5C2);
    public static readonly Vector4 TextFaint = Hex(0x808896);
    public static readonly Vector4 Link      = Hex(0x7DD3FC);

    /// <summary>Texte posé sur une surface claire (bouton d'accent, chip vif).</summary>
    public static readonly Vector4 TextOnLight = Hex(0x0E1116);

    // ─── Statuts ──────────────────────────────────────────────────────────────

    public static readonly Vector4 Online      = Hex(0x3DD68C);
    public static readonly Vector4 Idle        = Hex(0xF5B942);
    public static readonly Vector4 Danger      = Hex(0xF4595C);
    public static readonly Vector4 DangerHover = Hex(0xFF7376);

    // ─── Bordures ─────────────────────────────────────────────────────────────

    public static readonly Vector4 Border      = Hex(0x3E4552);
    public static readonly Vector4 BorderSoft  = Hex(0x313743);
    public static readonly Vector4 BorderLight = Hex(0x525C6E);

    /// <summary>
    /// Liseré clair posé sur l'arête haute d'une surface. C'est ce qui donne
    /// l'impression que la carte capte la lumière et se détache du fond.
    /// </summary>
    public static readonly Vector4 Highlight = Hex(0xFFFFFF, 0.055f);

    // ─── Métriques (en pixels non scalés : toujours passer par S()) ───────────

    public const float RadiusWindow = 10f;
    public const float RadiusCard   =  8f;
    public const float RadiusFrame  =  6f;
    public const float RadiusPill   = 10f;

    public const float SidebarWidth    = 186f;
    public const float SidebarItem     = 40f;
    public const float TitleBarHeight  = 40f;
    public const float StatusBarHeight = 26f;

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
    public static float S(float px) => px * ImGuiHelpers.GlobalScaleSafe;

    /// <summary>Met un couple de dimensions à l'échelle de l'interface Dalamud.</summary>
    public static Vector2 S(float x, float y) =>
        new(x * ImGuiHelpers.GlobalScaleSafe, y * ImGuiHelpers.GlobalScaleSafe);

    // ─── Utilitaires couleur ──────────────────────────────────────────────────

    public static Vector4 Alpha(Vector4 c, float a) => c with { W = a };

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
    public static Vector4 EnsureReadable(Vector4 color, float minimum = 0.30f)
    {
        var luminance = Luminance(color);
        return luminance >= minimum
            ? color
            : Mix(color, Text, (minimum - luminance) / Math.Max(minimum, 0.001f) * 0.8f);
    }

    public static Vector4 Mix(Vector4 a, Vector4 b, float t) => Vector4.Lerp(a, b, t);

    /// <summary>
    /// Luminance relative perçue (coefficients ITU-R BT.709). Le vert pèse dix
    /// fois plus que le bleu dans la perception, d'où l'écart des poids.
    /// </summary>
    public static float Luminance(Vector4 c) => 0.2126f * c.X + 0.7152f * c.Y + 0.0722f * c.Z;

    /// <summary>
    /// Couleur de texte lisible sur le fond donné. Indispensable depuis que
    /// l'accent est une couleur claire : du texte blanc sur du turquoise vif
    /// serait illisible.
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

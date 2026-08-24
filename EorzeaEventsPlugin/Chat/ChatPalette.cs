using System.Numerics;
using Lumina.Excel.Sheets;

namespace EorzeaEventsPlugin.Chat;

/// <summary>
/// Couleurs disponibles pour le chat.
///
/// Le chat du jeu ne prend pas de couleur libre : une teinte y est une ligne
/// de la feuille <c>UIColor</c>, désignée par sa clé, et une valeur RVB choisie
/// ailleurs ne s'afficherait pas du tout. La palette proposée dans les réglages
/// est donc lue dans les données du jeu plutôt qu'écrite à la main : une liste
/// de clés recopiée vieillirait au premier remaniement de la feuille, et
/// personne ne s'en apercevrait avant de voir du texte redevenu blanc.
/// </summary>
internal static class ChatPalette
{
    /// <summary>
    /// Clé neutre. Dans un message, elle rend au chat la couleur du canal ;
    /// dans les réglages, elle vaut « automatique », c'est-à-dire la teinte de
    /// l'interface du plugin ramenée à la palette du jeu. Un identifiant de
    /// couleur écrit en dur dans la configuration ferait autrement un défaut
    /// impossible à corriger sans migration.
    /// </summary>
    public const ushort Off = 0;

    /// <summary>Teintes retenues par défaut pour chaque convention d'écriture.</summary>
    public static Vector4 EmoteDefault  => Ui.Theme.Gold;
    public static Vector4 OocDefault    => Ui.Theme.TextFaint;
    public static Vector4 SpeechDefault => Ui.Theme.Link;
    public static Vector4 NameDefault   => Ui.Theme.Accent;

    /// <summary>
    /// Clé effective d'un réglage : celle choisie, ou la plus proche de la
    /// teinte par défaut tant que rien n'a été choisi.
    /// </summary>
    public static ushort Resolve(ushort configured, Vector4 fallback)
        => configured != Off ? configured : Nearest(fallback);

    // La feuille est parcourue largement, et non plus sur ses quatre-vingts
    // premières lignes : ce bloc-là ne contient que les gris, blancs et beiges
    // de l'interface. Une couleur d'accent verte s'y voyait ramenée « au plus
    // proche » dans une palette sans le moindre vert, et ressortait grise.
    private const uint ScanFrom = 1;
    private const uint ScanTo   = 700;

    // Secteurs de teinte retenus, en plus des neutres. Douze couvrent le cercle
    // chromatique sans noyer la grille des réglages : au-delà, deux pastilles
    // voisines ne se distinguent plus à l'œil.
    private const int HueBuckets = 12;

    /// <summary>
    /// En deçà de cette saturation, une couleur compte comme neutre. Sans ce
    /// tri, un beige pâle occuperait le secteur des oranges et l'emporterait
    /// face à une vraie couleur, faute de concurrence.
    /// </summary>
    private const float NeutralSaturation = 0.18f;

    private static ushort[]?  _keys;
    private static Vector4[]? _colors;

    /// <summary>Clés proposées à l'utilisateur, dans l'ordre d'affichage.</summary>
    public static IReadOnlyList<ushort> Keys
    {
        get { Build(); return _keys ?? []; }
    }

    /// <summary>Couleur affichable d'une clé, blanche si la feuille l'ignore.</summary>
    public static Vector4 Color(ushort key)
    {
        Build();

        for (var i = 0; _keys != null && i < _keys.Length; i++)
            if (_keys[i] == key) return _colors![i];

        return Read(key) ?? Ui.Theme.Text;
    }

    /// <summary>
    /// Clé de la palette la plus proche d'une couleur libre.
    ///
    /// Sert aux couleurs d'accent des fiches RP, qui sont saisies en RVB sur le
    /// site : faute de pouvoir les rendre telles quelles dans le chat, on
    /// prend la teinte du jeu qui s'en approche le plus, plutôt que d'ignorer le
    /// choix de l'auteur.
    /// </summary>
    public static ushort Nearest(Vector4 color)
    {
        Build();
        if (_keys is not { Length: > 0 }) return Off;

        var (wantedHue, wantedSaturation) = HueAndSaturation(color);

        var best     = _keys[0];
        var bestDist = float.MaxValue;

        for (var i = 0; i < _keys.Length; i++)
        {
            var c    = _colors![i];
            var dist = (c.X - color.X) * (c.X - color.X)
                     + (c.Y - color.Y) * (c.Y - color.Y)
                     + (c.Z - color.Z) * (c.Z - color.Z);

            var (hue, saturation) = HueAndSaturation(c);

            // La distance brute entre deux couleurs trompe dès qu'on quitte les
            // gris : un gris moyen est arithmétiquement proche d'un vert franc,
            // alors qu'il n'en donne aucune idée à la lecture. La teinte pèse
            // donc dans le choix, ce qui corrige l'accent vert qui ressortait
            // gris.
            if (wantedSaturation >= NeutralSaturation)
            {
                var hueGap = Math.Abs(hue - wantedHue);
                if (hueGap > 0.5f) hueGap = 1f - hueGap; // le cercle se referme

                dist += hueGap * hueGap * 4f;

                // Un neutre n'a pas de teinte, il échappe donc à la pénalité
                // ci-dessus et gagnerait par défaut contre une couleur franche
                // pour peu qu'il en soit arithmétiquement proche. Une pénalité
                // forfaitaire le remet à sa place, sans écarter les gris quand
                // c'est un gris qu'on cherche.
                if (saturation < NeutralSaturation) dist += 0.5f;
            }

            if (dist >= bestDist) continue;
            bestDist = dist;
            best     = _keys[i];
        }

        return best;
    }

    /// <summary>
    /// Constitue la palette une seule fois, au premier besoin.
    ///
    /// Pas au démarrage : la feuille est lue depuis les données du jeu, et le
    /// constructeur du plugin s'exécute avant qu'elles ne soient toutes prêtes.
    /// </summary>
    private static void Build()
    {
        if (_keys != null) return;

        // Un représentant par secteur de teinte, plus un pour les neutres : la
        // palette couvre ainsi tout le cercle chromatique, quel que soit
        // l'endroit de la feuille où le jeu range ses couleurs. Prendre les
        // premières lignes venues donnait une grille entière de gris.
        var best = new (ushort Key, Vector4 Color, float Score)?[HueBuckets + 1];

        for (var row = ScanFrom; row <= ScanTo; row++)
        {
            var raw = ReadRaw(row);
            if (raw is not { } packed) continue;

            // L'opacité nulle donne du texte invisible.
            if ((packed & 0xFF) == 0) continue;

            var color = Unpack(packed);

            // Le chat est sur fond sombre : une teinte trop sombre y serait
            // illisible, et le noir pur passerait pour un texte manquant.
            if (Ui.Theme.Luminance(color) < 0.22f) continue;

            var (hue, saturation) = HueAndSaturation(color);

            // Les neutres partagent un seul emplacement, le dernier : ils sont
            // légion dans la feuille et se ressemblent tous.
            var bucket = saturation < NeutralSaturation
                ? HueBuckets
                : Math.Min(HueBuckets - 1, (int)(hue * HueBuckets));

            // À teinte égale, la plus franche gagne : c'est celle qui se
            // distingue le mieux du blanc du chat.
            var score = saturation;
            if (best[bucket] is { } held && held.Score >= score) continue;

            best[bucket] = ((ushort)row, color, score);
        }

        var keys   = new List<ushort>(HueBuckets + 1);
        var colors = new List<Vector4>(HueBuckets + 1);

        foreach (var swatch in best)
        {
            if (swatch is not { } value) continue;
            keys.Add(value.Key);
            colors.Add(value.Color);
        }

        // Rien trouvé : les données de jeu ne sont pas encore montées. On ne
        // mémorise pas cet échec, sinon la palette resterait vide pour toute la
        // session à cause d'un premier affichage trop précoce.
        if (keys.Count == 0) return;

        _colors = [.. colors];
        _keys   = [.. keys];
    }

    /// <summary>
    /// Teinte (de 0 à 1) et saturation d'une couleur, au sens de la conversion
    /// vers l'espace TSV.
    ///
    /// Suffisant ici : on ne cherche pas une conversion colorimétrique juste,
    /// seulement à ranger des couleurs par famille et à savoir laquelle est la
    /// plus franche.
    /// </summary>
    private static (float Hue, float Saturation) HueAndSaturation(Vector4 c)
    {
        var max = Math.Max(c.X, Math.Max(c.Y, c.Z));
        var min = Math.Min(c.X, Math.Min(c.Y, c.Z));
        var delta = max - min;

        if (delta < 0.0001f || max <= 0f) return (0f, 0f);

        float hue;
        if (max == c.X)      hue = (c.Y - c.Z) / delta % 6f;
        else if (max == c.Y) hue = (c.Z - c.X) / delta + 2f;
        else                 hue = (c.X - c.Y) / delta + 4f;

        hue /= 6f;
        if (hue < 0f) hue += 1f;

        return (hue, delta / max);
    }

    private static Vector4? Read(ushort key)
    {
        var raw = ReadRaw(key);
        return raw is { } packed ? Unpack(packed) : null;
    }

    private static uint? ReadRaw(uint row)
    {
        try
        {
            var sheet = Plugin.DataManager.GetExcelSheet<UIColor>();
            return sheet?.GetRowOrDefault(row)?.Dark;
        }
        catch (Exception)
        {
            // Feuille absente ou données de jeu pas encore montées : la palette
            // se reconstruira au prochain appel plutôt que de faire tomber
            // l'affichage des réglages.
            return null;
        }
    }

    /// <summary>La feuille range ses couleurs en 0xRRGGBBAA.</summary>
    private static Vector4 Unpack(uint packed) => new(
        ((packed >> 24) & 0xFF) / 255f,
        ((packed >> 16) & 0xFF) / 255f,
        ((packed >>  8) & 0xFF) / 255f,
        1f);
}

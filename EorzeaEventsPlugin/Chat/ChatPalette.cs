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

    // Index complet des couleurs candidates. Il ne s'affiche jamais : il sert à
    // ramener une teinte libre, choisie au sélecteur ou lue sur une fiche RP, à
    // la ligne UIColor la plus proche. Chercher dans la seule grille faisait
    // retomber un violet sur l'une des treize pastilles, c'est-à-dire sur autre
    // chose que ce qui avait été demandé.
    private static ushort[]?  _allKeys;
    private static Vector4[]? _allColors;

    /// <summary>Couleur de chaque clé lisible, pour ne pas relire la feuille à chaque image.</summary>
    private static Dictionary<ushort, Vector4>? _byKey;

    // Résultats de Nearest déjà calculés, indexés sur la couleur demandée. Le
    // chat rappelle Nearest à chaque message reçu, toujours avec les mêmes
    // trois ou quatre teintes : recalculer une distance sur plusieurs centaines
    // de candidats à chaque ligne du journal serait payé pour rien.
    //
    // Aucun verrou : chat et interface tournent tous deux sur le fil principal
    // du jeu.
    private static readonly Dictionary<uint, ushort> NearestCache = [];

    /// <summary>Clés proposées à l'utilisateur, dans l'ordre d'affichage.</summary>
    public static IReadOnlyList<ushort> Keys
    {
        get { Build(); return _keys ?? []; }
    }

    /// <summary>Couleur affichable d'une clé, blanche si la feuille l'ignore.</summary>
    public static Vector4 Color(ushort key)
    {
        Build();

        if (_byKey != null && _byKey.TryGetValue(key, out var known)) return known;

        // Clé absente de l'index : elle vient d'une configuration écrite par une
        // version antérieure, ou d'une ligne écartée pour illisibilité. On la
        // relit plutôt que de la remplacer d'office, le choix de l'utilisateur
        // primant sur notre tri.
        return Read(key) ?? Ui.Theme.Text;
    }

    /// <summary>
    /// Couleur telle que le chat l'affichera pour une teinte libre.
    ///
    /// La palette du jeu n'a pas forcément la nuance demandée : montrer la
    /// teinte choisie plutôt que celle rendue ferait mentir les réglages, ce
    /// qui est exactement ce qu'on nous a signalé.
    /// </summary>
    public static Vector4 Rendered(Vector4 free) => Color(Nearest(free));

    /// <summary>
    /// Encode une teinte libre pour la configuration.
    ///
    /// Le fichier ne retenait jusqu'ici que la clé de palette approchée : la
    /// teinte réellement demandée était perdue à la fermeture de la fenêtre, et
    /// rien ne disait plus qu'une couleur personnalisée avait été choisie.
    ///
    /// L'octet de poids fort est forcé à 0xFF pour qu'aucune couleur valide ne
    /// vaille zéro, zéro signifiant « aucune teinte personnalisée ». Sans lui, le
    /// noir pur serait indistinguable de l'absence de choix.
    /// </summary>
    public static uint Encode(Vector4 color) => 0xFF000000u | Pack(color);

    /// <summary>
    /// Teinte libre relue depuis la configuration, ou <c>null</c> si aucune n'a
    /// été enregistrée.
    /// </summary>
    public static Vector4? Decode(uint stored)
    {
        if (stored == 0) return null;

        return new Vector4(((stored >> 16) & 0xFF) / 255f,
                           ((stored >> 8)  & 0xFF) / 255f,
                           (stored         & 0xFF) / 255f,
                           1f);
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
        if (_allKeys is not { Length: > 0 }) return Off;

        var wanted = Pack(color);
        if (NearestCache.TryGetValue(wanted, out var cached)) return cached;

        var (wantedHue, wantedSaturation) = HueAndSaturation(color);

        var best     = _allKeys[0];
        var bestDist = float.MaxValue;

        for (var i = 0; i < _allKeys.Length; i++)
        {
            var c    = _allColors![i];
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
            best     = _allKeys[i];
        }

        // Le cache ne grandit qu'au rythme des couleurs distinctes réellement
        // demandées : quelques teintes de réglages, plus un accent par fiche RP
        // croisée. Au-delà d'un plafond on repart de zéro, plutôt que de laisser
        // une session de plusieurs heures accumuler sans fin.
        if (NearestCache.Count > 512) NearestCache.Clear();
        NearestCache[wanted] = best;

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

        // Index complet, constitué dans la même passe : la lecture de la feuille
        // est le seul coût réel ici, et la faire deux fois n'apporterait rien.
        var all       = new List<ushort>(256);
        var allColors = new List<Vector4>(256);
        var byKey     = new Dictionary<ushort, Vector4>(256);

        // La feuille répète beaucoup de valeurs d'une ligne à l'autre. Les
        // dédupliquer garde l'index court et rend le rapprochement
        // reproductible : à couleur égale, c'est toujours la première ligne
        // rencontrée qui l'emporte.
        var seen = new HashSet<uint>();

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

            // Toute couleur lisible est candidate au rapprochement, même celle
            // qu'aucune pastille ne montrera : c'est ce qui permet à un violet
            // choisi au sélecteur de sortir violet, et non ramené à l'une des
            // treize teintes de la grille.
            byKey[(ushort)row] = color;

            if (seen.Add(packed))
            {
                all.Add((ushort)row);
                allColors.Add(color);
            }

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
        if (keys.Count == 0 || all.Count == 0) return;

        // Un rapprochement calculé sur une palette vide n'aurait rien à dire :
        // le cache est vidé pour que les premières réponses, éventuellement
        // fausses, ne survivent pas à la construction.
        NearestCache.Clear();

        _allColors = [.. allColors];
        _allKeys   = [.. all];
        _byKey     = byKey;
        _colors    = [.. colors];

        // Assigné en dernier : c'est lui qui sert de témoin de construction.
        _keys = [.. keys];
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

    /// <summary>
    /// Couleur ramenée à un entier, pour servir de clé de cache. L'opacité est
    /// ignorée : le chat n'en tient aucun compte.
    /// </summary>
    private static uint Pack(Vector4 c) =>
        (Component(c.X) << 16) | (Component(c.Y) << 8) | Component(c.Z);

    /// <summary>
    /// Composante ramenée à un octet, par arrondi et non par troncature : une
    /// couleur reconstituée depuis la feuille vaut n sur 255, et le produit par
    /// 255 retombe volontiers à n moins un millionième. Tronquer donnerait deux
    /// clés distinctes pour une même couleur, et le cache manquerait à chaque
    /// fois.
    /// </summary>
    private static uint Component(float v) =>
        (uint)Math.Clamp(MathF.Round(v * 255f), 0f, 255f);

    /// <summary>La feuille range ses couleurs en 0xRRGGBBAA.</summary>
    private static Vector4 Unpack(uint packed) => new(
        ((packed >> 24) & 0xFF) / 255f,
        ((packed >> 16) & 0xFF) / 255f,
        ((packed >>  8) & 0xFF) / 255f,
        1f);
}

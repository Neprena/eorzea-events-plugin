using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Titre court d'une fiche RP, rendu en pastille, avec son animation facultative.
///
/// Le titre était auparavant écrit en petite police sous le nom : trop peu de
/// pixels pour porter le moindre effet, et l'animation passait inaperçue. Il est
/// désormais dessiné comme sur le site, dans une pastille bordée et teintée de la
/// couleur d'accent, à une taille intermédiaire entre le corps de texte et le nom
/// du personnage. Il se remarque sans concurrencer le nom, qui reste l'élément
/// principal de l'entête.
///
/// ImGui ne sait pas dégrader la couleur d'un texte : la seule voie est de
/// découper la chaîne et de dessiner chaque glyphe avec sa propre teinte,
/// décalée en phase. Le coût est acceptable ici et nulle part ailleurs : le
/// titre fait 40 caractères au maximum et n'est dessiné qu'une fois par fiche.
///
/// Deux conséquences de ce rendu par glyphe, toutes deux traitées ici :
/// le fond de pastille doit être peint AVANT les glyphes, et les largeurs de
/// glyphes doivent être mesurées dans la police effectivement poussée pour le
/// dessin, sans quoi l'espacement entre les lettres se dérègle. La police est
/// donc choisie une fois par image, poussée une fois, et tout ce qui suit
/// (mesure, rembourrage, hauteur de pastille, dessin) en découle.
///
/// Comme pour le cadre du portrait, les périodes sont lentes et les amplitudes
/// faibles, et « animations réduites » fige l'effet sur sa valeur médiane au
/// lieu de le supprimer.
/// </summary>
internal static class AnimatedText
{
    /// <summary>Part du titre couverte par le bandeau clair des effets balayés.</summary>
    private const float BandWidth = 0.22f;

    /// <summary>Écart des copies atténuées qui composent le halo, en pixels.</summary>
    private const float HaloSpread = 1.5f;

    /// <summary>Amplitude verticale de l'ondulation, volontairement minuscule.</summary>
    private const float WaveAmplitude = 2f;

    /// <summary>
    /// Rembourrage de la pastille, exprimé en fraction de la hauteur de ligne et
    /// non en pixels : c'est ce qui fait suivre la pastille quand la police
    /// change de taille, au lieu de laisser un texte plus grand coller au bord.
    /// La hauteur de ligne vient d'une police déjà mise à l'échelle globale, il
    /// n'y a donc pas de Theme.S à appliquer par-dessus.
    /// </summary>
    private const float PadX = 0.45f;
    private const float PadY = 0.20f;

    /// <summary>Opacités du fond et de la bordure de la pastille.</summary>
    private const float FillAlpha   = 0.14f;
    private const float BorderAlpha = 0.45f;

    /// <summary>
    /// Ce qui sera effectivement dessiné cette image : le palier de police, le
    /// texte éventuellement tronqué, l'encombrement total de la pastille et son
    /// rembourrage. Un seul calcul fait autorité, partagé par le dessin et par
    /// la mesure que l'entête utilise pour centrer le bloc de nom.
    /// </summary>
    private readonly record struct Plan(int Level, string Text, Vector2 Size, Vector2 Padding);

    /// <param name="accent">
    /// Teinte de base du titre. Les appelants y passent la SECONDE couleur de la
    /// fiche : c'est elle qui habille le titre depuis l'origine. Elle teinte
    /// aussi le fond et la bordure de la pastille.
    /// </param>
    /// <param name="accent2">
    /// Autre couleur de la fiche, c'est-à-dire la PREMIÈRE, utilisée par le seul
    /// style « duotone ». Elle sert de point de départ du dégradé, qui court donc
    /// de la première couleur vers la seconde comme sur le site. Absente, elle
    /// retombe sur la teinte de base et le dégradé devient uni, ce qui reproduit
    /// exactement le rendu d'une fiche à une seule couleur.
    /// </param>
    public static void Draw(string? text, Vector4 accent, string? style, Vector4? accent2 = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var plan   = Prepare(text);
        var origin = ImGui.GetCursorScreenPos();
        var dl     = ImGui.GetWindowDrawList();

        // Avant les glyphes : le fond passerait sinon par-dessus le texte, le
        // rendu par glyphe n'ayant aucun ordre de peinture implicite.
        DrawPill(dl, origin, plan.Size, accent);

        // Les glyphes démarrent à l'intérieur du rembourrage, pas au coin de la
        // pastille.
        var at = origin + plan.Padding;

        // Une seule poussée de police pour toute la suite : c'est elle qui a servi
        // à mesurer dans Prepare, et c'est elle qui doit servir à mesurer chaque
        // glyphe dans PerGlyph.
        using var font = PushFont(plan.Level);

        switch (style)
        {
            // Bandeau clair qui balaie le titre.
            case "sweep":
                PerGlyph(dl, at, plan.Text, (i, n) => Band(accent, Theme.Text, i, n, 5f));
                break;

            // Même mécanique, reflet doré et plus lent : un lustre, pas un néon.
            case "sheen":
                PerGlyph(dl, at, plan.Text, (i, n) => Band(accent, Theme.Gold, i, n, 6f));
                break;

            case "rainbow":
                PerGlyph(dl, at, plan.Text, Rainbow);
                break;

            // Uniforme : tous les glyphes portent la même teinte, un seul appel
            // suffit donc, sans découpe.
            case "pulse":
                dl.AddText(at, ImGui.GetColorU32(
                    Theme.Mix(accent, Theme.Text, 0.05f + Pulse(1.26f) * 0.30f)), plan.Text);
                break;

            // Contour lumineux : copies décalées puis texte net, sans découpe.
            case "halo":
                Halo(dl, at, plan.Text, accent);
                break;

            // Dégradé figé de la première couleur de la fiche vers la seconde.
            case "duotone":
                PerGlyph(dl, at, plan.Text, (i, n) => Ramp(accent2 ?? accent, accent, i, n));
                break;

            // Seul effet qui déplace les glyphes plutôt que de les teinter.
            case "wave":
                PerGlyph(dl, at, plan.Text, (_, _) => accent, WaveOffset);
                break;

            // Uniforme lui aussi : l'intensité vaut pour tout le titre.
            case "neon":
                dl.AddText(at, ImGui.GetColorU32(Neon(accent)), plan.Text);
                break;

            // Animation absente ou inconnue : le titre en texte ordinaire, mais
            // toujours dans sa pastille et dans la couleur d'accent.
            default:
                dl.AddText(at, ImGui.GetColorU32(accent), plan.Text);
                break;
        }

        ImGui.Dummy(plan.Size);
    }

    /// <summary>
    /// Encombrement de la pastille, nul quand il n'y a pas de titre. Sert à
    /// l'entête, qui doit connaître la hauteur du bloc de nom AVANT de le
    /// dessiner pour le centrer sur le portrait.
    /// </summary>
    public static Vector2 Measure(string? text) =>
        string.IsNullOrWhiteSpace(text) ? Vector2.Zero : Prepare(text).Size;

    // ─── Mise en page ─────────────────────────────────────────────────────────

    /// <summary>
    /// Police du titre selon le palier : H2 (18 px demi-gras) d'abord, taille
    /// intermédiaire entre le corps et le nom rendu par <see cref="Text.Title"/>.
    /// Le palier suivant est le corps de texte, pour un titre trop long. Toutes
    /// deux viennent de l'atlas du plugin : aucun SetWindowFontScale, qui
    /// grossirait aussi tout ce qui est dessiné après.
    /// </summary>
    private static IDisposable PushFont(int level) =>
        level == 0 ? Fonts.PushH2() : Fonts.PushBody();

    /// <summary>
    /// Choisit la police et ajuste le texte pour que la pastille tienne dans la
    /// largeur restante à droite du portrait, bord intérieur de la carte compris.
    ///
    /// La mesure se fait dans chaque police candidate, à l'intérieur de sa propre
    /// poussée : CalcTextSize et la hauteur de ligne dépendent de la police
    /// courante, mesurer dans l'une pour dessiner dans l'autre donnerait une
    /// pastille fausse et un espacement de glyphes irrégulier.
    /// </summary>
    private static Plan Prepare(string text)
    {
        var safe = Glyphs.Safe(text);

        // Plancher : sur une fenêtre réduite à l'extrême, mieux vaut une pastille
        // qui dépasse un peu qu'une largeur négative et un texte réduit à « … ».
        var room = MathF.Max(ImGui.GetContentRegionAvail().X - Card.RightInset, Theme.S(48f));

        using (var _ = PushFont(0))
        {
            var line = ImGui.GetTextLineHeight();
            var pad  = new Vector2(line * PadX, line * PadY);

            if (ImGui.CalcTextSize(safe).X <= room - pad.X * 2f)
                return Build(0, safe, line, pad);
        }

        // Trop long en H2 : repli sur le corps de texte, puis troncature si même
        // celui-ci déborde. Un titre qui sort de la carte serait pire que tronqué.
        using (var _ = PushFont(1))
        {
            var line = ImGui.GetTextLineHeight();
            var pad  = new Vector2(line * PadX, line * PadY);

            return Build(1, Ellipsize(safe, room - pad.X * 2f), line, pad);
        }
    }

    /// <summary>À appeler dans la portée de police du palier concerné.</summary>
    private static Plan Build(int level, string text, float line, Vector2 pad) =>
        new(level, text, new Vector2(ImGui.CalcTextSize(text).X, line) + pad * 2f, pad);

    /// <summary>
    /// Tronque au point de suspension pour tenir dans <paramref name="limit"/>.
    ///
    /// Retrait d'un élément de texte à la fois plutôt qu'une recherche
    /// dichotomique : le titre fait 40 caractères au maximum, la boucle coûte
    /// moins que la complexité qu'on économiserait.
    /// </summary>
    private static string Ellipsize(string text, float limit)
    {
        if (ImGui.CalcTextSize(text).X <= limit) return text;

        var elements = TextElements(text);

        for (var kept = elements.Length - 1; kept > 0; kept--)
        {
            var candidate = string.Concat(elements[..kept]) + "…";
            if (ImGui.CalcTextSize(candidate).X <= limit) return candidate;
        }

        return "…";
    }

    /// <summary>
    /// Fond et bordure de la pastille, tous deux dans la couleur d'accent à
    /// faible opacité. Voile translucide et non mélange opaque comme
    /// <see cref="Chip"/> : la pastille repose sur l'entête, dont le fond est
    /// déjà teinté par la fiche, et un aplat y ferait une tache.
    /// </summary>
    private static void DrawPill(ImDrawListPtr dl, Vector2 origin, Vector2 size, Vector4 accent)
    {
        var rounding = Theme.S(Theme.RadiusPill);

        dl.AddRectFilled(origin, origin + size,
                         ImGui.GetColorU32(Theme.Alpha(accent, FillAlpha)), rounding);

        dl.AddRect(origin, origin + size,
                   ImGui.GetColorU32(Theme.Alpha(accent, BorderAlpha)), rounding,
                   ImDrawFlags.None, Theme.S(1f));
    }

    // ─── Rendu par glyphe ─────────────────────────────────────────────────────

    /// <summary>
    /// Dessine le titre glyphe par glyphe depuis <paramref name="origin"/>, la
    /// police étant déjà poussée par l'appelant.
    ///
    /// Les largeurs sont mesurées glyphe par glyphe et additionnées. Supposer une
    /// largeur fixe donnerait un espacement irrégulier dès qu'un « i » côtoie un
    /// « M ». ImGui n'applique pas de crénage, la somme des largeurs mesurées vaut
    /// donc exactement celle de la chaîne entière, et l'espacement reste celui du
    /// rendu ordinaire quelle que soit la taille de police retenue.
    /// </summary>
    /// <param name="offsetY">
    /// Décalage vertical par glyphe, en pixels déjà mis à l'échelle. Il ne modifie
    /// pas la place réservée : l'amplitude tient dans le rembourrage vertical de
    /// la pastille, et faire grandir celle-ci décalerait tout ce qui suit selon
    /// l'animation choisie.
    /// </param>
    private static void PerGlyph(ImDrawListPtr dl, Vector2 origin, string text,
                                 Func<int, int, Vector4> color,
                                 Func<int, int, float>? offsetY = null)
    {
        var glyphs = TextElements(text);
        var x      = 0f;

        for (var i = 0; i < glyphs.Length; i++)
        {
            var y = offsetY?.Invoke(i, glyphs.Length) ?? 0f;

            dl.AddText(origin + new Vector2(x, y),
                       ImGui.GetColorU32(color(i, glyphs.Length)), glyphs[i]);

            // Largeur réelle du glyphe, mesurée dans la police courante.
            x += ImGui.CalcTextSize(glyphs[i]).X;
        }
    }

    /// <summary>
    /// Découpe en éléments de texte et non en <c>char</c> : une paire de
    /// substitution ou une lettre suivie d'un accent combinant se dessinerait
    /// sinon en deux morceaux.
    /// </summary>
    private static string[] TextElements(string text)
    {
        var elements = new List<string>(text.Length);
        var walker   = System.Globalization.StringInfo.GetTextElementEnumerator(text);

        while (walker.MoveNext()) elements.Add((string)walker.Current);

        return [.. elements];
    }

    // ─── Teintes ──────────────────────────────────────────────────────────────

    private static float Pulse(float speed) =>
        Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion
            ? 0.5f
            : (MathF.Sin((float)ImGui.GetTime() * speed) + 1f) * 0.5f;

    /// <summary>Avance cyclique dans [0, 1), de période <paramref name="seconds"/>.</summary>
    private static float Phase(float seconds) =>
        Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion
            ? 0.5f
            : (float)(ImGui.GetTime() / seconds % 1.0);

    /// <summary>
    /// Teinte d'un glyphe sous un bandeau clair qui parcourt le titre. La
    /// distance au bandeau se mesure de façon cyclique, sans quoi l'effet
    /// s'arrêterait net au dernier caractère.
    /// </summary>
    private static Vector4 Band(Vector4 from, Vector4 to, int index, int count, float seconds)
    {
        var position = count > 1 ? (float)index / (count - 1) : 0f;
        var distance = MathF.Abs(position - Phase(seconds));
        distance = MathF.Min(distance, 1f - distance);

        var intensity = MathF.Max(0f, 1f - distance / BandWidth) * 0.45f;
        return Theme.Mix(from, to, intensity);
    }

    /// <summary>
    /// Dégradé de teintes le long du titre. Saturation et valeur sont bornées
    /// pour rester lisibles sur le fond sombre du plugin.
    /// </summary>
    private static Vector4 Rainbow(int index, int count)
    {
        var spread = count > 0 ? (float)index / count : 0f;
        var hue    = (Phase(9f) + spread * 0.5f) % 1f;

        return Theme.FromHsv(hue, 0.45f, 0.95f);
    }

    /// <summary>
    /// Dégradé bicolore figé le long du titre, du premier au dernier glyphe.
    /// Aucun appel à GetTime : c'est un habillage, pas une animation.
    /// </summary>
    private static Vector4 Ramp(Vector4 from, Vector4 to, int index, int count)
    {
        var position = count > 1 ? (float)index / (count - 1) : 0f;

        return Theme.Mix(from, to, position);
    }

    /// <summary>
    /// Décalage vertical d'un glyphe pour l'ondulation.
    ///
    /// Le retard cumulé sur toute la longueur du titre vaut un tiers de période :
    /// au-delà, les lettres partent dans tous les sens et le titre cesse de se
    /// lire comme un mot.
    ///
    /// « Animations réduites » ramène le décalage à zéro, valeur médiane de
    /// l'oscillation : le titre s'aligne au lieu de se figer en escalier, ce
    /// qu'un simple gel de la phase produirait.
    /// </summary>
    private static float WaveOffset(int index, int count)
    {
        if (Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion) return 0f;

        var lag = count > 1 ? (float)index / (count - 1) * 0.33f : 0f;

        // Période de 4 secondes, exprimée en tours entiers pour que le retard
        // par glyphe se lise directement comme une fraction de cycle.
        var angle = ((float)ImGui.GetTime() / 4f - lag) * MathF.Tau;

        return MathF.Sin(angle) * Theme.S(WaveAmplitude);
    }

    /// <summary>
    /// Halo : ImGui ne sait pas border un texte. La seule voie est de dessiner la
    /// chaîne plusieurs fois, décalée et atténuée, avant le texte net. Quatre
    /// copies suffisent, une par direction : les diagonales doubleraient le coût
    /// pour un gain invisible à un écart d'un pixel et demi.
    ///
    /// Cinq primitives par image, la respiration ne portant que sur l'opacité du
    /// halo. Le texte net, lui, ne bouge jamais : c'est ce qui rend le titre
    /// lisible en permanence.
    /// </summary>
    private static void Halo(ImDrawListPtr dl, Vector2 origin, string text, Vector4 accent)
    {
        // Période d'environ 6 secondes (2π / 6 ≈ 1,05 rad/s).
        var glow = ImGui.GetColorU32(
            Theme.Alpha(Theme.Mix(accent, Theme.Text, 0.35f), 0.22f + Pulse(1.05f) * 0.26f));

        var spread = Theme.S(HaloSpread);

        dl.AddText(origin + new Vector2(-spread, 0f), glow, text);
        dl.AddText(origin + new Vector2(spread, 0f),  glow, text);
        dl.AddText(origin + new Vector2(0f, -spread), glow, text);
        dl.AddText(origin + new Vector2(0f, spread),  glow, text);

        dl.AddText(origin, ImGui.GetColorU32(accent), text);
    }

    /// <summary>
    /// Vacillement de néon : quelques sursauts d'intensité en début de cycle,
    /// puis une longue plage stable. Le cycle dure 6 secondes et le vacillement
    /// n'en occupe que le premier sixième, sans quoi l'effet passerait du clin
    /// d'œil au stroboscope, ce qui n'a rien à faire en surimpression du jeu.
    ///
    /// « Animations réduites » fige la phase à 0,5, donc en pleine plage stable :
    /// le titre reste allumé, ce qui est bien la valeur médiane de l'effet.
    /// </summary>
    private static Vector4 Neon(Vector4 accent)
    {
        var phase = Phase(6f);

        // Le tube s'amorce : trois battements rapides avant de tenir. La valeur
        // absolue du cosinus donne des creux marqués et des sommets plats, plus
        // proches d'un tube qui accroche que d'une sinusoïde molle.
        var level = phase < 0.17f
            ? 0.45f + 0.55f * MathF.Abs(MathF.Cos(phase * 55f))
            : 1f;

        return Theme.Alpha(Theme.Mix(accent, Theme.Text, 0.25f * level),
                           0.45f + 0.55f * level);
    }
}

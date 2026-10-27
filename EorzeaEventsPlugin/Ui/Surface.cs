using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Rendu des surfaces : ombre portée, fond, bordure et liseré, et la nuit des
/// fenêtres, ses halos et le ciel étoilé des fiches.
///
/// Sur fond sombre, une carte qui n'a qu'un fond légèrement différent reste
/// invisible. Ce qui la détache, c'est la combinaison de trois signaux :
/// une ombre diffuse en dessous, une bordure nette, et un liseré clair sur
/// l'arête haute qui simule une source de lumière zénithale.
/// </summary>
internal static class Surface
{
    /// <summary>
    /// Ombre portée diffuse. Empile des contours de plus en plus larges et de
    /// plus en plus transparents : bien moins coûteux qu'un flou réel, et
    /// visuellement suffisant à cette échelle.
    /// </summary>
    public static void Shadow(ImDrawListPtr dl, Vector2 min, Vector2 max,
                              float rounding, float spread = 6f, float opacity = 1f)
    {
        var steps = Math.Max(1, (int)MathF.Round(Theme.S(spread)));

        for (var i = steps; i >= 1; i--)
        {
            var t     = i / (float)steps;          // 1 au plus large, ~0 au plus serré
            var alpha = Theme.Shadow.W * (1f - t) * (1f - t) * 0.5f * opacity;
            if (alpha <= 0.002f) continue;

            var grow = new Vector2(i, i);
            dl.AddRect(
                min - grow,
                max + grow + new Vector2(0f, Theme.S(1f)), // décalage bas : lumière zénithale
                ImGui.GetColorU32(Theme.Alpha(Theme.Shadow, alpha)),
                rounding + i,
                ImDrawFlags.None,
                1f);
        }
    }

    /// <summary>
    /// Peint une surface complète : ombre, fond, liseré haut, puis bordure.
    /// </summary>
    public static void Panel(ImDrawListPtr dl, Vector2 min, Vector2 max,
                             Vector4 background, Vector4? border = null,
                             float? rounding = null, bool shadow = true,
                             bool highlight = true, float shadowSpread = 6f)
    {
        var r = rounding ?? Theme.S(Theme.RadiusCard);

        if (shadow) Shadow(dl, min, max, r, shadowSpread);

        dl.AddRectFilled(min, max, ImGui.GetColorU32(background), r);

        if (highlight)
        {
            // Le liseré s'arrête avant les angles pour ne pas déborder de l'arrondi.
            var inset = r * 0.7f;
            dl.AddLine(
                new Vector2(min.X + inset, min.Y + 0.5f),
                new Vector2(max.X - inset, min.Y + 0.5f),
                ImGui.GetColorU32(Theme.Highlight),
                1f);
        }

        dl.AddRect(min, max, ImGui.GetColorU32(border ?? Theme.Border), r, ImDrawFlags.None, 1f);
    }

    /// <summary>
    /// Coordonnées de texture d'un recadrage « couvrir » : l'image remplit le
    /// cadre sans jamais être déformée, le débord étant rogné au centre.
    ///
    /// Partagé par la bannière des cartes et le portrait des fiches RP : sans ce
    /// calcul, des UV 0→1 étirent toute image dont le ratio diffère du cadre, ce
    /// qui devient franchement visible sur un grand portrait.
    /// </summary>
    public static (Vector2 Uv0, Vector2 Uv1) CoverUv(int textureWidth, int textureHeight,
                                                     float frameWidth, float frameHeight)
    {
        if (textureWidth <= 0 || textureHeight <= 0 || frameWidth <= 0f || frameHeight <= 0f)
            return (Vector2.Zero, Vector2.One);

        var imageAspect = textureWidth / (float)textureHeight;
        var frameAspect = frameWidth / frameHeight;

        if (imageAspect >= frameAspect)
        {
            // Image plus large que le cadre : on rogne les côtés.
            var span = frameAspect / imageAspect;
            var u0   = (1f - span) * 0.5f;
            return (new Vector2(u0, 0f), new Vector2(1f - u0, 1f));
        }

        // Image plus haute : on rogne en haut et en bas.
        var vSpan = imageAspect / frameAspect;
        var v0    = (1f - vSpan) * 0.5f;
        return (new Vector2(0f, v0), new Vector2(1f, 1f - v0));
    }

    /// <summary>
    /// Barre d'accent verticale collée au bord gauche d'une surface, qui
    /// épouse ses coins arrondis.
    ///
    /// ImGui ramène l'arrondi d'un rectangle à la moitié de sa plus petite
    /// dimension : une barre de 3 px ne s'arrondit que d'un pixel et demi, et
    /// ses angles droits dépassaient de la courbe de la carte, en haut et en
    /// bas. On peint donc la forme entière de la carte, rognée à la largeur de
    /// la barre : le bord gauche suit exactement l'arrondi.
    /// </summary>
    public static void AccentBar(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                 Vector4 color, float width = 3f, float? rounding = null)
    {
        var r = rounding ?? Theme.S(Theme.RadiusCard);

        dl.PushClipRect(min, new Vector2(min.X + Theme.S(width), max.Y), true);
        dl.AddRectFilled(min, max, ImGui.GetColorU32(color), r, ImDrawFlags.RoundCornersLeft);
        dl.PopClipRect();
    }

    // ─── Lumière ──────────────────────────────────────────────────────────────

    /// <summary>Paliers du halo, du centre au bord : (fraction du rayon, fraction de l'alpha).</summary>
    /// <remarks>
    /// Un seul palier donnerait un cône, dont la pointe se voit au centre. Le
    /// palier du milieu tasse la lumière vers le cœur, comme un halo réel.
    /// </remarks>
    private static readonly (float Radius, float Alpha)[] HaloStops = [(0f, 1f), (0.45f, 0.45f), (1f, 0f)];

    /// <summary>
    /// Tache de lumière elliptique, pleine au centre, nulle au bord.
    ///
    /// ImGui n'a ni flou ni dégradé radial. Le halo est un éventail de
    /// triangles dont les sommets portent la couleur : la carte graphique
    /// interpole d'un sommet à l'autre, ce qui donne un dégradé continu, sans les
    /// marches qu'aurait une pile de cercles. Repris de Linkpearl.
    /// </summary>
    public static void Halo(ImDrawListPtr dl, Vector2 center, Vector2 radius, Vector4 color, int segments = 48)
    {
        var uv    = ImGui.GetFontTexUvWhitePixel();
        var quads = segments * (HaloStops.Length - 1);

        // Six sommets par quadrilatère, sans partage : PrimVtx écrit l'index
        // avec le sommet, ce qui évite de tenir soi-même le compte des indices.
        dl.PrimReserve(quads * 6, quads * 6);

        for (var ring = 1; ring < HaloStops.Length; ring++)
        {
            var (innerRadius, innerAlpha) = HaloStops[ring - 1];
            var (outerRadius, outerAlpha) = HaloStops[ring];

            var inner = ImGui.GetColorU32(Theme.Alpha(color, color.W * innerAlpha));
            var outer = ImGui.GetColorU32(Theme.Alpha(color, color.W * outerAlpha));

            for (var i = 0; i < segments; i++)
            {
                var a = OnEllipse(center, radius * innerRadius, i, segments);
                var b = OnEllipse(center, radius * innerRadius, i + 1, segments);
                var c = OnEllipse(center, radius * outerRadius, i + 1, segments);
                var d = OnEllipse(center, radius * outerRadius, i, segments);

                dl.PrimVtx(a, uv, inner);
                dl.PrimVtx(d, uv, outer);
                dl.PrimVtx(c, uv, outer);

                dl.PrimVtx(a, uv, inner);
                dl.PrimVtx(c, uv, outer);
                dl.PrimVtx(b, uv, inner);
            }
        }
    }

    private static Vector2 OnEllipse(Vector2 center, Vector2 radius, int step, int segments)
    {
        var angle = MathF.Tau * step / segments;
        return center + new Vector2(MathF.Cos(angle) * radius.X, MathF.Sin(angle) * radius.Y);
    }

    /// <summary>
    /// Le halo d'un élément actif, autour de son rectangle arrondi.
    ///
    /// Même technique que l'ombre : des contours de plus en plus larges et
    /// transparents. À peindre avant l'élément, qui recouvre l'intérieur.
    /// </summary>
    public static void Glow(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding,
                            Vector4 color, float spread = 7f)
    {
        var steps = Math.Max(1, (int)MathF.Round(Theme.S(spread)));

        for (var i = 1; i <= steps; i++)
        {
            var t     = i / (float)steps;
            var alpha = color.W * 0.30f * (1f - t) * (1f - t);
            if (alpha <= 0.004f) continue;

            var grow = new Vector2(i, i);
            dl.AddRect(min - grow, max + grow, ImGui.GetColorU32(Theme.Alpha(color, alpha)),
                       rounding + i, ImDrawFlags.None, 1.5f);
        }
    }

    /// <summary>
    /// Dégradé vertical dans un rectangle dont seuls les angles hauts sont
    /// arrondis, ou aucun.
    ///
    /// <c>AddRectFilledMultiColor</c> ne sait pas arrondir : le dégradé couvre
    /// le milieu, et une bande unie de la hauteur du rayon, à la couleur du
    /// haut, porte les angles. La couture ne se voit pas, les couleurs se
    /// rejoignent exactement.
    /// </summary>
    private static void VerticalGradient(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                         uint top, uint bottom, float rounding, bool roundTop, bool roundBottom)
    {
        var r = Math.Min(rounding, (max.Y - min.Y) * 0.5f);

        var gradientTop    = roundTop ? min.Y + r : min.Y;
        var gradientBottom = roundBottom ? max.Y - r : max.Y;

        if (roundTop)
            dl.AddRectFilled(min, new Vector2(max.X, min.Y + r + 1f), top, r, ImDrawFlags.RoundCornersTop);

        dl.AddRectFilledMultiColor(new Vector2(min.X, gradientTop), new Vector2(max.X, gradientBottom),
                                   top, top, bottom, bottom);

        if (roundBottom)
            dl.AddRectFilled(new Vector2(min.X, max.Y - r - 1f), max, bottom, r, ImDrawFlags.RoundCornersBottom);
    }

    /// <summary>
    /// Le fond des fenêtres : la nuit en dégradé, un halo de cristal au centre
    /// du haut et un voile d'or plus large et plus pâle.
    ///
    /// Les halos sont plaqués contre le bord haut : centrés plus bas, ils
    /// tomberaient derrière le texte des pages, qui perdrait son contraste.
    /// L'opacité est celle de la fenêtre, pour que le décor du jeu transparaisse
    /// partout de la même façon.
    /// </summary>
    /// <param name="roundTop">Faux sous une barre de titre native, qui porte déjà les angles du haut.</param>
    public static void NightBackground(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                       float rounding, bool roundTop, float opacity)
    {
        var top    = ImGui.GetColorU32(Theme.Alpha(Theme.NightTop, opacity));
        var bottom = ImGui.GetColorU32(Theme.Alpha(Theme.NightBottom, opacity));

        VerticalGradient(dl, min, max, top, bottom, rounding, roundTop, roundBottom: true);

        var width  = max.X - min.X;
        var center = min.X + width * 0.5f;

        dl.PushClipRect(min, max, true);
        Halo(dl, new Vector2(center, min.Y), new Vector2(width * 0.5f, Theme.S(240f)),
             Theme.Alpha(Theme.HaloGold, Theme.HaloGold.W * opacity));
        Halo(dl, new Vector2(center, min.Y + Theme.S(30f)), new Vector2(width * 0.30f, Theme.S(170f)),
             Theme.Alpha(Theme.HaloCrystal, Theme.HaloCrystal.W * opacity));
        dl.PopClipRect();
    }

    // ─── Ciel étoilé ──────────────────────────────────────────────────────────

    private readonly record struct Star(float X, float Y, float Size, float Alpha, float Speed, float Phase, bool Gold);

    private readonly record struct Mote(float X, float Y, float Period, float Offset, bool Crystal);

    /// <summary>
    /// Le ciel, tiré une fois pour toutes avec une graine fixe : il est le même
    /// d'une fiche à l'autre et d'une session à l'autre, et rien n'est alloué à
    /// chaque image.
    /// </summary>
    private static readonly Star[] Stars = BuildStars();

    private static readonly Mote[] Motes = BuildMotes();

    private static Star[] BuildStars()
    {
        var random = new Random(7);
        var stars  = new Star[70];

        for (var i = 0; i < stars.Length; i++)
        {
            var twinkles = random.NextDouble() < 0.3;
            stars[i] = new Star(
                X:     (float)random.NextDouble(),
                Y:     (float)random.NextDouble() * 0.72f,
                Size:  random.NextDouble() < 0.85 ? 0.75f : 1.25f,
                Alpha: 0.35f + (float)random.NextDouble() * 0.5f,
                Speed: twinkles ? 1.2f + (float)random.NextDouble() * 1.8f : 0f,
                Phase: (float)random.NextDouble() * MathF.Tau,
                Gold:  random.NextDouble() < 0.2);
        }

        return stars;
    }

    private static Mote[] BuildMotes()
    {
        var random = new Random(11);
        var motes  = new Mote[10];

        for (var i = 0; i < motes.Length; i++)
            motes[i] = new Mote(
                X:       0.05f + (float)random.NextDouble() * 0.9f,
                Y:       0.35f + (float)random.NextDouble() * 0.45f,
                Period:  5f + (float)random.NextDouble() * 4f,
                Offset:  (float)random.NextDouble() * 6f,
                Crystal: i % 2 == 0);

        return motes;
    }

    /// <summary>
    /// Ciel étoilé d'en-tête de fiche : dégradé, deux halos, étoiles fixes dont
    /// certaines scintillent, et quelques poussières qui dérivent vers le haut.
    /// Le bas va jusqu'à <paramref name="bottom"/>, en général le milieu rendu
    /// transparent, pour s'effacer sur la surface qui le porte.
    ///
    /// Les étoiles sont blanches ou dorées quelle que soit la teinte du ciel :
    /// teintées, elles disparaîtraient dans un ciel de la même couleur.
    /// </summary>
    /// <param name="animated">Faux : ni scintillement ni poussières, le ciel reste figé.</param>
    public static void StarrySky(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding,
                                 Vector4 top, Vector4 middle, Vector4 bottom,
                                 Vector4 glow, Vector4 glow2, bool animated)
    {
        var size = max - min;
        var cut  = min.Y + size.Y * 0.45f;

        VerticalGradient(dl, min, new Vector2(max.X, cut), ImGui.GetColorU32(top), ImGui.GetColorU32(middle),
                         rounding, roundTop: true, roundBottom: false);
        dl.AddRectFilledMultiColor(new Vector2(min.X, cut), max,
                                   ImGui.GetColorU32(middle), ImGui.GetColorU32(middle),
                                   ImGui.GetColorU32(bottom), ImGui.GetColorU32(bottom));

        dl.PushClipRect(min, max, true);

        Halo(dl, new Vector2(min.X + size.X * 0.5f, min.Y - Theme.S(10f)),
             new Vector2(size.X * 0.36f, size.Y * 0.75f), glow);
        Halo(dl, new Vector2(min.X + size.X * 0.72f, min.Y), new Vector2(size.X * 0.26f, size.Y * 0.6f), glow2);

        var time   = animated ? (float)ImGui.GetTime() : 0f;
        var white  = new Vector4(0.92f, 0.95f, 1f, 1f);
        var golden = Theme.ActionHover;

        foreach (var star in Stars)
        {
            var alpha = star.Alpha;
            if (animated && star.Speed > 0f)
                alpha *= 0.55f + 0.45f * MathF.Sin(time * star.Speed + star.Phase);

            dl.AddCircleFilled(min + new Vector2(star.X * size.X, star.Y * size.Y), Theme.S(star.Size),
                               ImGui.GetColorU32(Theme.Alpha(star.Gold ? golden : white, alpha)), 8);
        }

        if (animated)
        {
            var drift = Theme.S(36f, -70f);

            foreach (var mote in Motes)
            {
                // Chaque poussière naît, monte en dérivant et s'éteint, puis
                // recommence : un cinquième du trajet pour paraître, le reste pour
                // s'effacer.
                var progress = (time + mote.Offset) % mote.Period / mote.Period;
                var alpha    = progress < 0.2f ? progress / 0.2f : (1f - progress) / 0.8f;
                var center   = min + new Vector2(mote.X * size.X, mote.Y * size.Y) + drift * progress;
                var color    = mote.Crystal ? Theme.Accent : golden;

                Halo(dl, center, new Vector2(Theme.S(5f)), Theme.Alpha(color, 0.5f * alpha), 16);
                dl.AddCircleFilled(center, Theme.S(1.4f), ImGui.GetColorU32(Theme.Alpha(color, 0.95f * alpha)), 8);
            }
        }

        dl.PopClipRect();
    }
}

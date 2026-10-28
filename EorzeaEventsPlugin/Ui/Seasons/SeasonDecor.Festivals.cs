using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Le décor des quatre autres fêtes du jeu : Little Ladies' Day, le festival du
/// Gold Saucer, la Moonfire Faire et The Rising. Chacune garde un seul élément
/// fort, et quelques détails autour.
///
/// Les positions reprennent les maquettes, sur une fenêtre de 1040 × 680 :
/// ce qui pend du haut se cale sur le bord droit, ce qui pose au sol sur la
/// barre d'état. Une longue liste recouvre la scène, comme elle recouvre déjà
/// la lune d'Halloween.
/// </summary>
internal static partial class SeasonDecor
{
    private static readonly Particle[] Notes    = Scatter(41, 2);
    private static readonly Particle[] Coins    = Scatter(31, 6);
    private static readonly Particle[] Embers   = Scatter(37, 3);
    private static readonly Particle[] Lanterns = Scatter(43, 2);
    private static readonly Particle[] Shards   = Scatter(47, 3);
    private static readonly Particle[] Feathers = Scatter(53, 3);

    /// <summary>Les étincelles du Gold Saucer, en fraction de la fenêtre, et leur décalage.</summary>
    private static readonly (float X, float Y, float Size, float Delay)[] Sparkles =
        [(0.40f, 0.62f, 14f, 0f), (0.54f, 0.80f, 12f, 1.2f), (0.87f, 0.82f, 16f, 2f)];

    // ─── Fête des demoiselles ─────────────────────────────────────────────────

    /// <summary>
    /// Une branche de pêcher en fleurs traverse le haut de la fenêtre, deux
    /// lanternes de papier y pendent. Quelques pétales, deux notes de musique,
    /// une brume rose au pied de la fenêtre.
    /// </summary>
    private static void LittleLadies(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size = max - min;
        Surface.Halo(dl, new Vector2(min.X + size.X * 0.72f, max.Y), new Vector2(size.X * 0.45f, Theme.S(170f)),
                     Theme.Hex(0xFF80B8, 0.16f * alpha));

        SeasonArt.Draw(dl, "blossom-branch", new Vector2(max.X - Theme.S(620f), min.Y - Theme.S(60f)),
                       Theme.S(640f, 160f), alpha);
        Hanging(dl, "bonbori", new Vector2(max.X - Theme.S(308f), min.Y + Theme.S(41f)), Theme.S(36f, 58.5f),
                Sway(t, 0f), alpha);
        Hanging(dl, "bonbori", new Vector2(max.X - Theme.S(415f), min.Y + Theme.S(62f)), Theme.S(36f, 58.5f),
                Sway(t, 2f), alpha);

        // Six pétales et une fleur entière, en diagonale comme ceux de Pâques.
        var span = size.Y + Theme.S(40f);
        for (var i = 0; i < 7; i++)
        {
            var petal  = Petals[i];
            var fall   = (petal.Y * span + t * Theme.S(16f + petal.Speed * 12f)) % span;
            var x      = Wrap(petal.X * size.X - fall * 0.35f + MathF.Sin(t * 0.9f + petal.Phase) * Theme.S(6f), size.X);
            var center = new Vector2(min.X + x, min.Y - Theme.S(20f) + fall);
            var angle  = t * petal.Spin * 1.6f + petal.Phase;

            if (i == 6)
                SeasonArt.Rotated(dl, "blossom", center, Theme.S(16f, 16f), angle, alpha * 0.9f);
            else
            {
                var side = Theme.S(12f + petal.Size * 4f);
                SeasonArt.Rotated(dl, petal.Variant % 2 == 0 ? "petal-pink" : "petal-blush", center,
                                  new Vector2(side, side * 0.75f), angle, alpha * 0.85f);
            }
        }

        // Les notes montent comme les feux follets : naître, monter, s'éteindre.
        foreach (var note in Notes)
        {
            var (progress, fade) = Cycle(t, note, 7f + note.Speed * 2f, 0.2f);
            var start = new Vector2(min.X + size.X * (0.3f + note.X * 0.6f), min.Y + size.Y * (0.5f + note.Y * 0.35f));
            SeasonArt.Centered(dl, "note", start + Theme.S(36f, -70f) * progress, Theme.S(15f, 18f), alpha * 0.7f * fade);
        }
    }

    /// <summary>Le balancement d'une lanterne : quatre degrés de part et d'autre, en huit secondes.</summary>
    private static float Sway(float t, float delay) => MathF.Sin((t + delay) * MathF.Tau / 8f) * 4f * MathF.PI / 180f;

    /// <summary>Une image suspendue par le milieu de son bord haut, tournée autour de ce point.</summary>
    private static void Hanging(ImDrawListPtr dl, string name, Vector2 anchor, Vector2 size, float angle, float alpha)
    {
        var (sin, cos) = MathF.SinCos(angle);
        SeasonArt.Rotated(dl, name, anchor + new Vector2(-sin, cos) * size.Y * 0.5f, size, angle, alpha);
    }

    // ─── Festival du Gold Saucer ──────────────────────────────────────────────

    /// <summary>
    /// Une étoile au néon en bas à gauche, quelques jetons qui tombent, des
    /// étincelles. La rampe d'ampoules et le nom en néon sont dans la barre de
    /// titre (<see cref="TitleEdge"/>, <see cref="BrandGlow"/>).
    /// </summary>
    private static void GoldSaucer(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size = max - min;

        // Le néon grésille : un bref creux toutes les sept secondes environ.
        var flicker = t % 7.3f is > 6.4f and < 6.55f ? 0.35f : 1f;
        SeasonArt.Draw(dl, "neon-star", new Vector2(min.X + Theme.S(256f), max.Y - Theme.S(210f)), Theme.S(130f, 130f),
                       alpha * 0.85f * flicker);

        foreach (var coin in Coins)
        {
            var period   = 8.5f + coin.Speed * 2.5f;
            var progress = (t + coin.Phase * 2f) % period / period;
            var fade     = progress < 0.08f ? progress / 0.08f : progress > 0.85f ? (1f - progress) / 0.15f : 1f;
            var side     = Theme.S(14f + coin.Size * 4f);
            var center   = new Vector2(min.X + size.X * (0.28f + coin.X * 0.68f),
                                       min.Y - Theme.S(40f) + progress * (size.Y + Theme.S(80f)));
            SeasonArt.Rotated(dl, "coin", center, new Vector2(side, side), progress * 5.2f + coin.Phase, alpha * fade);
        }

        foreach (var (x, y, side, delay) in Sparkles)
        {
            var glow = 0.15f + 0.85f * (0.5f + 0.5f * MathF.Sin((t + delay) * MathF.Tau / 2.8f));
            SeasonArt.Centered(dl, "sparkle", min + size * new Vector2(x, y), Theme.S(side, side), alpha * glow);
        }
    }

    // ─── Les Feux de la Mort ──────────────────────────────────────────────────

    /// <summary>
    /// Une plage de nuit au pied de la fenêtre : la mer, la lune orange qui
    /// s'y reflète, un palmier sur la dune. Deux feux d'artifice en haut,
    /// quelques lanternes et braises qui montent.
    /// </summary>
    private static void Moonfire(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size   = max - min;
        var seaTop = max.Y - Theme.S(110f);

        // La lueur du couchant au-dessus de l'horizon : orange en bas, rose, puis rien.
        var band   = Theme.S(150f);
        var clear  = ImGui.GetColorU32(Theme.Hex(0xFF408C, 0f));
        var rose   = ImGui.GetColorU32(Theme.Hex(0xFF408C, 0.10f * alpha));
        var ember  = ImGui.GetColorU32(Theme.Hex(0xFF763A, 0.29f * alpha));
        var middle = seaTop - band * 0.45f;
        dl.AddRectFilledMultiColor(new Vector2(min.X, seaTop - band), new Vector2(max.X, middle), clear, clear, rose, rose);
        dl.AddRectFilledMultiColor(new Vector2(min.X, middle), new Vector2(max.X, seaTop), rose, rose, ember, ember);

        Burst(dl, "burst-gold", new Vector2(min.X + size.X * 0.466f, min.Y + Theme.S(35f)), Theme.S(130f), 0f, t, alpha * 0.85f);
        Burst(dl, "burst-pink", new Vector2(min.X + size.X * 0.79f, min.Y + Theme.S(24f)), Theme.S(120f), 1.6f, t, alpha * 0.8f);

        // La lune se lève de la mer : la mer, peinte après, en cache le bas.
        var moonX = max.X - Theme.S(235f);
        SeasonArt.Centered(dl, "moon-full", new Vector2(moonX, seaTop - Theme.S(10f)), Theme.S(150f, 150f), alpha);

        dl.AddRectFilledMultiColor(new Vector2(min.X, seaTop), max,
                                   ImGui.GetColorU32(Theme.Hex(0x14425A, alpha)), ImGui.GetColorU32(Theme.Hex(0x14425A, alpha)),
                                   ImGui.GetColorU32(Theme.Hex(0x06131F, alpha)), ImGui.GetColorU32(Theme.Hex(0x06131F, alpha)));

        var depth = max.Y - seaTop;
        for (var y = seaTop + Theme.S(1f); y < max.Y; y += Theme.S(9f))
            dl.AddLine(new Vector2(min.X, y), new Vector2(max.X, y),
                       ImGui.GetColorU32(Theme.Hex(0x8FD8FF, 0.13f * alpha * (1f - (y - seaTop) / depth))), 1f);

        // Le reflet : des traits de lumière qui rétrécissent en s'éloignant, et frémissent.
        var shimmer = 0.5f + 0.5f * MathF.Sin(t * MathF.Tau / 5.2f);
        for (var k = 0; k < 8; k++)
        {
            var half = Theme.S(35f) * (1f - k / 9f) * (1f + 0.15f * shimmer);
            var y    = seaTop + Theme.S(4f + k * 10f);
            dl.AddRectFilled(new Vector2(moonX - half, y), new Vector2(moonX + half, y + Theme.S(3f)),
                             ImGui.GetColorU32(Theme.Hex(0xFFC478, 0.9f * alpha * (1f - k / 8f) * (1f - 0.4f * shimmer))),
                             Theme.S(1.5f));
        }

        // La dune, sous la barre d'état ; le palmier y plante son tronc.
        Dune(dl, min, max, alpha);
        var palm  = Theme.S(140f, 196f);
        var trunk = max.X - Theme.S(120f) + palm.X * 0.3f;
        SeasonArt.Draw(dl, "palm", new Vector2(max.X - Theme.S(120f), DuneTop(trunk, min, max) + Theme.S(6f) - palm.Y),
                       palm, alpha);

        foreach (var lantern in Lanterns)
        {
            var (progress, fade) = Cycle(t, lantern, 14f + lantern.Speed * 2f, 0.15f);
            var center = new Vector2(min.X + size.X * (0.3f + lantern.X * 0.3f), max.Y - Theme.S(20f) - progress * Theme.S(460f));
            SeasonArt.Centered(dl, "lantern", center, Theme.S(20f, 25f), alpha * fade);
        }

        foreach (var spark in Embers)
            Ember(dl, min + size * new Vector2(0.25f, 0.5f), size * new Vector2(0.7f, 0.4f), spark, t,
                  Theme.Hex(0xFFC266), Theme.Hex(0xFF8C3C), alpha);
    }

    /// <summary>
    /// La dune : le haut d'une ellipse plus large que la fenêtre, centrée sous
    /// son bord bas, qui n'en dépasse que le sommet.
    /// </summary>
    private static (Vector2 Center, Vector2 Radius) DuneShape(Vector2 min, Vector2 max) =>
        (new Vector2((min.X + max.X) * 0.5f, max.Y + Theme.S(40f)), new Vector2((max.X - min.X) * 0.5f + Theme.S(60f), Theme.S(86f)));

    private static void Dune(ImDrawListPtr dl, Vector2 min, Vector2 max, float alpha)
    {
        var (center, radius) = DuneShape(min, max);

        const int steps = 32;
        for (var i = 0; i <= steps; i++)
        {
            var (sin, cos) = MathF.SinCos(MathF.PI + MathF.PI * i / steps);
            dl.PathLineTo(center + new Vector2(cos * radius.X, sin * radius.Y));
        }

        dl.PathFillConvex(ImGui.GetColorU32(Theme.Hex(0x040B13, alpha)));
    }

    /// <summary>La hauteur de la dune à une abscisse donnée.</summary>
    private static float DuneTop(float x, Vector2 min, Vector2 max)
    {
        var (center, radius) = DuneShape(min, max);
        var u = Math.Clamp((x - center.X) / radius.X, -1f, 1f);
        return center.Y - radius.Y * MathF.Sqrt(1f - u * u);
    }

    // ─── Fête de la Commémoration ─────────────────────────────────────────────

    /// <summary>
    /// Un phénix de lumière s'élève au pied de la fenêtre, devant des rayons
    /// qui tournent lentement, à côté des cristaux de Mor Dhona. Quelques
    /// plumes, éclats de cristal et braises.
    /// </summary>
    private static void Rising(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size    = max - min;
        var phoenix = new Vector2(min.X + size.X * 0.594f, max.Y - Theme.S(121f));

        Rays(dl, phoenix, Theme.S(160f), t, alpha);
        SeasonArt.Centered(dl, "phoenix", phoenix + new Vector2(0f, MathF.Sin(t * 0.8f) * Theme.S(3f)),
                           Theme.S(280f, 158f), alpha);
        SeasonArt.Draw(dl, "crystal-cluster", max - Theme.S(180f, 215f), Theme.S(150f, 184.6f), alpha);

        foreach (var shard in Shards)
        {
            var (progress, fade) = Cycle(t, shard, 14f + shard.Speed * 3f, 0.15f);
            var center = new Vector2(min.X + size.X * (0.3f + shard.X * 0.65f), max.Y - Theme.S(20f) - progress * Theme.S(460f));
            SeasonArt.Centered(dl, "crystal", center, Theme.S(14f, 21f), alpha * fade);
        }

        var span = size.Y + Theme.S(40f);
        foreach (var feather in Feathers)
        {
            var fall = (feather.Y * span + t * Theme.S(12f + feather.Speed * 8f)) % span;
            var x    = Wrap(size.X * (0.3f + feather.X * 0.6f) - fall * 0.2f + MathF.Sin(t * 0.7f + feather.Phase) * Theme.S(10f),
                            size.X);
            SeasonArt.Rotated(dl, "feather", new Vector2(min.X + x, min.Y - Theme.S(20f) + fall), Theme.S(12f, 24f),
                              MathF.Sin(t * 0.9f + feather.Phase) * 0.6f, alpha * 0.85f);
        }

        // Les braises naissent autour du phénix.
        foreach (var spark in Embers)
            Ember(dl, phoenix - Theme.S(80f, 40f), Theme.S(160f, 80f), spark, t, Theme.Hex(0xFFD36A), Theme.Hex(0xFF9640), alpha);
    }

    /// <summary>
    /// Vingt-quatre rayons qui partent du centre, chacun pâlissant vers son
    /// bout : un dégradé par sommet, comme <see cref="Surface.Halo"/>. Un tour
    /// en quatre-vingt-dix secondes.
    /// </summary>
    private static void Rays(ImDrawListPtr dl, Vector2 center, float radius, float t, float alpha)
    {
        const int count = 24;
        var uv   = ImGui.GetFontTexUvWhitePixel();
        var core = ImGui.GetColorU32(Theme.Hex(0xFFCE82, 0.10f * alpha));
        var edge = ImGui.GetColorU32(Theme.Hex(0xFFCE82, 0f));
        var half = MathF.PI / count / 3f;

        dl.PrimReserve(count * 3, count * 3);
        for (var i = 0; i < count; i++)
        {
            var angle = t * MathF.Tau / 90f + i * MathF.Tau / count;
            dl.PrimVtx(center, uv, core);
            dl.PrimVtx(center + Direction(angle - half) * radius, uv, edge);
            dl.PrimVtx(center + Direction(angle + half) * radius, uv, edge);
        }
    }

    private static Vector2 Direction(float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        return new Vector2(cos, sin);
    }

    // ─── Commun ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Avancement dans une boucle de <paramref name="period"/> secondes, et
    /// opacité : apparaître sur <paramref name="fadeIn"/>, puis s'effacer jusqu'au bout.
    /// </summary>
    private static (float Progress, float Fade) Cycle(float t, Particle particle, float period, float fadeIn)
    {
        var progress = (t + particle.Phase * 3f) % period / period;
        var fade     = progress < fadeIn ? progress / fadeIn : (1f - progress) / (1f - fadeIn);
        return (progress, fade);
    }

    /// <summary>
    /// Une braise : un point clair dans sa lueur, qui naît quelque part dans la
    /// zone donnée, monte en dérivant puis s'éteint.
    /// </summary>
    private static void Ember(ImDrawListPtr dl, Vector2 areaMin, Vector2 area, Particle spark, float t,
                              Vector4 core, Vector4 glow, float alpha)
    {
        var (progress, fade) = Cycle(t, spark, 7f + spark.Speed * 2f, 0.2f);
        var at = areaMin + area * new Vector2(spark.X, spark.Y) + Theme.S(36f, -70f) * progress;

        dl.AddCircleFilled(at, Theme.S(5f), ImGui.GetColorU32(Theme.Alpha(glow, 0.3f * alpha * fade)), 12);
        dl.AddCircleFilled(at, Theme.S(2.5f), ImGui.GetColorU32(Theme.Alpha(core, alpha * fade)), 10);
    }
}

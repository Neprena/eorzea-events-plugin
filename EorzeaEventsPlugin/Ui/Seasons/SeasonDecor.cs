using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Le décor des thèmes, peint là où la Nuit dorée peint ses halos : derrière
/// le contenu, si bien qu'une carte ou un texte ne passe jamais dessous.
/// S'y ajoutent les ornements du logo, la guirlande et le compte à rebours de
/// la barre de titre, la neige posée sur les cartes et le nid du menu.
///
/// Les particules sont tirées une fois pour toutes avec une graine fixe, comme
/// le ciel étoilé de <see cref="Surface"/> : rien n'est alloué à chaque image.
/// Toutes les dimensions passent par <see cref="Theme.S(float)"/>.
/// </summary>
internal static class SeasonDecor
{
    private readonly record struct Particle(float X, float Y, float Speed, float Size, float Phase, float Spin, int Variant);

    // Avant les particules : les champs statiques s'initialisent dans l'ordre du
    // fichier, et Scatter en lit la longueur.
    private static readonly Vector4[] ConfettiColors =
        [Theme.Hex(0xE9BE5C), Theme.Hex(0xFF7FA8), Theme.Hex(0x6CCBFF), Theme.Hex(0x6FE0B0), Theme.Hex(0xF1ECE0)];

    private static readonly Particle[] Snow     = Scatter(11, 70);
    private static readonly Particle[] Flakes   = Scatter(23, 5);
    private static readonly Particle[] Petals   = Scatter(13, 16);
    private static readonly Particle[] Confetti = Scatter(17, 48);
    private static readonly Particle[] Wisps    = Scatter(19, 6);
    private static readonly Particle[] Hearts   = Scatter(29, 12);

    /// <summary>Les chauves-souris autour de la lune : décalage, envergure et battement, en pixels à 100 %.</summary>
    private static readonly (float X, float Y, float Width, float Period)[] Bats =
        [(-62f, -4f, 34f, 0.9f), (64f, -12f, 26f, 0.76f), (118f, 16f, 20f, 1.04f), (-130f, 18f, 18f, 0.82f)];

    private static readonly string[] Bulbs = ["bulb-gold", "bulb-red", "bulb-green", "bulb-ice"];

    /// <summary>Le rose de la Saint-Valentin, celui du cœur.</summary>
    public static readonly Vector4 Pink = Theme.Hex(0xFF7FA8);

    private static Particle[] Scatter(int seed, int count)
    {
        var random = new Random(seed);
        var list   = new Particle[count];

        for (var i = 0; i < count; i++)
            list[i] = new Particle(
                X:       (float)random.NextDouble(),
                Y:       (float)random.NextDouble(),
                Speed:   (float)random.NextDouble(),
                Size:    (float)random.NextDouble(),
                Phase:   (float)random.NextDouble() * MathF.Tau,
                Spin:    (float)random.NextDouble() * 2f - 1f,
                Variant: random.Next(ConfettiColors.Length));

        return list;
    }

    // ─── Fond ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Le décor de fond, sur toute la fenêtre. Appelé par
    /// <see cref="Shell.ThemedWindow"/> juste après la nuit.
    /// </summary>
    public static void Backdrop(ImDrawListPtr dl, Vector2 min, Vector2 max, float opacity)
    {
        if (!SeasonState.Decor) return;

        // Un peu plus présent que la fenêtre elle-même : à 40 % d'opacité, une
        // lune à 40 % ne se verrait plus.
        var alpha = Math.Clamp(opacity + 0.25f, 0.6f, 1f);
        var t     = SeasonState.Time;

        dl.PushClipRect(min, max, true);
        switch (SeasonState.Current)
        {
            case Season.Halloween: Halloween(dl, min, max, t, alpha); break;
            case Season.Christmas: Snowfall(dl, min, max, t, alpha);  break;
            case Season.Easter:    Petalfall(dl, min, max, t, alpha); break;
            case Season.NewYear:   NewYear(dl, min, max, t, alpha);   break;
            case Season.Valentine: Heartrise(dl, min, max, t, alpha); break;
        }
        dl.PopClipRect();
    }

    /// <summary>La lune derrière la barre de titre, ses chauves-souris, et des feux follets à la place des poussières.</summary>
    private static void Halloween(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size = max - min;
        var moon = new Vector2(min.X + size.X * 0.6f, min.Y + Theme.S(24f));
        SeasonArt.Centered(dl, "moon", moon, Theme.S(64f, 64f), alpha);

        foreach (var (x, y, width, period) in Bats)
        {
            // Les ailes battent : l'image s'écrase en hauteur puis se rouvre.
            var flap  = 0.55f + 0.45f * (0.5f + 0.5f * MathF.Sin(t * MathF.Tau / period));
            var drift = new Vector2(MathF.Sin(t * 0.35f + x) * 8f, MathF.Cos(t * 0.5f + y) * 3f);
            SeasonArt.Centered(dl, "bat", moon + Theme.S(x + drift.X, y + drift.Y),
                               Theme.S(width, width * 0.5f * flap), alpha);
        }

        foreach (var wisp in Wisps)
        {
            // Même cycle que les poussières des fiches : naître, monter, s'éteindre.
            var period   = 6f + wisp.Speed * 3f;
            var progress = (t + wisp.Phase) % period / period;
            var fade     = progress < 0.2f ? progress / 0.2f : (1f - progress) / 0.8f;
            var start    = new Vector2(min.X + size.X * (0.25f + wisp.X * 0.7f), min.Y + size.Y * (0.35f + wisp.Y * 0.5f));
            SeasonArt.Centered(dl, "wisp", start + Theme.S(36f, -70f) * progress, Theme.S(15f, 20f), alpha * fade);
        }
    }

    /// <summary>Flocons ronds de toutes tailles, et quelques cristaux plus gros qui tournent en tombant.</summary>
    private static void Snowfall(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var width = max.X - min.X;
        var span  = max.Y - min.Y + Theme.S(20f);

        foreach (var flake in Snow)
        {
            var fall = (flake.Y * span + t * Theme.S(12f + flake.Speed * 22f)) % span;
            var x    = min.X + flake.X * width + MathF.Sin(t * 0.7f + flake.Phase) * Theme.S(6f);
            var tone = new Vector4(1f, 1f, 1f, (0.45f + flake.Size * 0.45f) * alpha);
            dl.AddCircleFilled(new Vector2(x, min.Y - Theme.S(10f) + fall), Theme.S(0.9f + flake.Size * 1.5f),
                               ImGui.GetColorU32(tone), 8);
        }

        foreach (var flake in Flakes)
        {
            var fall = (flake.Y * span + t * Theme.S(8f + flake.Speed * 6f)) % span;
            var x    = min.X + (0.15f + flake.X * 0.8f) * width + MathF.Sin(t * 0.4f + flake.Phase) * Theme.S(10f);
            var side = Theme.S(12f + flake.Size * 6f);
            SeasonArt.Rotated(dl, "snowflake", new Vector2(x, min.Y - Theme.S(10f) + fall), new Vector2(side, side),
                              t * 0.3f * flake.Spin + flake.Phase, alpha * 0.85f);
        }
    }

    /// <summary>Pétales qui tombent en diagonale en tournant sur eux-mêmes.</summary>
    private static void Petalfall(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var width = max.X - min.X;
        var span  = max.Y - min.Y + Theme.S(40f);

        foreach (var petal in Petals)
        {
            var fall = (petal.Y * span + t * Theme.S(18f + petal.Speed * 14f)) % span;
            var x    = Wrap(petal.X * width - fall * 0.35f + MathF.Sin(t * 0.9f + petal.Phase) * Theme.S(6f), width);
            var side = Theme.S(11f + petal.Size * 5f);
            SeasonArt.Rotated(dl, petal.Variant % 2 == 0 ? "petal-pink" : "petal-blush",
                              new Vector2(min.X + x, min.Y - Theme.S(20f) + fall), new Vector2(side, side * 0.75f),
                              t * petal.Spin * 1.6f + petal.Phase, alpha * 0.9f);
        }
    }

    /// <summary>Les confettis du Nouvel An. Le feu d'artifice de minuit est à part, voir <see cref="Fireworks"/>.</summary>
    private static void NewYear(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size = max - min;
        var span = size.Y + Theme.S(20f);

        foreach (var bit in Confetti)
        {
            var fall   = (bit.Y * span + t * Theme.S(20f + bit.Speed * 20f)) % span;
            var center = new Vector2(min.X + bit.X * size.X + MathF.Sin(t + bit.Phase) * Theme.S(8f),
                                     min.Y - Theme.S(10f) + fall);
            var (sin, cos) = MathF.SinCos(t * bit.Spin * 3f + bit.Phase);
            var x = new Vector2(cos, sin) * Theme.S(3.2f);
            var y = new Vector2(-sin, cos) * Theme.S(1.6f);
            dl.AddQuadFilled(center - x - y, center + x - y, center + x + y, center - x + y,
                             ImGui.GetColorU32(Theme.Alpha(ConfettiColors[bit.Variant], 0.85f * alpha)));
        }
    }

    /// <summary>
    /// Le feu d'artifice des dix premières minutes de l'année. Sur la date
    /// seule, comme le compte à rebours qui le précède : la Heavensturn peut ne
    /// commencer qu'après minuit, et le thème du Nouvel An avec elle.
    /// </summary>
    public static bool FireworksNow =>
        SeasonState.EasterEggs && Plugin.Config.SeasonalDecor
        && SeasonState.Now is { Month: 1, Day: 1, Hour: 0, Minute: < 10 };

    /// <summary>Trois gerbes décalées, qui s'ouvrent puis s'éteignent, en haut de la zone donnée.</summary>
    public static void Fireworks(ImDrawListPtr dl, Vector2 min, Vector2 max)
    {
        var t    = SeasonState.Time;
        var size = max - min;

        ReadOnlySpan<(float X, float Y, float Size, float Delay)> bursts =
            [(0.35f, 0.14f, 120f, 0f), (0.58f, 0.08f, 96f, 1.1f), (0.78f, 0.18f, 132f, 2.2f)];

        foreach (var (x, y, side, delay) in bursts)
        {
            var progress = (t + delay) % 3.2f / 3.2f;
            var opening  = 0.2f + 0.88f * (1f - (1f - progress) * (1f - progress));
            var fade     = progress < 0.12f ? progress / 0.12f : progress > 0.7f ? (1f - progress) / 0.3f : 1f;
            SeasonArt.Centered(dl, "burst-gold", min + size * new Vector2(x, y), Theme.S(side, side) * opening, fade);
        }
    }

    /// <summary>Des cœurs qui montent lentement du bas de la fenêtre en se balançant, et s'effacent en route.</summary>
    private static void Heartrise(ImDrawListPtr dl, Vector2 min, Vector2 max, float t, float alpha)
    {
        var size = max - min;
        var span = size.Y + Theme.S(40f);

        foreach (var heart in Hearts)
        {
            var period   = 10f + heart.Speed * 6f;
            var progress = (t + heart.Phase * 3f) % period / period;
            var fade     = progress < 0.15f ? progress / 0.15f : (1f - progress) / 0.85f;
            var x        = min.X + size.X * (0.06f + heart.X * 0.9f) + MathF.Sin(t * 0.8f + heart.Phase) * Theme.S(10f);
            var y        = max.Y + Theme.S(20f) - progress * span;
            var side     = Theme.S(10f + heart.Size * 10f);

            SeasonArt.Rotated(dl, "heart", new Vector2(x, y), new Vector2(side, side * 22f / 24f),
                              MathF.Sin(t * 0.9f + heart.Phase) * 0.25f, alpha * fade * (0.45f + heart.Size * 0.4f));
        }
    }

    private static float Wrap(float value, float modulo) => (value % modulo + modulo) % modulo;

    // ─── Logo ─────────────────────────────────────────────────────────────────

    /// <summary>Le 1er avril : pompon de mog et « kupo » au bout des titres.</summary>
    private static bool Kupo => SeasonState.AprilFools && SeasonState.EasterEggs;

    /// <summary>Un événement est en cours et le logo porte un ornement : il passe en grand médaillon.</summary>
    public static bool Festive => SeasonState.Decor || Kupo;

    /// <summary>Ce qui passe derrière le logo : les oreilles de lapin. À peindre avant lui.</summary>
    public static void LogoBehind(ImDrawListPtr dl, Vector2 min, float side)
    {
        if (Kupo || !SeasonState.Decor || SeasonState.Current != Season.Easter) return;

        SeasonArt.Draw(dl, "bunny-ears", At(min, side, 0f, -8f), Scaled(side, 27f, 21.6f));
    }

    /// <summary>Ce qui se pose sur le logo : chapeau, bonnet, cœur, pompon. À peindre après lui.</summary>
    public static void LogoFront(ImDrawListPtr dl, Vector2 min, float side)
    {
        if (Kupo)
        {
            SeasonArt.Draw(dl, "pompom", At(min, side, -12f, -5f), Scaled(side, 20f, 20f));
            return;
        }

        if (!SeasonState.Decor) return;

        switch (SeasonState.Current)
        {
            case Season.Halloween: Hat(dl, "witch-hat", min, side, -9f, -9f, 32f, 24f, -14f);     break;
            case Season.Christmas: Hat(dl, "starlight-hat", min, side, -6f, -9f, 32f, 24f, -8f);  break;
            case Season.NewYear:   Hat(dl, "party-hat", min, side, -7f, -8f, 17f, 19.5f, -18f);   break;
            case Season.Valentine: Heartbeat(dl, min, side);                                       break;
        }
    }

    /// <summary>Un couvre-chef penché. Les coordonnées sont celles d'un logo de 28 px, la taille de la barre à 100 %.</summary>
    private static void Hat(ImDrawListPtr dl, string name, Vector2 min, float side,
                            float x, float y, float width, float height, float degrees)
    {
        var size = Scaled(side, width, height);
        SeasonArt.Rotated(dl, name, At(min, side, x, y) + size * 0.5f, size, degrees * MathF.PI / 180f);
    }

    /// <summary>Le cœur posé sur le coin du logo bat : deux pulsations rapprochées, puis un repos.</summary>
    private static void Heartbeat(ImDrawListPtr dl, Vector2 min, float side)
    {
        var phase = SeasonState.Time % 1.4f;
        var beat  = 1f + 0.14f * (Pulse(phase, 0.10f) + 0.7f * Pulse(phase, 0.32f));
        var size  = Scaled(side, 14f, 13f);
        SeasonArt.Centered(dl, "heart", At(min, side, 18f, -4f) + size * 0.5f, size * beat);
    }

    /// <summary>Une bosse brève centrée sur <paramref name="at"/>, nulle ailleurs.</summary>
    private static float Pulse(float phase, float at) => MathF.Max(0f, 1f - MathF.Abs(phase - at) / 0.09f);

    private static Vector2 At(Vector2 min, float side, float x, float y) => min + new Vector2(x, y) * (side / 28f);

    private static Vector2 Scaled(float side, float width, float height) => new Vector2(width, height) * (side / 28f);

    // ─── Barre de titre ───────────────────────────────────────────────────────

    /// <summary>
    /// Entre la marque et les boutons : la guirlande de Noël, ou le compte à
    /// rebours du 31 décembre. Purement décoratif, la zone reste celle qui
    /// déplace la fenêtre.
    /// </summary>
    public static void TitleBar(ImDrawListPtr dl, float left, float right, float top, float height)
    {
        if (Countdown() is { } countdown)
            CountdownChip(dl, countdown, left, right, top, height);
        else if (SeasonState.Decor && SeasonState.Current == Season.Christmas)
            Garland(dl, left + Theme.S(24f), right - Theme.S(24f), top);
    }

    /// <summary>« 2027 dans 00:42:17 », le 31 décembre seulement.</summary>
    private static string? Countdown()
    {
        // Sur la date seule : le 31 décembre peut encore appartenir à la Starlight.
        if (!SeasonState.EasterEggs) return null;

        var now = SeasonState.Now;
        if (now is not { Month: 12, Day: 31 }) return null;

        var left = now.Date.AddDays(1) - now;
        return string.Format(Plugin.L.NewYearCountdown, now.Year + 1, left.ToString(@"hh\:mm\:ss"));
    }

    private static void CountdownChip(ImDrawListPtr dl, string text, float left, float right, float top, float height)
    {
        var size  = ImGui.CalcTextSize(text);
        var padX  = Theme.S(10f);
        var chipH = MathF.Round(size.Y + Theme.S(8f));
        var max   = new Vector2(right - Theme.S(8f), top + (height + chipH) * 0.5f);
        var min   = new Vector2(max.X - size.X - padX * 2f, max.Y - chipH);
        if (min.X < left + Theme.S(12f)) return;

        dl.AddRectFilled(min, max, ImGui.GetColorU32(Theme.Alpha(Theme.Action, 0.14f)), chipH * 0.5f);
        dl.AddRect(min, max, ImGui.GetColorU32(Theme.Border), chipH * 0.5f, ImDrawFlags.None, 1f);
        dl.AddText(new Vector2(min.X + padX, min.Y + (chipH - size.Y) * 0.5f), ImGui.GetColorU32(Theme.TitleGold), text);
    }

    /// <summary>Deux festons de fil, cinq ampoules chacun, qui scintillent en décalé.</summary>
    private static void Garland(ImDrawListPtr dl, float left, float right, float top)
    {
        if (right - left < Theme.S(200f)) return;

        var t    = SeasonState.Time;
        var y    = top + Theme.S(8f);
        var sag  = Theme.S(44f);
        var mid  = (left + right) * 0.5f;
        var wire = ImGui.GetColorU32(Theme.Hex(0xD6ECFF, 0.35f));
        var bulb = 0;

        foreach (var (a, b) in (ReadOnlySpan<(float, float)>)[(left, mid), (mid, right)])
        {
            var p0 = new Vector2(a, y);
            var p1 = new Vector2(b, y);
            var c  = new Vector2((a + b) * 0.5f, y + sag);
            dl.AddBezierQuadratic(p0, c, p1, wire, Theme.S(1f));

            for (var k = 0; k < 5; k++, bulb++)
            {
                var s     = 0.15f + k * 0.175f;
                var point = (1f - s) * (1f - s) * p0 + 2f * s * (1f - s) * c + s * s * p1;
                var glow  = 0.35f + 0.65f * (0.5f + 0.5f * MathF.Sin(t * MathF.Tau / 2.8f + bulb * 0.7f));
                SeasonArt.Draw(dl, Bulbs[bulb % Bulbs.Length], point - Theme.S(6f, 3f), Theme.S(12f, 16.5f), glow);
            }
        }
    }

    // ─── Cartes ───────────────────────────────────────────────────────────────

    /// <summary>La neige posée sur l'arête haute des cartes, à Noël.</summary>
    public static void CardCap(ImDrawListPtr dl, Vector2 min, Vector2 max)
    {
        if (!SeasonState.Decor || SeasonState.Current != Season.Christmas) return;

        var inset = Theme.S(10f);
        if (max.X - min.X < inset * 6f) return;

        // Retournée une carte sur deux, pour que les congères ne tombent pas
        // toutes au même endroit d'une liste.
        var flip = (int)(min.Y / Theme.S(7f)) % 2 == 0;
        SeasonArt.Draw(dl, "snowcap", new Vector2(min.X + inset, min.Y - Theme.S(7f)),
                       new Vector2(max.X - min.X - inset * 2f, Theme.S(15f)), flipX: flip);
    }

    // ─── Menu ─────────────────────────────────────────────────────────────────

    public static string NavLabel(string label) => Kupo ? string.Format(Plugin.L.KupoLabel, label) : label;

    public static string Heading(string title) => Kupo ? string.Format(Plugin.L.KupoHeading, title) : title;

    /// <summary>À la Saint-Valentin, l'entrée « Amis RP » prend un cœur rose.</summary>
    public static bool HeartIcon(string pageId) =>
        SeasonState.Decor && SeasonState.Current == Season.Valentine && pageId == "friends";

    /// <summary>
    /// Le nid de la chasse aux œufs, dans l'espace libre du menu entre les
    /// entrées et celles ancrées en bas. Rien si la place manque.
    /// </summary>
    public static void Nest(ImDrawListPtr dl, Vector2 top, float width, float from, float to, bool rail)
    {
        if (rail || SeasonHunts.Active != SeasonHunts.Eggs) return;

        var size = Theme.S(72f, 40f);
        if (to - from < size.Y + Theme.S(16f)) return;

        var hunt   = SeasonHunts.Eggs;
        var found  = SeasonHunts.Count(hunt.Key);
        var origin = new Vector2(top.X + Theme.S(14f), top.Y + to - size.Y - Theme.S(8f));

        SeasonArt.Draw(dl, "nest-back", origin, size);

        ReadOnlySpan<(float X, float Y, float Degrees)> spots = [(21f, 11.5f, -12f), (30f, 8.5f, 0f), (39f, 10.5f, 8f), (48f, 12.5f, 16f)];
        for (var i = 0; i < Math.Min(found, spots.Length); i++)
            SeasonArt.Rotated(dl, SeasonHunts.ArtFor(hunt, hunt.Pages[i]), origin + Theme.S(spots[i].X, spots[i].Y),
                              Theme.S(12f, 15f), spots[i].Degrees * MathF.PI / 180f);

        SeasonArt.Draw(dl, "nest-front", origin, size);

        using var font = Fonts.PushSmall();
        var text  = string.Format(Plugin.L.HuntNest, found, hunt.Pages.Length);
        var label = ImGui.CalcTextSize(text);
        var x     = origin.X + size.X + Theme.S(8f);
        if (x + label.X <= top.X + width - Theme.S(8f))
            dl.AddText(new Vector2(x, origin.Y + (size.Y - label.Y) * 0.5f), ImGui.GetColorU32(Theme.TextMuted), text);
    }
}

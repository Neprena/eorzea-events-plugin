using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Cadre du portrait d'une fiche RP.
///
/// Le style vient du serveur, qui a déjà filtré selon l'adhésion : reçu nul, il
/// donne exactement le cadre historique de 2 px. Ce cas par défaut n'est pas un
/// chemin parallèle mais une branche du même <c>switch</c>, pour que le rendu de
/// base et les effets ne puissent pas diverger.
///
/// Les animations sont volontairement lentes (périodes de 5 à 6 secondes) et de
/// faible amplitude : c'est un décor permanent affiché pendant que le joueur
/// joue, pas un indicateur transitoire. Le risque n'est pas le processeur, c'est
/// la fatigue visuelle. Le réglage « animations réduites » de Dalamud fige
/// chaque effet sur sa valeur médiane plutôt que de le supprimer, sans quoi le
/// cadre changerait d'aspect au lieu de s'immobiliser.
/// </summary>
internal static class PortraitFrame
{
    /// <summary>Épaisseur du liseré de base, celle du rendu historique.</summary>
    private const float Thickness = 2f;

    /// <param name="accent2">
    /// Seconde couleur de la fiche, utilisée par le seul style « duo ». Absente,
    /// elle retombe sur la première : le contour bicolore d'une fiche à une seule
    /// teinte vaut alors exactement le liseré uni, sans cas particulier à écrire.
    /// </param>
    public static void Draw(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                            Vector4 accent, string? style, Vector4? accent2 = null)
    {
        switch (style)
        {
            case "glow":    DrawGlow(dl, min, max, radius, accent);    break;
            case "shimmer": DrawShimmer(dl, min, max, radius, accent); break;
            case "orbit":   DrawOrbit(dl, min, max, radius, accent);   break;
            case "gilded":  DrawGilded(dl, min, max, radius, accent);  break;
            case "corners": DrawCorners(dl, min, max, accent);         break;
            case "ripple":  DrawRipple(dl, min, max, radius, accent);  break;
            case "duo":     DrawDuo(dl, min, max, accent, accent2 ?? accent); break;

            // Style absent ou inconnu : le cadre d'origine, à l'identique. Une
            // valeur ajoutée côté serveur retombe donc ici plutôt que de faire
            // disparaître le liseré.
            default: DrawPlain(dl, min, max, radius, accent); break;
        }
    }

    private static void DrawPlain(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                                  Vector4 accent) =>
        dl.AddRect(min, max, ImGui.GetColorU32(accent), radius, ImDrawFlags.None,
                   Theme.S(Thickness));

    /// <summary>
    /// Oscillation lente entre 0 et 1. Figée au milieu quand l'utilisateur a
    /// demandé des animations réduites : l'effet reste visible, il ne bouge plus.
    /// </summary>
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
    /// Point du périmètre pour une avance <paramref name="u"/> dans [0, 1), au
    /// départ du coin haut-gauche et dans le sens horaire.
    ///
    /// Le tracé suit le rectangle NON arrondi : calculer la trajectoire sur les
    /// coins arrondis demanderait de la trigonométrie pour un écart de quelques
    /// pixels, invisible à cette épaisseur de trait. Partagé par l'orbite et le
    /// contour bicolore, qui parcourent le même chemin : deux copies finiraient
    /// par diverger.
    /// </summary>
    private static Vector2 PerimeterPoint(Vector2 min, Vector2 max, float u)
    {
        var w = max.X - min.X;
        var h = max.Y - min.Y;
        var d = (u - MathF.Floor(u)) * 2f * (w + h);

        if (d < w) return new Vector2(min.X + d, min.Y);
        d -= w;
        if (d < h) return new Vector2(max.X, min.Y + d);
        d -= h;
        if (d < w) return new Vector2(max.X - d, max.Y);
        d -= w;
        return new Vector2(min.X, max.Y - d);
    }

    // ─── Effets ───────────────────────────────────────────────────────────────

    /// <summary>Halo : le liseré de base plus deux rectangles concentriques.</summary>
    private static void DrawGlow(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                                 Vector4 accent)
    {
        DrawPlain(dl, min, max, radius, accent);

        // Période d'environ 5 secondes (2π / 5 ≈ 1,26 rad/s).
        var strength = 0.45f + Pulse(1.26f) * 0.30f;

        for (var ring = 1; ring <= 2; ring++)
        {
            var spread = Theme.S(2.5f * ring);
            var offset = new Vector2(spread, spread);
            var alpha  = strength * (ring == 1 ? 0.42f : 0.18f);

            dl.AddRect(min - offset, max + offset,
                       ImGui.GetColorU32(Theme.Alpha(accent, alpha)),
                       radius + spread, ImDrawFlags.None, Theme.S(1.5f));
        }
    }

    /// <summary>
    /// Miroitement : un seul liseré, dont la teinte va et vient entre l'accent et
    /// le blanc du texte. Coût strictement identique au cadre de base.
    /// </summary>
    private static void DrawShimmer(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                                    Vector4 accent)
    {
        // Période d'environ 6 secondes (2π / 6 ≈ 1,05 rad/s).
        var color = Theme.Mix(accent, Theme.Text, 0.10f + Pulse(1.05f) * 0.35f);

        dl.AddRect(min, max, ImGui.GetColorU32(color), radius, ImDrawFlags.None,
                   Theme.S(Thickness));
    }

    /// <summary>
    /// Orbite : une traînée lumineuse qui fait très lentement le tour du cadre.
    ///
    /// La première version dessinait un arc court, de longueur fixe et de couleur
    /// uniforme : deux bords nets qui se lisaient comme un objet se déplaçant le
    /// long du cadre, un serpent, et non comme une lueur. La traînée corrige les
    /// deux causes à la fois. Elle est longue, un tiers du périmètre, et son
    /// opacité varie en continu : elle monte sur les tout premiers segments pour
    /// que la tête ne soit pas un bord franc, puis décroît jusqu'à zéro à la fin
    /// de la queue. Aucun segment ne commence ni ne finit sur une valeur non
    /// nulle, il n'y a donc plus d'arête visible nulle part.
    ///
    /// Le tour dure sept secondes, contre six auparavant pour un arc cinq fois
    /// plus court : la tête avance donc bien plus lentement à l'œil.
    ///
    /// Le liseré de base garde sa pleine opacité, contrairement à la version
    /// précédente qui l'atténuait : la queue s'éteignant complètement, un liseré
    /// affaibli faisait disparaître le cadre là où la traînée était absente.
    ///
    /// Vingt-cinq primitives par image : le liseré de base plus vingt-quatre
    /// segments. Chaque segment porte sa propre opacité, ce qui interdit le
    /// polyline unique, qui n'admet qu'une seule couleur.
    /// </summary>
    private static void DrawOrbit(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                                  Vector4 accent)
    {
        DrawPlain(dl, min, max, radius, accent);

        if (max.X - min.X <= 0f || max.Y - min.Y <= 0f) return;

        const int   Segments = 24;
        const float Tail     = 0.34f;   // part du périmètre couverte par la traînée
        const float HeadFade = 0.10f;   // part de la traînée servant à ouvrir la tête
        const float Peak     = 0.85f;   // opacité maximale, juste derrière la tête

        // Un tour en 7 secondes : assez lent pour rester du décor.
        var head      = Phase(7f);
        var color     = Theme.Mix(accent, Theme.Text, 0.65f);
        var thickness = Theme.S(Thickness);
        var from      = PerimeterPoint(min, max, head);

        for (var i = 1; i <= Segments; i++)
        {
            // Avance dans la traînée : 0 à la tête, 1 au bout de la queue.
            var to = PerimeterPoint(min, max, head - Tail * i / Segments);

            // Opacité prise au milieu du segment, la seule valeur qui ne
            // privilégie ni son début ni sa fin.
            var s = (i - 0.5f) / Segments;

            // Montée courte puis décroissance jusqu'à zéro : les deux extrémités
            // de la traînée s'éteignent, il n'y a donc aucun bord net.
            var fade = MathF.Min(s / HeadFade, 1f) * (1f - s);

            dl.AddLine(from, to, ImGui.GetColorU32(Theme.Alpha(color, fade * Peak)), thickness);
            from = to;
        }
    }

    /// <summary>
    /// Doré : deux liserés statiques, l'extérieur dans l'accent, l'intérieur dans
    /// l'or du thème. Aucun appel à GetTime : cet habillage ne bouge pas.
    /// </summary>
    private static void DrawGilded(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                                   Vector4 accent)
    {
        DrawPlain(dl, min, max, radius, accent);

        var inset = Theme.S(3f);

        dl.AddRect(min + new Vector2(inset, inset), max - new Vector2(inset, inset),
                   ImGui.GetColorU32(Theme.Alpha(Theme.Gold, 0.75f)),
                   MathF.Max(radius - inset, 0f), ImDrawFlags.None, Theme.S(1.5f));
    }

    /// <summary>
    /// Équerres : quatre angles ouverts, sans contour fermé. Aucun appel à
    /// GetTime, cet habillage est strictement immobile.
    ///
    /// La longueur des branches suit le plus petit côté. Une valeur absolue
    /// donnerait, sur un portrait 3:4, des équerres trop longues en largeur et
    /// trop courtes en hauteur ; et sur la vignette de 128 px de la liste, deux
    /// angles voisins se rejoindraient, refermant le cadre que ce style est
    /// justement censé laisser ouvert.
    ///
    /// Huit segments par image. Un polyline par angle demanderait autant de
    /// sommets pour un appel plus indirect.
    /// </summary>
    private static void DrawCorners(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 accent)
    {
        var w = max.X - min.X;
        var h = max.Y - min.Y;
        if (w <= 0f || h <= 0f) return;

        // Un quart du petit côté : toujours strictement sous la moitié, donc
        // deux branches opposées ne peuvent jamais se toucher.
        var arm   = MathF.Min(w, h) * 0.24f;
        var color = ImGui.GetColorU32(accent);
        var t     = Theme.S(Thickness);

        var topRight   = new Vector2(max.X, min.Y);
        var bottomLeft = new Vector2(min.X, max.Y);

        dl.AddLine(min, min + new Vector2(arm, 0f), color, t);
        dl.AddLine(min, min + new Vector2(0f, arm), color, t);

        dl.AddLine(topRight, topRight - new Vector2(arm, 0f), color, t);
        dl.AddLine(topRight, topRight + new Vector2(0f, arm), color, t);

        dl.AddLine(bottomLeft, bottomLeft + new Vector2(arm, 0f), color, t);
        dl.AddLine(bottomLeft, bottomLeft - new Vector2(0f, arm), color, t);

        dl.AddLine(max, max - new Vector2(arm, 0f), color, t);
        dl.AddLine(max, max - new Vector2(0f, arm), color, t);
    }

    /// <summary>
    /// Onde : le liseré de base, plus un unique rectangle qui s'écarte du cadre
    /// en s'effaçant, sur une période de 4 secondes.
    ///
    /// Un seul front à la fois : deux fronts superposés donneraient une cible de
    /// tir plutôt qu'une respiration. L'opacité décroît avec l'écartement, sans
    /// quoi l'onde disparaîtrait d'un coup en fin de cycle, ce qui se lit comme
    /// un clignotement. Deux primitives par image, le cadre de base compris.
    /// </summary>
    private static void DrawRipple(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius,
                                   Vector4 accent)
    {
        DrawPlain(dl, min, max, radius, accent);

        var progress = Phase(4f);
        var spread   = Theme.S(1f + progress * 7f);
        var offset   = new Vector2(spread, spread);

        dl.AddRect(min - offset, max + offset,
                   ImGui.GetColorU32(Theme.Alpha(accent, (1f - progress) * 0.50f)),
                   radius + spread, ImDrawFlags.None, Theme.S(1.5f));
    }

    /// <summary>
    /// Bicolore : un contour fixe dont la teinte glisse de l'accent vers la
    /// seconde couleur le long du périmètre. Aucun appel à GetTime.
    ///
    /// ImGui ne sait pas dégrader un AddRect : le contour est donc découpé en
    /// segments, chacun tiré dans la teinte de son milieu. Seize suffisent, un
    /// par côté court du portrait, pour que la transition passe pour continue.
    ///
    /// Le mélange suit un aller-retour et non une rampe de 0 à 1 : sur un tracé
    /// fermé, une rampe ferait se toucher les deux extrémités du dégradé au coin
    /// haut-gauche, avec une cassure nette à cet endroit précis.
    /// </summary>
    private static void DrawDuo(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                Vector4 accent, Vector4 accent2)
    {
        if (max.X - min.X <= 0f || max.Y - min.Y <= 0f) return;

        const int Segments = 16;

        var thickness = Theme.S(Thickness);
        var from      = PerimeterPoint(min, max, 0f);

        for (var i = 1; i <= Segments; i++)
        {
            var u  = (float)i / Segments;
            var to = PerimeterPoint(min, max, u);

            // Milieu du segment, replié en aller-retour sur [0, 1].
            var middle = u - 0.5f / Segments;
            var blend  = middle < 0.5f ? middle * 2f : (1f - middle) * 2f;

            dl.AddLine(from, to, ImGui.GetColorU32(Theme.Mix(accent, accent2, blend)), thickness);
            from = to;
        }
    }
}

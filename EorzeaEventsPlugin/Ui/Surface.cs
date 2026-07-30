using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Rendu des surfaces surélevées : ombre portée, fond, bordure et liseré.
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
    /// Barre d'accent verticale collée au bord gauche d'une surface, arrondie
    /// du même rayon pour épouser le coin.
    /// </summary>
    public static void AccentBar(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                 Vector4 color, float width = 3f, float? rounding = null)
    {
        var r = rounding ?? Theme.S(Theme.RadiusCard);
        dl.AddRectFilled(
            min,
            new Vector2(min.X + Theme.S(width), max.Y),
            ImGui.GetColorU32(color),
            r,
            ImDrawFlags.RoundCornersLeft);
    }
}

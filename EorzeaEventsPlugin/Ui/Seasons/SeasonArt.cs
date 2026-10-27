using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using System.Numerics;
using System.Reflection;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Les images des thèmes, embarquées dans l'assemblage comme le logo
/// (<see cref="Brand"/>). Elles sont rastérisées depuis
/// <c>Assets/Images/Seasons/src</c> par <c>scripts/render-season-assets.ts</c>.
///
/// Tant qu'une texture n'est pas prête, rien n'est dessiné : le décor apparaît
/// une image plus tard, sans rien décaler.
/// </summary>
internal static class SeasonArt
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    private static IDalamudTextureWrap? Get(string name) =>
        Plugin.TextureProvider.GetFromManifestResource(Assembly, $"Images.Seasons.{name}.png").GetWrapOrDefault();

    private static uint Tint(float alpha) => ImGui.GetColorU32(new Vector4(1f, 1f, 1f, Math.Clamp(alpha, 0f, 1f)));

    /// <summary>Dessine l'image dans le rectangle donné, retournée au besoin.</summary>
    public static void Draw(ImDrawListPtr dl, string name, Vector2 min, Vector2 size, float alpha = 1f,
                            bool flipX = false, bool flipY = false)
    {
        if (alpha <= 0.01f || Get(name) is not { } wrap) return;

        var uv0 = new Vector2(flipX ? 1f : 0f, flipY ? 1f : 0f);
        var uv1 = new Vector2(flipX ? 0f : 1f, flipY ? 0f : 1f);
        dl.AddImage(wrap.Handle, min, min + size, uv0, uv1, Tint(alpha));
    }

    /// <summary>Dessine l'image centrée sur un point.</summary>
    public static void Centered(ImDrawListPtr dl, string name, Vector2 center, Vector2 size, float alpha = 1f) =>
        Draw(dl, name, center - size * 0.5f, size, alpha);

    /// <summary>Dessine l'image centrée sur un point et tournée d'un angle en radians.</summary>
    public static void Rotated(ImDrawListPtr dl, string name, Vector2 center, Vector2 size, float angle,
                               float alpha = 1f)
    {
        if (alpha <= 0.01f || Get(name) is not { } wrap) return;

        var (sin, cos) = MathF.SinCos(angle);
        var x = new Vector2(cos, sin) * size.X * 0.5f;
        var y = new Vector2(-sin, cos) * size.Y * 0.5f;

        dl.AddImageQuad(wrap.Handle,
                        center - x - y, center + x - y, center + x + y, center - x + y,
                        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                        Tint(alpha));
    }
}

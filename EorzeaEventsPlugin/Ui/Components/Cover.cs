using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Couverture en tête d'une fiche : événement, lieu, actualité.
///
/// Chaque fenêtre avait la sienne, de 150 à 180 px, et l'une étirait l'image
/// au lieu de la rogner. L'image occupe toute la largeur sur une hauteur
/// plafonnée, rognée en son centre dans le sens qui dépasse : entière, une
/// affiche prendrait la moitié de la fenêtre avant le moindre mot.
/// </summary>
internal static class Cover
{
    public const float MaxHeight = 170f;

    /// <summary>Ne dessine rien tant que l'image n'est pas arrivée.</summary>
    public static void Draw(IDalamudTextureWrap? wrap)
    {
        if (wrap is null || wrap.Width == 0 || wrap.Height == 0) return;

        var width  = ImGui.GetContentRegionAvail().X;
        var height = Math.Min(Theme.S(MaxHeight), width * wrap.Height / wrap.Width);
        if (width <= 0f || height <= 0f) return;

        // Part de l'image gardée sur chaque axe : 1 sur celui qui tient, moins
        // sur celui qu'il faut rogner pour ne pas déformer.
        var imageAspect = wrap.Width / (float)wrap.Height;
        var frameAspect = width / height;
        var keepU = imageAspect > frameAspect ? frameAspect / imageAspect : 1f;
        var keepV = imageAspect > frameAspect ? 1f : imageAspect / frameAspect;

        ImGui.Image(wrap.Handle, new Vector2(width, height),
                    new Vector2((1f - keepU) / 2f, (1f - keepV) / 2f),
                    new Vector2((1f + keepU) / 2f, (1f + keepV) / 2f));
        Layout.Spacer(Theme.GapS);
    }
}

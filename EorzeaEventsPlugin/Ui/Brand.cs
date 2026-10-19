using Dalamud.Bindings.ImGui;
using System.Numerics;
using System.Reflection;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Le logo du plugin, embarqué dans l'assemblage.
///
/// Statique comme <see cref="Fonts"/> : la barre de titre est une aide
/// statique, et lui passer la texture de main en main traverserait toutes les
/// fenêtres. La texture partagée de Dalamud se charge seule au premier appel ;
/// tant qu'elle n'est pas prête, rien n'est dessiné et <see cref="Draw"/> rend
/// faux, l'appelant gardant alors sa place.
/// </summary>
internal static class Brand
{
    private const string Resource = "Images.logo.png";

    /// <summary>Dessine le logo dans le carré donné. Rend faux s'il n'est pas encore chargé.</summary>
    public static bool Draw(ImDrawListPtr dl, Vector2 min, float side)
    {
        var shared = Plugin.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), Resource);
        if (shared.GetWrapOrDefault() is not { } wrap) return false;

        dl.AddImage(wrap.Handle, min, min + new Vector2(side, side));
        return true;
    }
}

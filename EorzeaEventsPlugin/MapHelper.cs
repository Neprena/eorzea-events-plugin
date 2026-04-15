using Dalamud.Utility;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace EorzeaEventsPlugin;

internal static class MapHelper
{
    /// <summary>Convertit une coordonnée monde FFXIV en coordonnée carte (fourchette 1–42).</summary>
    public static float WorldToMapCoord(float worldCoord, uint sizeFactor, int offset)
        => MapUtil.ConvertWorldCoordXZToMapCoord(worldCoord, sizeFactor, offset);

    /// <summary>
    /// Retourne les coordonnées carte (X, Y) à partir de coordonnées monde,
    /// en résolvant la Map via le DataManager. Retourne null si la map est inconnue.
    /// </summary>
    public static (float x, float y)? WorldToMapCoords(float worldX, float worldZ, uint mapId)
    {
        var mapRow = Plugin.DataManager.GetExcelSheet<Map>()?.GetRowOrDefault(mapId);
        if (mapRow == null) return null;
        var coords = MapUtil.WorldToMap(new Vector2(worldX, worldZ), mapRow.Value);
        return (coords.X, coords.Y);
    }

    /// <summary>
    /// Raccourci : convertit des coordonnées monde en coordonnées carte (affichage)
    /// en utilisant Plugin.ClientState.MapId — la sous-map active du joueur (ex : quartier de logement)
    /// qui possède le bon sizeFactor/offset pour les coordonnées affichées en jeu.
    /// </summary>
    public static (float x, float y)? WorldToCurrentMapCoords(float worldX, float worldZ)
    {
        var mapId = Plugin.ClientState.MapId;
        if (mapId == 0) return null;
        return WorldToMapCoords(worldX, worldZ, mapId);
    }

    /// <summary>
    /// Retourne les coordonnées carte actuellement affichées pour le joueur local
    /// en s'appuyant directement sur l'helper officiel de Dalamud.
    /// </summary>
    public static (float x, float y)? GetLocalPlayerMapCoords()
    {
        var player = Plugin.ObjectTable.LocalPlayer;
        if (player == null) return null;

        var coords = MapUtil.GetMapCoordinates(player, correctZOffset: false);
        return (coords.X, coords.Y);
    }
}

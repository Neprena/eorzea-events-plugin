using Lumina.Excel.Sheets;

namespace EorzeaEventsPlugin;

internal static class MapHelper
{
    /// <summary>Convertit une coordonnée monde FFXIV en coordonnée carte (fourchette 1–42).</summary>
    public static float WorldToMapCoord(float worldCoord, uint sizeFactor, short offset)
    {
        var scale = sizeFactor / 100f;
        return 41f / scale * ((worldCoord + offset) / 2048f) + 1f;
    }

    /// <summary>
    /// Retourne les coordonnées carte (X, Y) à partir de coordonnées monde,
    /// en résolvant la Map via le DataManager. Retourne null si la map est inconnue.
    /// </summary>
    public static (float x, float y)? WorldToMapCoords(float worldX, float worldZ, uint mapId)
    {
        var mapRow = Plugin.DataManager.GetExcelSheet<Map>()?.GetRowOrDefault(mapId);
        if (mapRow == null) return null;
        return (
            WorldToMapCoord(worldX, mapRow.Value.SizeFactor, mapRow.Value.OffsetX),
            WorldToMapCoord(worldZ, mapRow.Value.SizeFactor, mapRow.Value.OffsetY)
        );
    }
}

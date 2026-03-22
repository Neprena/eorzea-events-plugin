using Lumina.Excel.Sheets;

namespace EorzeaEventsPlugin;

internal static class MapHelper
{
    /// <summary>Convertit une coordonnée monde FFXIV en coordonnée carte (fourchette 1–42).</summary>
    public static float WorldToMapCoord(float worldCoord, uint sizeFactor, short offset)
    {
        var scale = sizeFactor / 100f;
        return 41f / scale * ((worldCoord + 1024f + offset) / 2048f) + 1f;
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

    /// <summary>
    /// Raccourci : convertit des coordonnées monde en coordonnées carte (affichage)
    /// en résolvant automatiquement la map depuis le territoire actif du joueur.
    /// Applique deux passes pour correspondre au comportement des sous-maps de logement.
    /// </summary>
    public static (float x, float y)? WorldToCurrentMapCoords(float worldX, float worldZ)
    {
        var mapId = Plugin.DataManager.GetExcelSheet<TerritoryType>()
                          ?.GetRowOrDefault(Plugin.ClientState.TerritoryType)
                          ?.Map.RowId;
        if (!mapId.HasValue) return null;
        var pass1 = WorldToMapCoords(worldX, worldZ, mapId.Value);
        if (!pass1.HasValue) return null;
        return WorldToMapCoords(pass1.Value.x, pass1.Value.y, mapId.Value);
    }
}

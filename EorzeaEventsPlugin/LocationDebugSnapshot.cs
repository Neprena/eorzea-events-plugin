using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace EorzeaEventsPlugin;

internal sealed class LocationDebugSnapshot
{
    public string CharacterName { get; init; } = "—";
    public string WorldName { get; init; } = "—";

    public ushort TerritoryId { get; init; }
    public uint MapId { get; init; }
    public string TerritoryName { get; init; } = "—";

    public uint? MapRowId { get; init; }
    public uint? PlaceNameRowId { get; init; }
    public string PlaceName { get; init; } = "—";
    public uint? SizeFactor { get; init; }
    public short? OffsetX { get; init; }
    public short? OffsetY { get; init; }

    public Vector3? WorldPosition { get; init; }
    public Vector3? DisplayMapPosition { get; init; }
    public (float x, float y)? FallbackMapPosition { get; init; }

    public bool HasHousingManager { get; init; }
    public int? RawWard { get; init; }
    public int? RawPlot { get; init; }
    public int? RawRoom { get; init; }
    public int? Ward { get; init; }
    public int? Plot { get; init; }
    public int? Room { get; init; }
    public uint? OriginalHouseTerritoryTypeId { get; init; }
    public bool HasHousingContext => HasHousingManager && RawWard.HasValue && RawWard.Value >= 0;
    public bool HasPlot => Plot.HasValue;
    public bool HasRoom => Room.HasValue;
    public string HousingGuess { get; init; } = "unknown";

    public static unsafe LocationDebugSnapshot Collect()
    {
        var player = Plugin.ObjectTable.LocalPlayer;
        var territoryId = Plugin.ClientState.TerritoryType;
        var mapId = Plugin.ClientState.MapId;

        var territoryRow = Plugin.DataManager.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryId);
        var mapRow = mapId == 0 ? null : Plugin.DataManager.GetExcelSheet<Map>()?.GetRowOrDefault(mapId);

        Vector3? worldPos = player?.Position;
        Vector3? displayMapPos = null;
        (float x, float y)? fallbackMapPos = null;
        if (player != null)
        {
            displayMapPos = MapUtil.GetMapCoordinates(player, correctZOffset: false);
            fallbackMapPos = MapHelper.WorldToCurrentMapCoords(player.Position.X, player.Position.Z);
        }

        var hm = HousingManager.Instance();
        int? rawWard = null;
        int? rawPlot = null;
        int? rawRoom = null;
        uint? originalHouseTerritoryTypeId = null;
        if (hm != null)
        {
            rawWard = hm->GetCurrentWard();
            rawPlot = hm->GetCurrentPlot();
            rawRoom = hm->GetCurrentRoom();
            originalHouseTerritoryTypeId = HousingManager.GetOriginalHouseTerritoryTypeId();
        }

        return new LocationDebugSnapshot
        {
            CharacterName = player?.Name.ToString() ?? "—",
            WorldName = player?.CurrentWorld.Value.Name.ToString() ?? "—",
            TerritoryId = territoryId,
            MapId = mapId,
            TerritoryName = territoryRow?.PlaceName.Value.Name.ToString() ?? "—",
            MapRowId = mapRow?.RowId,
            PlaceNameRowId = mapRow?.PlaceName.RowId,
            PlaceName = mapRow?.PlaceName.Value.Name.ToString() ?? "—",
            SizeFactor = mapRow?.SizeFactor,
            OffsetX = mapRow?.OffsetX,
            OffsetY = mapRow?.OffsetY,
            WorldPosition = worldPos,
            DisplayMapPosition = displayMapPos,
            FallbackMapPosition = fallbackMapPos,
            HasHousingManager = hm != null,
            RawWard = rawWard,
            RawPlot = rawPlot,
            RawRoom = rawRoom,
            Ward = rawWard is >= 0 ? rawWard + 1 : null,
            Plot = rawPlot is >= 0 ? rawPlot + 1 : null,
            Room = rawRoom is > 0 ? rawRoom : null,
            OriginalHouseTerritoryTypeId = originalHouseTerritoryTypeId,
            HousingGuess = InferHousingGuess(rawPlot, rawRoom),
        };
    }

    private static string InferHousingGuess(int? rawPlot, int? rawRoom)
    {
        if (rawRoom is > 0) return "room_or_apartment";
        if (rawPlot is >= 0) return "house_plot";
        return "unknown";
    }

    public string ToDebugDump()
    {
        var sb = new StringBuilder();
        AppendLine(sb, "characterName", CharacterName);
        AppendLine(sb, "worldName", WorldName);
        AppendLine(sb, "territoryId", TerritoryId);
        AppendLine(sb, "territoryName", TerritoryName);
        AppendLine(sb, "mapId", MapId);
        AppendLine(sb, "mapRowId", MapRowId);
        AppendLine(sb, "mapPlaceNameRowId", PlaceNameRowId);
        AppendLine(sb, "mapPlaceName", PlaceName);
        AppendLine(sb, "mapSizeFactor", SizeFactor);
        AppendLine(sb, "mapOffsetX", OffsetX);
        AppendLine(sb, "mapOffsetY", OffsetY);
        AppendLine(sb, "worldPos", FormatVector3(WorldPosition));
        AppendLine(sb, "displayMapPos", FormatVector3(DisplayMapPosition));
        AppendLine(sb, "fallbackMapPos", FormatMap2(FallbackMapPosition));
        AppendLine(sb, "hasHousingManager", HasHousingManager);
        AppendLine(sb, "rawWard", RawWard);
        AppendLine(sb, "rawPlot", RawPlot);
        AppendLine(sb, "rawRoom", RawRoom);
        AppendLine(sb, "ward", Ward);
        AppendLine(sb, "plot", Plot);
        AppendLine(sb, "room", Room);
        AppendLine(sb, "originalHouseTerritoryTypeId", OriginalHouseTerritoryTypeId);
        AppendLine(sb, "hasHousingContext", HasHousingContext);
        AppendLine(sb, "hasPlot", HasPlot);
        AppendLine(sb, "hasRoom", HasRoom);
        AppendLine(sb, "housingGuess", HousingGuess);
        return sb.ToString();
    }

    private static void AppendLine(StringBuilder sb, string key, object? value)
        => sb.Append(key).Append(": ").AppendLine(value?.ToString() ?? "—");

    private static string FormatVector3(Vector3? value)
        => value.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "X={0:F2}, Y={1:F2}, Z={2:F2}", value.Value.X, value.Value.Y, value.Value.Z)
            : "—";

    private static string FormatMap2((float x, float y)? value)
        => value.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "X={0:F2}, Y={1:F2}", value.Value.x, value.Value.y)
            : "—";
}

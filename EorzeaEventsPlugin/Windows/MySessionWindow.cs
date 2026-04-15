using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace EorzeaEventsPlugin.Windows;

public class MySessionWindow : Window
{
    private readonly Configuration _config;

    // Formulaire création
    private string _title         = string.Empty;
    private string _description   = string.Empty;
    private string _characterName = string.Empty;
    private int    _duration      = 2;

    // État session en cours
    private RpSessionDto? _activeSession;
    private bool          _busy          = false;
    private string        _statusMsg     = string.Empty;
    private bool          _statusIsError = false;

    // Mode édition
    private bool   _editing   = false;
    private string _editTitle = string.Empty;
    private string _editDesc  = string.Empty;

    // Alertes contextuelles
    private bool _pendingZonePrompt         = false;
    private bool _pendingRpTagPrompt        = false;
    private bool _pendingRpTagActivePrompt  = false;

    // Polling
    private DateTime _lastSessionCheck = DateTime.MinValue;
    private const int PollIntervalSeconds = 5;

    public bool HasActiveSession => _activeSession != null;

    public MySessionWindow(Configuration config)
        : base("My RP Session##mysession")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 460),
            MaximumSize = new Vector2(640, 700),
        };
        _config = config;
    }

    // ─── Helpers jeu ─────────────────────────────────────────────────────────

    private string GetCurrentWorld()
        => Plugin.ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString() ?? Plugin.L.WorldUnknown;

    private string GetCurrentZone()
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet == null) return Plugin.L.ZoneUnknown;
        var row = sheet.GetRowOrDefault(Plugin.ClientState.TerritoryType);
        return row?.PlaceName.Value.Name.ToString() ?? Plugin.L.ZoneUnknown;
    }

    private (uint territoryId, uint mapId) GetCurrentTerritoryMap()
    {
        var territoryId = (uint)Plugin.ClientState.TerritoryType;
        var mapId       = Plugin.ClientState.MapId;
        return (territoryId, mapId);
    }

    private string GetCharacterName()
        => Plugin.ObjectTable.LocalPlayer?.Name.ToString() ?? string.Empty;

    private (float x, float z)? GetCurrentPosition()
    {
        var pos = Plugin.ObjectTable.LocalPlayer?.Position;
        return pos.HasValue ? (pos.Value.X, pos.Value.Z) : null;
    }

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    private unsafe record HousingInfo(int Ward, int? Plot, int? Room, int? RawPlot);

    private static unsafe HousingInfo? GetCurrentHousing()
    {
        var hm = HousingManager.Instance();
        if (hm == null) return null;
        var ward = hm->GetCurrentWard();
        if (ward < 0) return null;
        var plot = hm->GetCurrentPlot();
        var room = hm->GetCurrentRoom();
        return new HousingInfo(
            Ward: ward + 1,
            Plot: plot >= 0 ? plot + 1 : null,
            Room: room > 0  ? room     : null,
            RawPlot: plot
        );
    }

    // ─── API actions ──────────────────────────────────────────────────────────

    public void SetActiveSession(RpSessionDto? session)
    {
        _activeSession           = session;
        _pendingZonePrompt       = false;
        _pendingRpTagPrompt      = false;
        _pendingRpTagActivePrompt = false;
    }

    public void OnZoneChanged()      => _pendingZonePrompt        = true;
    public void OnRpTagRemoved()     => _pendingRpTagPrompt       = true;
    public void OnRpTagActivated()   => _pendingRpTagActivePrompt = true;

    private void StartSession()
    {
        var l = Plugin.L;
        if (string.IsNullOrWhiteSpace(Plugin.Config.ApiToken)) { ShowError(l.ErrTokenMissing); return; }
        var pos      = GetCurrentPosition();
        var housing  = GetCurrentHousing();
        var (terId, mapId) = GetCurrentTerritoryMap();
        var mapCoords = MapHelper.GetLocalPlayerMapCoords()
                     ?? (pos.HasValue ? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z) : null);
        Plugin.Log.Debug($"[StartSession] world=({pos?.x:F2},{pos?.z:F2}) mapId={mapId} → map=({mapCoords?.x:F2},{mapCoords?.y:F2})");
        var req = new CreateSessionRequest
        {
            Title         = _title.Trim(),
            Description   = string.IsNullOrWhiteSpace(_description) ? null : _description.Trim(),
            Location      = GetCurrentZone(),
            Server        = GetCurrentWorld(),
            CharacterName = string.IsNullOrWhiteSpace(_characterName) ? null : _characterName.Trim(),
            PosX          = mapCoords?.Item1,
            PosZ          = mapCoords?.Item2,
            Ward          = housing?.Ward,
            Plot          = housing?.Plot,
            Room          = housing?.Room,
            RawPlot       = housing?.RawPlot,
            Duration      = _duration,
            TerritoryId   = terId,
            MapId         = mapId,
        };
        if (string.IsNullOrWhiteSpace(req.Title)) { ShowError(l.ErrTitleRequired); return; }

        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var session = await Plugin.Api.CreateSessionAsync(req);
                _activeSession            = session;
                _pendingRpTagActivePrompt = false;
                _config.ActiveSessionId   = session!.Id;
                _config.Save();
                ShowSuccess(l.StatusStarted);
                _title = _description = string.Empty;
            }
            catch (Exception ex)
            {
                // 409 : une session est déjà active en base — on tente de la récupérer
                if (ex.Message.Contains("déjà en cours", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var ids      = await Plugin.Api.GetMySessionIdsAsync();
                        var firstId  = ids.Count > 0 ? System.Linq.Enumerable.First(ids) : null;
                        var existing = firstId != null ? await Plugin.Api.GetSessionAsync(firstId) : null;
                        if (existing != null)
                        {
                            _activeSession            = existing;
                            _pendingRpTagActivePrompt = false;
                            _config.ActiveSessionId   = existing.Id;
                            _config.Save();
                            ShowSuccess(l.StatusRecovered);
                            _title = _description = string.Empty;
                            return;
                        }
                    }
                    catch { /* si la récupération échoue, on affiche l'erreur originale */ }
                }
                ShowError(ex.Message);
            }
            finally { _busy = false; }
        });
    }

    private void UpdateSession()
    {
        var l = Plugin.L;
        if (_activeSession == null) return;
        var id  = _activeSession.Id;
        var req = new UpdateSessionRequest
        {
            Title       = string.IsNullOrWhiteSpace(_editTitle) ? null : _editTitle.Trim(),
            Description = _editDesc.Trim().Length > 0 ? _editDesc.Trim() : null,
        };
        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var updated = await Plugin.Api.UpdateSessionAsync(id, req);
                if (updated != null) { _activeSession = updated; _editing = false; ShowSuccess(l.StatusUpdated); }
                else ShowError(l.ErrUpdate);
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void RefreshPosition()
    {
        var l = Plugin.L;
        if (_activeSession == null) return;
        var pos      = GetCurrentPosition();
        var housing  = GetCurrentHousing();
        var zone     = GetCurrentZone();
        var world    = GetCurrentWorld();
        var charName = GetCharacterName();
        var (terId, mapId) = GetCurrentTerritoryMap();
        var mapCoords = MapHelper.GetLocalPlayerMapCoords()
                     ?? (pos.HasValue ? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z) : null);
        Plugin.Log.Debug($"[RefreshPosition] world=({pos?.x:F2},{pos?.z:F2}) mapId={mapId} → map=({mapCoords?.x:F2},{mapCoords?.y:F2})");
        var id  = _activeSession.Id;
        var req = new UpdateSessionRequest
        {
            PosX          = mapCoords?.Item1,
            PosZ          = mapCoords?.Item2,
            Ward          = housing?.Ward,
            Plot          = housing?.Plot,
            Room          = housing?.Room,
            RawPlot       = housing?.RawPlot,
            Location      = zone,
            Server        = world,
            CharacterName = string.IsNullOrEmpty(charName) ? null : charName,
            TerritoryId   = terId,
            MapId         = mapId,
        };
        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var updated = await Plugin.Api.UpdateSessionAsync(id, req);
                if (updated != null)
                {
                    _activeSession = updated;
                    var posMsg = (updated.PosX.HasValue && updated.PosZ.HasValue)
                        ? $" (X {updated.PosX.Value:F1}  Y {updated.PosZ.Value:F1})"
                        : string.Empty;
                    ShowSuccess(l.StatusPosUpdated + posMsg);
                }
                else ShowError(l.ErrUpdate);
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void ExtendSession(int hours = 1)
    {
        var l = Plugin.L;
        if (_activeSession == null) return;
        var id  = _activeSession.Id;
        var req = new UpdateSessionRequest { Duration = hours };
        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var updated = await Plugin.Api.UpdateSessionAsync(id, req);
                if (updated != null) { _activeSession = updated; ShowSuccess(string.Format(l.StatusExtended, hours)); }
                else ShowError(l.ErrExtend);
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void EndSession()
    {
        var l = Plugin.L;
        if (_activeSession == null) return;
        _busy = true; _statusMsg = string.Empty;
        var id = _activeSession.Id;
        Task.Run(async () =>
        {
            try
            {
                await Plugin.Api.EndSessionAsync(id);
                _activeSession            = null;
                _pendingZonePrompt        = false;
                _pendingRpTagPrompt       = false;
                _pendingRpTagActivePrompt = false;
                _config.ActiveSessionId = null;
                _config.Save();
                ShowSuccess(l.StatusEnded);
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    public void PollSessionStatus()
    {
        if (_busy || _activeSession == null) return;
        if ((DateTime.UtcNow - _lastSessionCheck).TotalSeconds < PollIntervalSeconds) return;
        _lastSessionCheck = DateTime.UtcNow;
        var id = _activeSession.Id;
        Task.Run(async () =>
        {
            try
            {
                var session = await Plugin.Api.GetSessionAsync(id);
                if (session == null || session.EndedAt != null)
                {
                    _activeSession = null;
                    _config.ActiveSessionId = null;
                    _config.Save();
                }
            }
            catch { /* silencieux */ }
        });
    }

    private void ShowError(string msg)   { _statusMsg = msg; _statusIsError = true;  }
    private void ShowSuccess(string msg) { _statusMsg = msg; _statusIsError = false; }

    // ─── Draw ─────────────────────────────────────────────────────────────────

    public override void Draw()
    {
        var l = Plugin.L;
        WindowName = l.MySessionTitle + "##mysession";

        if (string.IsNullOrWhiteSpace(_config.ApiToken) || !Plugin.Api.IsTokenValid)
        {
            var tokenMissing = string.IsNullOrWhiteSpace(_config.ApiToken);
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 0.6f, 0, 1),
                tokenMissing ? l.ErrTokenMissing : "⚠  " + l.TokenInvalidLine1);
            ImGui.Spacing();
            ImGui.TextWrapped(tokenMissing ? l.MySessionTokenMissingDesc : l.MySessionTokenInvalidDesc);
            ImGui.Spacing();
            if (ImGui.Button(tokenMissing ? l.BtnConfigureNow : l.TokenReconfigure, UiSizes.PrimaryButton))
                Plugin.OpenSetup(tokenInvalid: !tokenMissing);
            return;
        }

        if (_activeSession != null)
            DrawActiveSession();
        else
            DrawCreateForm();

        if (!string.IsNullOrWhiteSpace(_statusMsg))
        {
            ImGui.Spacing();
            var color = _statusIsError
                ? new Vector4(1, 0.35f, 0.35f, 1)
                : new Vector4(0.3f, 0.9f, 0.5f, 1);
            ImGui.TextColored(color, _statusMsg);
        }
    }

    // ─── Formulaire création ──────────────────────────────────────────────────

    private void DrawCreateForm()
    {
        var l = Plugin.L;
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), l.SessionCreate);
        ImGui.Separator();
        ImGui.Spacing();

        // ── Suggestion : tag RP activé ──────────────────────────────────────────
        if (_pendingRpTagActivePrompt)
        {
            var dl    = ImGui.GetWindowDrawList();
            var avail = ImGui.GetContentRegionAvail().X;
            var p0    = ImGui.GetCursorScreenPos();
            dl.ChannelsSplit(2);
            dl.ChannelsSetCurrent(1);
            ImGui.Spacing();
            ImGui.Indent(8f);
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f), l.AlertRpTagActivTitle);
            ImGui.TextWrapped(l.AlertRpTagActivDesc);
            ImGui.Spacing();
            if (ImGui.Button(l.Ignore + "##rptag_active", UiSizes.SmallButton)) { _pendingRpTagActivePrompt = false; IsOpen = false; }
            ImGui.Unindent(8f);
            ImGui.Spacing();
            var p1 = ImGui.GetCursorScreenPos();
            dl.ChannelsSetCurrent(0);
            dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(0.3f, 0.9f, 0.5f, 0.10f)), 4f);
            dl.AddRect(      p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(0.3f, 0.9f, 0.5f, 0.45f)), 4f);
            dl.ChannelsMerge();
            ImGui.Spacing();
        }

        var world   = GetCurrentWorld();
        var zone    = GetCurrentZone();
        var pos     = GetCurrentPosition();
        var housing = GetCurrentHousing();
        ImGui.TextDisabled($"{l.FieldServer}: {world}   •   {l.FieldLocation}: {zone}");
        if (housing != null)
        {
            var loc = housing.Plot.HasValue
                ? string.Format(l.HousingWardPlot, housing.Ward, housing.Plot)
                : housing.Room.HasValue
                    ? string.Format(l.HousingWardRoom, housing.Ward, housing.Room)
                    : string.Format(l.HousingWard, housing.Ward);
            ImGui.TextDisabled($"{l.FieldHousing}: {loc}");
        }
        if (pos.HasValue)
        {
            var c = MapHelper.GetLocalPlayerMapCoords()
                 ?? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z);
            ImGui.TextDisabled(c.HasValue
                ? $"{l.FieldPosition}: X {c.Value.x:F1}   Y {c.Value.y:F1}"
                : $"{l.FieldPosition}: X {pos.Value.x:F1}   Y {pos.Value.z:F1}");
        }
        ImGui.Spacing();

        ImGui.Text(l.FieldTitle + " *");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##title", ref _title, 100);

        ImGui.Spacing();
        if (string.IsNullOrEmpty(_characterName))
            _characterName = GetCharacterName();

        ImGui.Text(l.FieldCharName);
        ImGui.SetNextItemWidth(-80);
        ImGui.InputText("##charname", ref _characterName, 60);
        ImGui.SameLine();
        if (ImGui.Button(l.Auto, UiSizes.SmallButton))
            _characterName = GetCharacterName();

        ImGui.Spacing();
        ImGui.Text(l.FieldDesc + " (opt.)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("##desc", ref _description, 500, new Vector2(-1, 60));

        ImGui.Spacing();
        ImGui.Text(l.FieldDuration);
        ImGui.SetNextItemWidth(120);
        ImGui.SliderInt("##duration", ref _duration, 1, 8);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var canStart = !_busy && !string.IsNullOrWhiteSpace(_title);
        if (!canStart) ImGui.BeginDisabled();
        if (ImGui.Button(_busy ? l.StatusCreating : l.RpNewSession, new Vector2(-1, 0)))
            StartSession();
        if (!canStart) ImGui.EndDisabled();
    }

    // ─── Session active ───────────────────────────────────────────────────────

    private void DrawActiveSession()
    {
        var l = Plugin.L;
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), l.SessionActive);
        ImGui.Separator();
        ImGui.Spacing();

        // ── Alerte : changement de zone/TP ──────────────────────────────────
        if (_pendingZonePrompt)
        {
            var dl    = ImGui.GetWindowDrawList();
            var avail = ImGui.GetContentRegionAvail().X;
            var p0    = ImGui.GetCursorScreenPos();
            dl.ChannelsSplit(2);
            dl.ChannelsSetCurrent(1);
            ImGui.Spacing();
            ImGui.Indent(8f);
            ImGui.TextColored(new Vector4(1f, 0.75f, 0.1f, 1f), l.AlertZoneChangedTitle);
            ImGui.TextWrapped(l.AlertZoneChangedDesc);
            ImGui.Spacing();
            if (ImGui.Button(l.BtnUpdatePos + "##zone", UiSizes.WideButton)) { _pendingZonePrompt = false; RefreshPosition(); }
            ImGui.SameLine();
            if (ImGui.Button(l.Ignore + "##zone", UiSizes.SmallButton))       { _pendingZonePrompt = false; IsOpen = false; }
            ImGui.Unindent(8f);
            ImGui.Spacing();
            var p1 = ImGui.GetCursorScreenPos();
            dl.ChannelsSetCurrent(0);
            dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(1f, 0.75f, 0.1f, 0.10f)), 4f);
            dl.AddRect(      p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(1f, 0.75f, 0.1f, 0.45f)), 4f);
            dl.ChannelsMerge();
            ImGui.Spacing();
        }

        // ── Alerte : tag RP retiré ───────────────────────────────────────────
        if (_pendingRpTagPrompt)
        {
            var dl    = ImGui.GetWindowDrawList();
            var avail = ImGui.GetContentRegionAvail().X;
            var p0    = ImGui.GetCursorScreenPos();
            dl.ChannelsSplit(2);
            dl.ChannelsSetCurrent(1);
            ImGui.Spacing();
            ImGui.Indent(8f);
            ImGui.TextColored(new Vector4(0.75f, 0.5f, 1f, 1f), l.AlertRpTagRemovedTitle);
            ImGui.TextWrapped(l.AlertRpTagRemovedDesc);
            ImGui.Spacing();
            if (ImGui.Button(l.BtnEnd + "##rptag", UiSizes.MediumButton))    { _pendingRpTagPrompt = false; EndSession(); }
            ImGui.SameLine();
            if (ImGui.Button(l.Ignore + "##rptag", UiSizes.SmallButton))      _pendingRpTagPrompt = false;
            ImGui.Unindent(8f);
            ImGui.Spacing();
            var p1 = ImGui.GetCursorScreenPos();
            dl.ChannelsSetCurrent(0);
            dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(0.75f, 0.5f, 1f, 0.10f)), 4f);
            dl.AddRect(      p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(0.75f, 0.5f, 1f, 0.45f)), 4f);
            dl.ChannelsMerge();
            ImGui.Spacing();
        }

        if (!_editing)
        {
            ImGui.Text($"{l.FieldTitle}: {_activeSession!.Title}");
            ImGui.Text($"{l.FieldLocation}: {_activeSession.Location} ({_activeSession.Server})");
            if (!string.IsNullOrEmpty(_activeSession.CharacterName))
                ImGui.Text($"{l.FieldCharName}: {_activeSession.CharacterName}");
            if (_activeSession.Ward.HasValue)
            {
                var housing = _activeSession.Room.HasValue
                    ? string.Format(l.HousingWardRoom, _activeSession.Ward, _activeSession.Room)
                    : _activeSession.Plot.HasValue
                        ? string.Format(l.HousingWardPlot, _activeSession.Ward, _activeSession.Plot)
                        : string.Format(l.HousingWard, _activeSession.Ward);
                ImGui.TextDisabled($"{l.FieldHousing}: {housing}");
            }
            var livePos = GetCurrentPosition();
            if (livePos.HasValue)
            {
                var coords = MapHelper.GetLocalPlayerMapCoords()
                          ?? MapHelper.WorldToCurrentMapCoords(livePos.Value.x, livePos.Value.z);
                if (coords.HasValue)
                    ImGui.TextDisabled($"{l.FieldPosition}: X {coords.Value.x:F1}   Y {coords.Value.y:F1}");
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (!_busy)
            {
                if (ImGui.Button(l.BtnModify, UiSizes.SmallButton))
                {
                    _editTitle = _activeSession.Title;
                    _editDesc  = string.Empty;
                    _editing   = true;
                }
                ImGui.SameLine();
                if (ImGui.Button(l.BtnUpdatePos, UiSizes.WideButton))
                    RefreshPosition();
                ImGui.SameLine();
                if (ImGui.Button(l.BtnExtend, UiSizes.MediumButton))
                    ExtendSession(1);
                ImGui.SameLine();
                if (ImGui.Button(l.ViewOnline, UiSizes.SmallButton))
                    OpenUrl(_config.BaseUrl + "/rp-live");

                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.8f, 0.15f, 0.15f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f,  0.2f,  1));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.7f, 0.1f,  0.1f,  1));
                if (ImGui.Button(l.BtnEnd, new Vector2(-1, 0)))
                    EndSession();
                ImGui.PopStyleColor(3);
            }
            else ImGui.TextDisabled(l.Processing);
        }
        else
        {
            ImGui.Text(l.FieldTitle + " *");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##edittitle", ref _editTitle, 100);

            ImGui.Spacing();
            ImGui.Text(l.FieldDesc);
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextMultiline("##editdesc", ref _editDesc, 500, new Vector2(-1, 55));

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var canSave = !_busy && !string.IsNullOrWhiteSpace(_editTitle);
            if (!canSave) ImGui.BeginDisabled();
            if (ImGui.Button(l.Save, UiSizes.MediumButton))
                UpdateSession();
            if (!canSave) ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button(l.Cancel, UiSizes.SmallButton))
                _editing = false;
        }
    }
}

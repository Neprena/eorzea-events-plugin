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

    private string _title         = string.Empty;
    private string _description   = string.Empty;
    private string _characterName = string.Empty;
    private int    _duration      = 2;

    private RpSessionDto? _activeSession;
    private bool          _busy          = false;
    private string        _statusMsg     = string.Empty;
    private bool          _statusIsError = false;

    private bool   _editing   = false;
    private string _editTitle = string.Empty;
    private string _editDesc  = string.Empty;

    private bool _pendingZonePrompt         = false;
    private bool _pendingRpTagPrompt        = false;
    private bool _pendingRpTagActivePrompt  = false;
    private bool _pendingExpiryPrompt       = false;
    private bool _expiryDismissed           = false;

    private bool   _pendingActiveEventWarning = false;
    private string _conflictEstabName         = string.Empty;
    private string _conflictEventTitle        = string.Empty;

    private bool   _pendingActiveRpWarning  = false;
    private string _conflictRpSessionTitle  = string.Empty;
    private string _conflictRpAuthorName    = string.Empty;

    private bool   _pendingEventPromoBlock = false;
    private string _promoEventTitle        = string.Empty;
    private string _promoEstabName         = string.Empty;

    private DateTime _lastSessionCheck = DateTime.MinValue;
    private const int PollIntervalSeconds = 5;

    private DateTime _lastAutoPositionRefresh = DateTime.MinValue;
    private const int AutoPositionRefreshSeconds = 300;

    public bool HasActiveSession => _activeSession != null;

    public MySessionWindow(Configuration config)
        : base("My RP Session##mysession")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 380),
            MaximumSize = new Vector2(950, 700),
        };
        _config = config;
    }

    // ─── Helpers jeu ─────────────────────────────────────────────────────────

    private string GetCurrentWorld()
        => Plugin.ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString() ?? Plugin.L.WorldUnknown;

    private unsafe string GetCurrentZone()
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet == null) return Plugin.L.ZoneUnknown;
        var terType = Plugin.ClientState.TerritoryType;
        var hm = HousingManager.Instance();
        if (hm != null && hm->GetCurrentWard() >= 0)
        {
            var orig = HousingManager.GetOriginalHouseTerritoryTypeId();
            if (orig != 0) terType = orig;
        }
        var row = sheet.GetRowOrDefault(terType);
        return row?.PlaceName.Value.Name.ToString() ?? Plugin.L.ZoneUnknown;
    }

    private (uint territoryId, uint mapId) GetCurrentTerritoryMap()
        => ((uint)Plugin.ClientState.TerritoryType, Plugin.ClientState.MapId);

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
        return new HousingInfo(Ward: ward + 1, Plot: plot >= 0 ? plot + 1 : null,
            Room: room > 0 ? room : null, RawPlot: plot);
    }

    private static bool? ResolveWing(uint mapId, int? rawPlot)
    {
        if (rawPlot == -127) return true;
        if (rawPlot == -128) return false;
        return mapId switch
        {
            72 or 82 or 83 or 364 or 679 => false,
            192 or 193 or 194 or 365 or 680 => true,
            _ => null,
        };
    }

    private static string AppendAnnex(int ward, bool? wing)
        => wing == true ? $"{ward} ({Plugin.L.HousingAnnex})" : ward.ToString();

    private static string FormatHousingLabel(int ward, int? plot, int? room, bool? wing)
    {
        var w = AppendAnnex(ward, wing);
        var l = Plugin.L;
        if (room.HasValue) return string.Format(l.HousingWardRoom, w, room.Value);
        if (plot.HasValue) return string.Format(l.HousingWardPlot, w, plot.Value);
        return string.Format(l.HousingWard, w);
    }

    // ─── API actions ──────────────────────────────────────────────────────────

    public void SetActiveSession(RpSessionDto? session)
    {
        _activeSession            = session;
        _lastAutoPositionRefresh  = DateTime.UtcNow;
        _pendingZonePrompt        = false;
        _pendingRpTagPrompt       = false;
        _pendingRpTagActivePrompt = false;
        _pendingExpiryPrompt      = false;
        _expiryDismissed          = false;
    }

    public void OnZoneChanged()      => _pendingZonePrompt        = true;
    public void OnRpTagRemoved()     => _pendingRpTagPrompt       = true;
    public void OnRpTagActivated()   => _pendingRpTagActivePrompt = true;

    private void StartSession(bool force = false)
    {
        var l = Plugin.L;
        if (!Plugin.Api.HasToken) { ShowError(l.ErrTokenMissing); return; }
        var pos      = GetCurrentPosition();
        var housing  = GetCurrentHousing();
        var (terId, mapId) = GetCurrentTerritoryMap();
        var mapCoords = MapHelper.GetLocalPlayerMapCoords()
                     ?? (pos.HasValue ? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z) : null);
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
            Force         = force,
        };
        if (string.IsNullOrWhiteSpace(req.Title)) { ShowError(l.ErrTitleRequired); return; }

        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var session = await Plugin.Api.CreateSessionAsync(req);
                _activeSession = session; _pendingRpTagActivePrompt = false;
                _pendingActiveEventWarning = false;
                _pendingActiveRpWarning    = false;
                _pendingEventPromoBlock    = false;
                _config.ActiveSessionId = session!.Id; _config.Save();
                ShowSuccess(l.StatusStarted);
                _title = _description = string.Empty;
            }
            catch (ActiveEventConflictException aex)
            {
                _conflictEstabName   = aex.EstablishmentName;
                _conflictEventTitle  = aex.EventTitle;
                _pendingActiveEventWarning = true;
            }
            catch (ActiveRpConflictException rex)
            {
                _conflictRpSessionTitle = rex.SessionTitle;
                _conflictRpAuthorName   = rex.AuthorName;
                _pendingActiveRpWarning = true;
            }
            catch (EventPromotionBlockedException pex)
            {
                _promoEstabName         = pex.EstablishmentName;
                _promoEventTitle        = pex.EventTitle;
                _pendingEventPromoBlock = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("déjà en cours", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var ids     = await Plugin.Api.GetMySessionIdsAsync();
                        var firstId = ids.Count > 0 ? System.Linq.Enumerable.First(ids) : null;
                        var existing = firstId != null ? await Plugin.Api.GetSessionAsync(firstId) : null;
                        if (existing != null)
                        {
                            _activeSession = existing; _pendingRpTagActivePrompt = false;
                            _config.ActiveSessionId = existing.Id; _config.Save();
                            ShowSuccess(l.StatusRecovered);
                            _title = _description = string.Empty;
                            return;
                        }
                    }
                    catch { }
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

    // Met à jour la position de la session active.
    // silent=true : rafraîchissement automatique → aucun feedback UI (pas de _busy ni de
    // _statusMsg) et propagation Discord désactivée côté serveur (req.Silent).
    private void RefreshPosition(bool silent = false)
    {
        var l = Plugin.L;
        if (_activeSession == null) return;
        _lastAutoPositionRefresh = DateTime.UtcNow;
        var pos     = GetCurrentPosition();
        var housing = GetCurrentHousing();
        var (terId, mapId) = GetCurrentTerritoryMap();
        var mapCoords = MapHelper.GetLocalPlayerMapCoords()
                     ?? (pos.HasValue ? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z) : null);
        var charName = GetCharacterName();
        var id = _activeSession.Id;
        var req = new UpdateSessionRequest
        {
            PosX = mapCoords?.Item1, PosZ = mapCoords?.Item2,
            Ward = housing?.Ward, Plot = housing?.Plot, Room = housing?.Room, RawPlot = housing?.RawPlot,
            Location = GetCurrentZone(), Server = GetCurrentWorld(),
            CharacterName = string.IsNullOrEmpty(charName) ? null : charName,
            TerritoryId = terId, MapId = mapId,
            Silent = silent ? true : null,
        };

        if (silent)
        {
            Task.Run(async () =>
            {
                try
                {
                    var updated = await Plugin.Api.UpdateSessionAsync(id, req);
                    if (updated != null) _activeSession = updated;
                }
                catch { /* silencieux : rafraîchissement automatique */ }
            });
            return;
        }

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
                        ? $" (X {updated.PosX.Value:F1}  Y {updated.PosZ.Value:F1})" : string.Empty;
                    ShowSuccess(l.StatusPosUpdated + posMsg);
                }
                else ShowError(l.ErrUpdate);
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    /// <summary>
    /// Appelé à chaque tick du framework : déclenche un rafraîchissement automatique et silencieux
    /// de la position toutes les 5 min si l'option est activée et qu'une session est active.
    /// </summary>
    public void AutoRefreshPositionIfDue()
    {
        if (_activeSession == null || _busy) return;
        if (!_config.AutoRefreshPosition) return;
        if ((DateTime.UtcNow - _lastAutoPositionRefresh).TotalSeconds < AutoPositionRefreshSeconds) return;
        RefreshPosition(silent: true);
    }

    private void ExtendSession(int hours = 1)
    {
        var l = Plugin.L;
        if (_activeSession == null) return;
        _busy = true; _statusMsg = string.Empty;
        var id = _activeSession.Id;
        Task.Run(async () =>
        {
            try
            {
                var updated = await Plugin.Api.UpdateSessionAsync(id, new UpdateSessionRequest { Duration = hours });
                if (updated != null) { _activeSession = updated; _expiryDismissed = false; ShowSuccess(string.Format(l.StatusExtended, hours)); }
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
                _activeSession = null; _pendingZonePrompt = false;
                _pendingRpTagPrompt = false; _pendingRpTagActivePrompt = false;
                _config.ActiveSessionId = null; _config.Save();
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

        if (_config.AlertOnSessionExpiring && !_pendingExpiryPrompt && !_expiryDismissed
            && _activeSession.ExpiresAt != null
            && DateTime.TryParse(_activeSession.ExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var exp)
            && (exp - DateTime.UtcNow).TotalMinutes is > 0 and <= 15)
        {
            _pendingExpiryPrompt = true;
            IsOpen = true;
        }

        var id = _activeSession.Id;
        Task.Run(async () =>
        {
            try
            {
                var session = await Plugin.Api.GetSessionAsync(id);
                if (session == null || session.EndedAt != null)
                {
                    _activeSession = null; _pendingExpiryPrompt = false;
                    _expiryDismissed = false; _config.ActiveSessionId = null; _config.Save();
                }
                else _activeSession = session;
            }
            catch { }
        });
    }

    private void ShowError(string msg)   { _statusMsg = msg; _statusIsError = true;  }
    private void ShowSuccess(string msg) { _statusMsg = msg; _statusIsError = false; }

    // ─── Draw ─────────────────────────────────────────────────────────────────

    public override void Draw()
    {
        var l = Plugin.L;
        WindowName = l.MySessionTitle + "##mysession";

        if (!Plugin.Api.HasToken || !Plugin.Api.IsTokenValid)
        {
            var tokenMissing = !Plugin.Api.HasToken;
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 0.6f, 0, 1),
                tokenMissing ? l.ErrTokenMissing : "⚠  " + l.TokenInvalidLine1);
            ImGui.Spacing();
            ImGui.TextWrapped(tokenMissing ? l.MySessionTokenMissingDesc : l.MySessionTokenInvalidDesc);
            ImGui.Spacing();
            if (UiPrimitives.ColorButton(tokenMissing ? l.BtnConfigureNow : l.TokenReconfigure, UiStyle.PrimaryButton,
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                Plugin.OpenSetup(tokenInvalid: !tokenMissing);
            return;
        }

        if (_activeSession != null) DrawActiveSession();
        else DrawCreateForm();

        if (!string.IsNullOrWhiteSpace(_statusMsg))
        {
            ImGui.Spacing();
            ImGui.TextColored(_statusIsError ? new Vector4(1, 0.35f, 0.35f, 1) : UiStyle.StatusOpen, _statusMsg);
        }
    }

    // ─── Création : contexte (gauche 38%) | formulaire (droite 62%) ──────────

    private void DrawCreateForm()
    {
        var l = Plugin.L;
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextSection, l.SessionCreate.ToUpper());
        ImGui.Separator();
        ImGui.Spacing();

        if (_pendingRpTagActivePrompt)
            UiPrimitives.DrawAlert(UiStyle.StatusOpen, l.AlertRpTagActivTitle, l.AlertRpTagActivDesc, () =>
            {
                if (ImGui.Button(l.Ignore + "##rptag_active", UiStyle.SmallButton))
                    { _pendingRpTagActivePrompt = false; IsOpen = false; }
            });

        if (_pendingActiveEventWarning)
            UiPrimitives.DrawAlert(new Vector4(1f, 0.75f, 0.1f, 1f),
                l.AlertActiveEventTitle,
                string.Format(l.AlertActiveEventDesc, _conflictEventTitle, _conflictEstabName),
                () =>
                {
                    if (UiPrimitives.ColorButton(l.BtnCreateAnyway + "##activeevent", UiStyle.WideButton,
                        UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                        { _pendingActiveEventWarning = false; StartSession(force: true); }
                    ImGui.SameLine();
                    if (ImGui.Button(l.Cancel + "##activeevent", UiStyle.SmallButton))
                        _pendingActiveEventWarning = false;
                });

        if (_pendingActiveRpWarning)
            UiPrimitives.DrawAlert(new Vector4(1f, 0.75f, 0.1f, 1f),
                l.AlertActiveRpTitle,
                string.Format(l.AlertActiveRpDesc, _conflictRpSessionTitle, _conflictRpAuthorName),
                () =>
                {
                    if (UiPrimitives.ColorButton(l.BtnCreateAnyway + "##activerp", UiStyle.WideButton,
                        UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                        { _pendingActiveRpWarning = false; StartSession(force: true); }
                    ImGui.SameLine();
                    if (ImGui.Button(l.Cancel + "##activerp", UiStyle.SmallButton))
                        _pendingActiveRpWarning = false;
                });

        // Blocage dur (IA) : promo d'un évènement déjà annoncé → PAS de bouton "forcer".
        if (_pendingEventPromoBlock)
            UiPrimitives.DrawAlert(new Vector4(0.9f, 0.25f, 0.25f, 1f),
                l.AlertEventPromoTitle,
                string.Format(l.AlertEventPromoDesc, _promoEventTitle, _promoEstabName),
                () =>
                {
                    if (ImGui.Button(l.Cancel + "##eventpromo", UiStyle.SmallButton))
                        _pendingEventPromoBlock = false;
                });

        if (!ImGui.BeginTable("##createform", 2, ImGuiTableFlags.None)) return;
        ImGui.TableSetupColumn("ctx",  ImGuiTableColumnFlags.WidthStretch, 0.38f);
        ImGui.TableSetupColumn("form", ImGuiTableColumnFlags.WidthStretch, 0.62f);
        ImGui.TableNextRow();

        // Contexte détecté (gauche)
        ImGui.TableSetColumnIndex(0);
        var pos     = GetCurrentPosition();
        var housing = GetCurrentHousing();
        var wing    = housing != null ? ResolveWing(Plugin.ClientState.MapId, housing.RawPlot) : null;

        UiPrimitives.DrawCard(() =>
        {
            ImGui.TextColored(UiStyle.TextSection, l.FieldLocation.ToUpper());
            ImGui.Spacing();
            UiPrimitives.DrawIcon("");
            ImGui.SameLine(0, 4);
            ImGui.TextColored(UiStyle.TextMuted, GetCurrentZone());
            ImGui.TextColored(UiStyle.TextSubtle, $"  {GetCurrentWorld()}");
            if (housing != null)
            {
                UiPrimitives.DrawIcon("");
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted, FormatHousingLabel(housing.Ward, housing.Plot, housing.Room, wing));
            }
            if (pos.HasValue)
            {
                var c = MapHelper.GetLocalPlayerMapCoords()
                     ?? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z);
                UiPrimitives.DrawIcon("");
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextSubtle, c.HasValue
                    ? $"X {c.Value.x:F1}   Y {c.Value.y:F1}"
                    : $"X {pos.Value.x:F1}   Y {pos.Value.z:F1}");
            }
        });

        // Formulaire (droite)
        ImGui.TableSetColumnIndex(1);

        ImGui.TextColored(UiStyle.TextTitle, "✦ " + l.FieldTitle + " *");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##title", ref _title, 100);

        ImGui.Spacing();
        if (string.IsNullOrEmpty(_characterName)) _characterName = GetCharacterName();
        ImGui.TextColored(UiStyle.TextMuted, l.FieldCharName);
        ImGui.SetNextItemWidth(-(UiStyle.SmallButton.X + ImGui.GetStyle().ItemSpacing.X));
        ImGui.InputText("##charname", ref _characterName, 60);
        ImGui.SameLine();
        if (ImGui.Button(l.Auto, UiStyle.SmallButton)) _characterName = GetCharacterName();

        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextSubtle, l.FieldDesc + " (opt.)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("##desc", ref _description, 500, new Vector2(-1, 60));

        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextMuted, l.FieldDuration);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderInt("##duration", ref _duration, 1, 8);

        ImGui.Spacing();
        var canStart = !_busy && !string.IsNullOrWhiteSpace(_title);
        if (!canStart) ImGui.BeginDisabled();
        if (UiPrimitives.ColorButton(_busy ? l.StatusCreating : l.RpNewSession, new Vector2(-1, 0),
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
            StartSession();
        if (!canStart) ImGui.EndDisabled();

        ImGui.EndTable();
    }

    // ─── Session active : alertes + info (gauche 58%) | actions (droite 42%) ─

    private void DrawActiveSession()
    {
        var l = Plugin.L;
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.StatusOpen, l.SessionActive.ToUpper());
        ImGui.Separator();
        ImGui.Spacing();

        if (_pendingZonePrompt)
            UiPrimitives.DrawAlert(new Vector4(1f, 0.75f, 0.1f, 1f), l.AlertZoneChangedTitle, l.AlertZoneChangedDesc, () =>
            {
                if (UiPrimitives.ColorButton(l.BtnUpdatePos + "##zone", UiStyle.WideButton,
                    UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                    { _pendingZonePrompt = false; RefreshPosition(); }
                ImGui.SameLine();
                if (ImGui.Button(l.Ignore + "##zone", UiStyle.SmallButton))
                    { _pendingZonePrompt = false; IsOpen = false; }
            });

        if (_pendingRpTagPrompt)
            UiPrimitives.DrawAlert(new Vector4(0.75f, 0.5f, 1f, 1f), l.AlertRpTagRemovedTitle, l.AlertRpTagRemovedDesc, () =>
            {
                if (UiPrimitives.ColorButton(l.BtnEnd + "##rptag", UiStyle.MediumButton,
                    UiStyle.DangerNormal, UiStyle.DangerHovered, UiStyle.DangerActive))
                    { _pendingRpTagPrompt = false; EndSession(); }
                ImGui.SameLine();
                if (ImGui.Button(l.Ignore + "##rptag", UiStyle.SmallButton))
                    _pendingRpTagPrompt = false;
            });

        if (_pendingExpiryPrompt && _activeSession?.ExpiresAt != null
            && DateTime.TryParse(_activeSession.ExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            var mins = Math.Max(1, (int)Math.Ceiling((expiresAt - DateTime.UtcNow).TotalMinutes));
            UiPrimitives.DrawAlert(new Vector4(1f, 0.55f, 0.15f, 1f), l.AlertExpiryTitle, string.Format(l.AlertExpiryDesc, mins), () =>
            {
                if (UiPrimitives.ColorButton(l.BtnExtend + "##expiry", UiStyle.WideButton,
                    UiStyle.SuccessNormal, UiStyle.SuccessHovered, UiStyle.SuccessActive))
                    { _pendingExpiryPrompt = false; ExtendSession(1); }
                ImGui.SameLine();
                if (UiPrimitives.ColorButton(l.BtnStop + "##expiry_stop", UiStyle.MediumButton,
                    UiStyle.DangerNormal, UiStyle.DangerHovered, UiStyle.DangerActive))
                    { _pendingExpiryPrompt = false; EndSession(); }
                ImGui.SameLine();
                if (ImGui.Button(l.Ignore + "##expiry", UiStyle.SmallButton))
                    { _pendingExpiryPrompt = false; _expiryDismissed = true; }
            });
        }

        if (_editing) { DrawEditForm(); return; }

        if (!ImGui.BeginTable("##activesession", 2, ImGuiTableFlags.None)) return;
        ImGui.TableSetupColumn("info",    ImGuiTableColumnFlags.WidthStretch, 0.58f);
        ImGui.TableSetupColumn("actions", ImGuiTableColumnFlags.WidthStretch, 0.42f);
        ImGui.TableNextRow();

        // Info session (gauche)
        ImGui.TableSetColumnIndex(0);
        UiPrimitives.DrawCard(() =>
        {
            ImGui.TextColored(UiStyle.TextTitle, _activeSession!.Title);
            ImGui.Spacing();
            UiPrimitives.DrawIcon("");
            ImGui.SameLine(0, 4);
            ImGui.TextColored(UiStyle.TextMuted, $"{_activeSession.Location}  •  {_activeSession.Server}");
            if (!string.IsNullOrEmpty(_activeSession.CharacterName))
            {
                UiPrimitives.DrawIcon("");
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted, _activeSession.CharacterName);
            }
            if (_activeSession.Ward.HasValue)
            {
                UiPrimitives.DrawIcon("");
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted,
                    FormatHousingLabel(_activeSession.Ward.Value, _activeSession.Plot, _activeSession.Room, _activeSession.Wing));
            }
            var livePos = GetCurrentPosition();
            if (livePos.HasValue)
            {
                var coords = MapHelper.GetLocalPlayerMapCoords()
                          ?? MapHelper.WorldToCurrentMapCoords(livePos.Value.x, livePos.Value.z);
                if (coords.HasValue)
                {
                    UiPrimitives.DrawIcon("");
                    ImGui.SameLine(0, 4);
                    ImGui.TextColored(UiStyle.TextSubtle, $"X {coords.Value.x:F1}   Y {coords.Value.y:F1}");
                }
            }
        });

        // Actions (droite)
        ImGui.TableSetColumnIndex(1);
        if (_busy)
        {
            ImGui.TextColored(UiStyle.TextSubtle, Plugin.L.Processing);
        }
        else
        {
            if (ImGui.Button(l.BtnModify, new Vector2(-1, 0)))
                { _editTitle = _activeSession!.Title; _editDesc = string.Empty; _editing = true; }
            ImGui.Spacing();
            if (ImGui.Button(l.BtnUpdatePos, new Vector2(-1, 0))) RefreshPosition();
            ImGui.Spacing();
            if (UiPrimitives.ColorButton(l.BtnExtend, new Vector2(-1, 0),
                UiStyle.SuccessNormal, UiStyle.SuccessHovered, UiStyle.SuccessActive))
                ExtendSession(1);
            ImGui.Spacing();
            if (ImGui.Button(l.ViewOnline, new Vector2(-1, 0)))
                OpenUrl(_config.BaseUrl + "/rp-live");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (UiPrimitives.ColorButton(l.BtnEnd, new Vector2(-1, 0),
                UiStyle.DangerNormal, UiStyle.DangerHovered, UiStyle.DangerActive))
                EndSession();
        }

        ImGui.EndTable();
    }

    private void DrawEditForm()
    {
        var l = Plugin.L;
        ImGui.Spacing();
        UiPrimitives.DrawCard(() =>
        {
            ImGui.TextColored(UiStyle.TextTitle, "✦ " + l.FieldTitle + " *");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##edittitle", ref _editTitle, 100);
            ImGui.Spacing();
            ImGui.TextColored(UiStyle.TextSubtle, l.FieldDesc + " (opt.)");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextMultiline("##editdesc", ref _editDesc, 500, new Vector2(-1, 60));
            ImGui.Spacing();
            var canSave = !_busy && !string.IsNullOrWhiteSpace(_editTitle);
            if (!canSave) ImGui.BeginDisabled();
            if (UiPrimitives.ColorButton(l.Save, UiStyle.MediumButton,
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                UpdateSession();
            if (!canSave) ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button(l.Cancel, UiStyle.SmallButton)) _editing = false;
        });
    }
}

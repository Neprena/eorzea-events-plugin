using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using EorzeaEventsPlugin.Api;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace EorzeaEventsPlugin.Windows;

public class MySessionWindow : ThemedWindow
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
    private string _promoReason            = string.Empty;

    private DateTime _lastSessionCheck = DateTime.MinValue;
    private const int PollIntervalSeconds = 5;

    private DateTime _lastAutoPositionRefresh = DateTime.MinValue;
    private const int AutoPositionRefreshSeconds = 300;

    public bool HasActiveSession => _activeSession != null;

    public MySessionWindow(Configuration config)
        : base("My RP Session##mysession")
    {
        LogicalSizeConstraints = new WindowSizeConstraints
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
        // Ne substituer le nom de zone par l'intérieur d'estate QUE lorsqu'on est
        // réellement dans un logement. IndoorTerritory != null = intérieur chargé ;
        // null en plein ward (où GetCurrentWard() >= 0 est trompeur).
        if (hm != null && hm->IndoorTerritory != null)
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
            InstanceId    = Plugin.GetPublicInstanceId(),
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
                _promoReason            = Plugin.L == Loc.Fr ? pex.ReasonFr : pex.ReasonEn;
                if (string.IsNullOrEmpty(_promoReason))
                    _promoReason = !string.IsNullOrEmpty(pex.ReasonFr) ? pex.ReasonFr : pex.ReasonEn;
                _pendingEventPromoBlock = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("déjà en cours", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var ids     = await Plugin.Api.GetMySessionIdsAsync() ?? [];
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
            TerritoryId = terId, InstanceId = Plugin.GetPublicInstanceId(), MapId = mapId,
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

        if (!Plugin.Api.HasToken || Plugin.Api.IsTokenRevoked)
        {
            var tokenMissing = !Plugin.Api.HasToken;

            Feedback.EmptyState(Icons.Warning,
                tokenMissing ? l.ErrTokenMissing : l.TokenInvalidLine1,
                tokenMissing ? l.MySessionTokenMissingDesc : l.MySessionTokenInvalidDesc,
                tokenMissing ? l.BtnConfigureNow : l.TokenReconfigure,
                () => Plugin.OpenSetup(tokenInvalid: !tokenMissing));
            return;
        }

        if (_activeSession != null) DrawActiveSession();
        else DrawCreateForm();

        if (!string.IsNullOrWhiteSpace(_statusMsg))
        {
            Layout.Spacer(Theme.GapS);
            ImGui.TextColored(_statusIsError ? Theme.Danger : Theme.Online,
                              $"{(_statusIsError ? Icons.Warning : Icons.Check).S()}  {_statusMsg}");
        }
    }

    // ─── Création : contexte (gauche 38%) | formulaire (droite 62%) ──────────

    private void DrawCreateForm()
    {
        var l = Plugin.L;

        Layout.SectionHeader(l.SessionCreate, Icons.Plus);

        DrawPendingAlerts(l);

        if (!ImGui.BeginTable("##createform", 2, ImGuiTableFlags.None)) return;
        ImGui.TableSetupColumn("ctx",  ImGuiTableColumnFlags.WidthStretch, 0.38f);
        ImGui.TableSetupColumn("form", ImGuiTableColumnFlags.WidthStretch, 0.62f);
        ImGui.TableNextRow();

        // Contexte détecté (gauche)
        ImGui.TableSetColumnIndex(0);
        DrawDetectedContext(l);

        // Formulaire (droite)
        ImGui.TableSetColumnIndex(1);

        Inputs.Field("##title", l.FieldTitle + " *", ref _title, 100, showCounter: true);

        Layout.Spacer(Theme.GapS);
        // Nom du perso non modifiable : toujours le personnage actuellement
        // connecté, lié à la clé API. Affichage en lecture seule.
        _characterName = GetCharacterName();
        Text.Muted(l.FieldCharName);
        Text.WithIcon(Icons.Character, _characterName, Theme.Text, Theme.TextMuted);

        Layout.Spacer(Theme.GapS);
        Inputs.Field("##desc", l.FieldDesc, ref _description, 500,
                     multiline: true, height: 70f);

        Layout.Spacer(Theme.GapS);
        Text.Muted(l.FieldDuration);
        ImGui.SetNextItemWidth(Card.FullWidth);
        ImGui.SliderInt("##duration", ref _duration, 1, 8);

        Layout.Spacer(Theme.GapM);
        if (Btn.Draw(_busy ? l.StatusCreating : l.RpNewSession, BtnTone.Primary, BtnSize.Block,
                     Icons.RpLive, disabled: _busy || string.IsNullOrWhiteSpace(_title)))
            StartSession();

        ImGui.EndTable();
    }

    /// <summary>Position, monde et logement lus dans le jeu, avant publication.</summary>
    private void DrawDetectedContext(Loc l)
    {
        var pos     = GetCurrentPosition();
        var housing = GetCurrentHousing();
        var wing    = housing != null ? ResolveWing(Plugin.ClientState.MapId, housing.RawPlot) : null;

        using var card = Card.Begin("session_ctx", interactive: false);

        Layout.SectionHeader(l.FieldLocation, Icons.Location);

        Text.WithIcon(Icons.World, $"{GetCurrentZone()}  ·  {GetCurrentWorld()}",
                      Theme.TextMuted, Theme.TextFaint, wrap: true);

        if (housing != null)
            Text.WithIcon(Icons.Housing,
                          FormatHousingLabel(housing.Ward, housing.Plot, housing.Room, wing),
                          Theme.TextMuted, Theme.TextFaint, wrap: true);

        if (!pos.HasValue) return;

        var c = MapHelper.GetLocalPlayerMapCoords()
             ?? MapHelper.WorldToCurrentMapCoords(pos.Value.x, pos.Value.z);

        Text.WithIcon(Icons.Map,
                      c.HasValue ? $"X {c.Value.x:F1}   Y {c.Value.y:F1}"
                                 : $"X {pos.Value.x:F1}   Y {pos.Value.z:F1}",
                      Theme.TextFaint, Theme.TextFaint);
    }

    /// <summary>
    /// Bandeaux de confirmation avant création. Le blocage pour promotion d'un
    /// événement déjà annoncé n'offre volontairement pas de « créer quand
    /// même » : c'est un refus, pas un avertissement.
    ///
    /// Les boutons de ces bandeaux sont en tonalité <c>Secondary</c> et non
    /// <c>Ghost</c>. Un bouton fantôme a un fond transparent, invisible sur la
    /// carte teintée que peint <c>Feedback.Alert</c> : quand il est seul dans
    /// l'encart, rien ne signale qu'il y a un contrôle avant de le survoler.
    /// </summary>
    private void DrawPendingAlerts(Loc l)
    {
        if (_pendingRpTagActivePrompt)
            Feedback.Alert(Theme.Online, Icons.Check, l.AlertRpTagActivTitle, l.AlertRpTagActivDesc, () =>
            {
                if (Btn.Draw(l.Ignore, BtnTone.Secondary, BtnSize.Small, id: "rptag_active"))
                    { _pendingRpTagActivePrompt = false; IsOpen = false; }
            });

        if (_pendingActiveEventWarning)
            Feedback.Alert(Theme.Idle, Icons.Warning, l.AlertActiveEventTitle,
                string.Format(l.AlertActiveEventDesc, _conflictEventTitle, _conflictEstabName),
                () => ConfirmOrCancel("activeevent", () => _pendingActiveEventWarning = false));

        if (_pendingActiveRpWarning)
            Feedback.Alert(Theme.Idle, Icons.Warning, l.AlertActiveRpTitle,
                string.Format(l.AlertActiveRpDesc, _conflictRpSessionTitle, _conflictRpAuthorName),
                () => ConfirmOrCancel("activerp", () => _pendingActiveRpWarning = false));

        if (_pendingEventPromoBlock)
            Feedback.Alert(Theme.Danger, Icons.Blocked, l.AlertEventPromoTitle,
                string.Format(l.AlertEventPromoDesc, _promoEventTitle, _promoEstabName)
                    + (string.IsNullOrEmpty(_promoReason)
                        ? string.Empty
                        : "\n\n" + string.Format(l.AlertEventPromoReason, _promoReason)),
                () =>
                {
                    if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Small, id: "eventpromo"))
                        _pendingEventPromoBlock = false;
                });

        // System.Action explicite : Lumina expose aussi un type « Action ».
        void ConfirmOrCancel(string id, System.Action dismiss)
        {
            if (Btn.Draw(l.BtnCreateAnyway, BtnTone.Primary, BtnSize.Medium, id: $"force_{id}"))
                { dismiss(); StartSession(force: true); }

            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Small, id: $"cancel_{id}"))
                dismiss();
        }
    }

    // ─── Session active : alertes + info (gauche 58%) | actions (droite 42%) ─

    private void DrawActiveSession()
    {
        var l = Plugin.L;

        Layout.SectionHeader(l.SessionActive, Icons.RpLive, tone: Theme.Online);

        if (_pendingZonePrompt)
            Feedback.Alert(Theme.Idle, Icons.Location, l.AlertZoneChangedTitle, l.AlertZoneChangedDesc, () =>
            {
                if (Btn.Draw(l.BtnUpdatePos, BtnTone.Primary, BtnSize.Medium, Icons.Refresh, id: "zone_upd"))
                    { _pendingZonePrompt = false; RefreshPosition(); }

                ImGui.SameLine(0f, Theme.S(Theme.GapS));
                if (Btn.Draw(l.Ignore, BtnTone.Secondary, BtnSize.Small, id: "zone_ign"))
                    { _pendingZonePrompt = false; IsOpen = false; }
            });

        if (_pendingRpTagPrompt)
            Feedback.Alert(Theme.Link, Icons.Warning, l.AlertRpTagRemovedTitle, l.AlertRpTagRemovedDesc, () =>
            {
                if (Btn.Draw(l.BtnEnd, BtnTone.Danger, BtnSize.Medium, id: "rptag_end"))
                    { _pendingRpTagPrompt = false; EndSession(); }

                ImGui.SameLine(0f, Theme.S(Theme.GapS));
                if (Btn.Draw(l.Ignore, BtnTone.Secondary, BtnSize.Small, id: "rptag_ign"))
                    _pendingRpTagPrompt = false;
            });

        if (_pendingExpiryPrompt && _activeSession?.ExpiresAt != null
            && DateTime.TryParse(_activeSession.ExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            var mins = Math.Max(1, (int)Math.Ceiling((expiresAt - DateTime.UtcNow).TotalMinutes));
            Feedback.Alert(Theme.Idle, Icons.Clock, l.AlertExpiryTitle, string.Format(l.AlertExpiryDesc, mins), () =>
            {
                if (Btn.Draw(l.BtnExtend, BtnTone.Primary, BtnSize.Medium, Icons.Plus, id: "exp_ext"))
                    { _pendingExpiryPrompt = false; ExtendSession(1); }

                ImGui.SameLine(0f, Theme.S(Theme.GapS));
                if (Btn.Draw(l.BtnStop, BtnTone.Danger, BtnSize.Medium, id: "exp_stop"))
                    { _pendingExpiryPrompt = false; EndSession(); }

                ImGui.SameLine(0f, Theme.S(Theme.GapS));
                if (Btn.Draw(l.Ignore, BtnTone.Secondary, BtnSize.Small, id: "exp_ign"))
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
        DrawActiveSessionCard();

        // Actions (droite)
        ImGui.TableSetColumnIndex(1);
        if (_busy)
        {
            Text.Muted(l.Processing);
        }
        else
        {
            if (Btn.Draw(l.BtnModify, BtnTone.Secondary, BtnSize.Block, Icons.Edit))
                { _editTitle = _activeSession!.Title; _editDesc = string.Empty; _editing = true; }

            Layout.Spacer(Theme.GapXs);
            if (Btn.Draw(l.BtnUpdatePos, BtnTone.Secondary, BtnSize.Block, Icons.Location))
                RefreshPosition();

            Layout.Spacer(Theme.GapXs);
            if (Btn.Draw(l.BtnExtend, BtnTone.Primary, BtnSize.Block, Icons.Plus))
                ExtendSession(1);

            Layout.Spacer(Theme.GapXs);
            if (Btn.Draw(l.ViewOnline, BtnTone.Ghost, BtnSize.Block, Icons.External))
                OpenUrl(_config.BaseUrl + "/rp-live");

            Layout.Divider(Theme.GapS);
            if (Btn.Draw(l.BtnEnd, BtnTone.Danger, BtnSize.Block, Icons.Close))
                EndSession();
        }

        ImGui.EndTable();
    }

    /// <summary>Récapitulatif de la session publiée, tel que les autres la voient.</summary>
    private void DrawActiveSessionCard()
    {
        using var card = Card.Begin("session_active", interactive: false);

        Text.H2(_activeSession!.Title);
        Layout.Spacer(Theme.GapS);

        Text.WithIcon(Icons.Location, $"{_activeSession.Location}  ·  {_activeSession.Server}",
                      Theme.TextMuted, Theme.TextFaint, wrap: true);

        if (!string.IsNullOrEmpty(_activeSession.CharacterName))
            Text.WithIcon(Icons.Character, _activeSession.CharacterName,
                          Theme.TextMuted, Theme.TextFaint);

        if (_activeSession.Ward.HasValue)
            Text.WithIcon(Icons.Housing,
                          FormatHousingLabel(_activeSession.Ward.Value, _activeSession.Plot,
                                             _activeSession.Room, _activeSession.Wing),
                          Theme.TextMuted, Theme.TextFaint);

        var livePos = GetCurrentPosition();
        if (livePos is not { } position) return;

        var coords = MapHelper.GetLocalPlayerMapCoords()
                  ?? MapHelper.WorldToCurrentMapCoords(position.x, position.z);
        if (coords is not { } c) return;

        Text.WithIcon(Icons.Map, $"X {c.x:F1}   Y {c.y:F1}", Theme.TextFaint, Theme.TextFaint);
    }

    private void DrawEditForm()
    {
        var l = Plugin.L;

        using var card = Card.Begin("session_edit", interactive: false);

        Inputs.Field("##edittitle", l.FieldTitle + " *", ref _editTitle, 100, showCounter: true);

        Layout.Spacer(Theme.GapS);
        Inputs.Field("##editdesc", l.FieldDesc, ref _editDesc, 500,
                     multiline: true, height: 70f);

        Layout.Spacer(Theme.GapM);
        if (Btn.Draw(l.Save, BtnTone.Primary, BtnSize.Medium, Icons.Check,
                     disabled: _busy || string.IsNullOrWhiteSpace(_editTitle)))
            UpdateSession();

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Small)) _editing = false;
    }
}

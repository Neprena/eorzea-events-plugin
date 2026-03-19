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
        : base("Ma session RP##mysession")
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
        => Plugin.ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString() ?? "Monde inconnu";

    private string GetCurrentZone()
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet == null) return "Zone inconnue";
        var row = sheet.GetRowOrDefault(Plugin.ClientState.TerritoryType);
        return row?.PlaceName.Value.Name.ToString() ?? "Zone inconnue";
    }

    private (uint territoryId, uint mapId) GetCurrentTerritoryMap()
    {
        var territoryId = (uint)Plugin.ClientState.TerritoryType;
        var sheet       = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var row         = sheet?.GetRowOrDefault(Plugin.ClientState.TerritoryType);
        var mapId       = row?.Map.RowId ?? 0u;
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

    private unsafe record HousingInfo(int Ward, int? Plot, int? Room);

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
            Room: room > 0  ? room     : null
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
        if (string.IsNullOrWhiteSpace(Plugin.Config.ApiToken)) { ShowError("Token API non configuré."); return; }
        var pos      = GetCurrentPosition();
        var housing  = GetCurrentHousing();
        var (terId, mapId) = GetCurrentTerritoryMap();
        var req = new CreateSessionRequest
        {
            Title         = _title.Trim(),
            Description   = string.IsNullOrWhiteSpace(_description) ? null : _description.Trim(),
            Location      = GetCurrentZone(),
            Server        = GetCurrentWorld(),
            CharacterName = string.IsNullOrWhiteSpace(_characterName) ? null : _characterName.Trim(),
            PosX          = pos?.x,
            PosZ          = pos?.z,
            Ward          = housing?.Ward,
            Plot          = housing?.Plot ?? housing?.Room,
            Duration      = _duration,
            TerritoryId   = terId,
            MapId         = mapId,
        };
        if (string.IsNullOrWhiteSpace(req.Title)) { ShowError("Le titre est requis."); return; }

        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var session = await Plugin.Api.CreateSessionAsync(req);
                if (session != null)
                {
                    _activeSession            = session;
                    _pendingRpTagActivePrompt = false;
                    _config.ActiveSessionId   = session.Id;
                    _config.Save();
                    ShowSuccess("Session démarrée !");
                    _title = _description = string.Empty;
                }
                else ShowError("Erreur lors de la création.");
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void UpdateSession()
    {
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
                if (updated != null) { _activeSession = updated; _editing = false; ShowSuccess("Session mise à jour."); }
                else ShowError("Erreur lors de la mise à jour.");
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void RefreshPosition()
    {
        if (_activeSession == null) return;
        var pos      = GetCurrentPosition();
        var housing  = GetCurrentHousing();
        var zone     = GetCurrentZone();
        var world    = GetCurrentWorld();
        var charName = GetCharacterName();
        var (terId, mapId) = GetCurrentTerritoryMap();
        var id  = _activeSession.Id;
        var req = new UpdateSessionRequest
        {
            PosX          = pos?.x,
            PosZ          = pos?.z,
            Ward          = housing?.Ward,
            Plot          = housing?.Plot ?? housing?.Room,
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
                if (updated != null) { _activeSession = updated; ShowSuccess("Position mise à jour."); }
                else ShowError("Erreur lors de la mise à jour.");
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void ExtendSession(int hours = 1)
    {
        if (_activeSession == null) return;
        var id  = _activeSession.Id;
        var req = new UpdateSessionRequest { Duration = hours };
        _busy = true; _statusMsg = string.Empty;
        Task.Run(async () =>
        {
            try
            {
                var updated = await Plugin.Api.UpdateSessionAsync(id, req);
                if (updated != null) { _activeSession = updated; ShowSuccess($"Session prolongée de {hours}h."); }
                else ShowError("Erreur lors de la prolongation.");
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { _busy = false; }
        });
    }

    private void EndSession()
    {
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
                ShowSuccess("Session terminée.");
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
        if (string.IsNullOrWhiteSpace(_config.ApiToken))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 0.6f, 0, 1), "Token API non configuré.");
            ImGui.TextWrapped("Générez un token sur votre dashboard, puis collez-le dans la configuration.");
            ImGui.Spacing();
            if (ImGui.Button("Ouvrir la configuration"))
                Plugin.OpenConfig();
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
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), "Nouvelle session RP sauvage");
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
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f), "✦  Tag RP activé !");
            ImGui.TextWrapped("Vous êtes en mode RP. Souhaitez-vous annoncer une session RP sauvage ?");
            ImGui.Spacing();
            if (ImGui.SmallButton("Ignorer##rptag_active")) { _pendingRpTagActivePrompt = false; IsOpen = false; }
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
        ImGui.TextDisabled($"Serveur : {world}   •   Zone : {zone}");
        if (housing != null)
        {
            var loc = housing.Plot.HasValue
                ? $"Quartier {housing.Ward}  —  Parcelle {housing.Plot}"
                : housing.Room.HasValue
                    ? $"Quartier {housing.Ward}  —  Appartement {housing.Room}"
                    : $"Quartier {housing.Ward}";
            ImGui.TextDisabled($"Logement : {loc}");
        }
        if (pos.HasValue)
            ImGui.TextDisabled($"Position : X {pos.Value.x:F1}   Y {pos.Value.z:F1}");
        ImGui.Spacing();

        ImGui.Text("Titre *");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##title", ref _title, 100);

        ImGui.Spacing();
        // Pré-remplissage automatique si le champ est vide
        if (string.IsNullOrEmpty(_characterName))
            _characterName = GetCharacterName();

        ImGui.Text("Nom du personnage");
        ImGui.SetNextItemWidth(-80);
        ImGui.InputText("##charname", ref _characterName, 60);
        ImGui.SameLine();
        if (ImGui.Button("Auto"))
            _characterName = GetCharacterName();

        ImGui.Spacing();
        ImGui.Text("Description (optionnelle)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("##desc", ref _description, 500, new Vector2(-1, 60));

        ImGui.Spacing();
        ImGui.Text("Durée (heures)");
        ImGui.SetNextItemWidth(120);
        ImGui.SliderInt("##duration", ref _duration, 1, 8);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var canStart = !_busy && !string.IsNullOrWhiteSpace(_title);
        if (!canStart) ImGui.BeginDisabled();
        if (ImGui.Button(_busy ? "Démarrage..." : "Démarrer la session", new Vector2(-1, 0)))
            StartSession();
        if (!canStart) ImGui.EndDisabled();
    }

    // ─── Session active ───────────────────────────────────────────────────────

    private void DrawActiveSession()
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), "Session en cours");
        ImGui.Separator();
        ImGui.Spacing();

        // ── Alerte : changement de zone/TP ──────────────────────────────────
        if (_pendingZonePrompt)
        {
            var dl    = ImGui.GetWindowDrawList();
            var avail = ImGui.GetContentRegionAvail().X;
            var p0    = ImGui.GetCursorScreenPos();
            dl.ChannelsSplit(2);
            dl.ChannelsSetCurrent(1); // contenu au-dessus
            ImGui.Spacing();
            ImGui.Indent(8f);
            ImGui.TextColored(new Vector4(1f, 0.75f, 0.1f, 1f), "⚠  Changement de zone détecté");
            ImGui.TextWrapped("Voulez-vous mettre à jour votre emplacement ou terminer la session ?");
            ImGui.Spacing();
            if (ImGui.SmallButton("Update de la position")) { _pendingZonePrompt = false; RefreshPosition(); }
            ImGui.SameLine();
            if (ImGui.SmallButton("Ignorer##zone"))        { _pendingZonePrompt = false; IsOpen = false; }
            ImGui.Unindent(8f);
            ImGui.Spacing();
            var p1 = ImGui.GetCursorScreenPos();
            dl.ChannelsSetCurrent(0); // fond en dessous
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
            dl.ChannelsSetCurrent(1); // contenu au-dessus
            ImGui.Spacing();
            ImGui.Indent(8f);
            ImGui.TextColored(new Vector4(0.75f, 0.5f, 1f, 1f), "⚠  Tag RP retiré");
            ImGui.TextWrapped("Vous n'êtes plus en mode RP. Souhaitez-vous terminer la session ?");
            ImGui.Spacing();
            if (ImGui.SmallButton("Terminer##rptag"))      { _pendingRpTagPrompt = false; EndSession(); }
            ImGui.SameLine();
            if (ImGui.SmallButton("Ignorer##rptag"))         _pendingRpTagPrompt = false;
            ImGui.Unindent(8f);
            ImGui.Spacing();
            var p1 = ImGui.GetCursorScreenPos();
            dl.ChannelsSetCurrent(0); // fond en dessous
            dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(0.75f, 0.5f, 1f, 0.10f)), 4f);
            dl.AddRect(      p0, new Vector2(p0.X + avail, p1.Y), ImGui.GetColorU32(new Vector4(0.75f, 0.5f, 1f, 0.45f)), 4f);
            dl.ChannelsMerge();
            ImGui.Spacing();
        }

        if (!_editing)
        {
            ImGui.Text($"Titre : {_activeSession!.Title}");
            ImGui.Text($"Lieu  : {_activeSession.Location} ({_activeSession.Server})");
            if (!string.IsNullOrEmpty(_activeSession.CharacterName))
                ImGui.Text($"Perso : {_activeSession.CharacterName}");
            if (_activeSession.Ward.HasValue)
            {
                var housing = _activeSession.Plot.HasValue
                    ? $"Quartier {_activeSession.Ward}  —  Parcelle {_activeSession.Plot}"
                    : $"Quartier {_activeSession.Ward}";
                ImGui.TextDisabled($"Log.  : {housing}");
            }
            var livePos = GetCurrentPosition();
            if (livePos.HasValue)
            {
                var mapId  = Plugin.DataManager.GetExcelSheet<TerritoryType>()
                                   ?.GetRowOrDefault(Plugin.ClientState.TerritoryType)
                                   ?.Map.RowId;
                var coords = mapId.HasValue
                    ? MapHelper.WorldToMapCoords(livePos.Value.x, livePos.Value.z, mapId.Value)
                    : null;
                if (coords.HasValue)
                    ImGui.TextDisabled($"Pos   : X {coords.Value.x:F1}   Y {coords.Value.y:F1}");
                else
                    ImGui.TextDisabled($"Pos   : X {livePos.Value.x:F1}   Y {livePos.Value.z:F1}");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (!_busy)
            {
                if (ImGui.Button("Modifier", new Vector2(100, 0)))
                {
                    _editTitle = _activeSession.Title;
                    _editDesc  = string.Empty;
                    _editing   = true;
                }
                ImGui.SameLine();
                if (ImGui.Button("Update de la position", new Vector2(150, 0)))
                    RefreshPosition();
                ImGui.SameLine();
                if (ImGui.Button("Prolonger (+1h)", new Vector2(120, 0)))
                    ExtendSession(1);
                ImGui.SameLine();
                if (ImGui.Button("Voir en ligne", new Vector2(100, 0)))
                    OpenUrl(_config.BaseUrl + "/rp-live");

                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.8f, 0.15f, 0.15f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f,  0.2f,  1));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.7f, 0.1f,  0.1f,  1));
                if (ImGui.Button("Terminer la session", new Vector2(-1, 0)))
                    EndSession();
                ImGui.PopStyleColor(3);
            }
            else ImGui.TextDisabled("Traitement...");
        }
        else
        {
            ImGui.Text("Titre *");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##edittitle", ref _editTitle, 100);

            ImGui.Spacing();
            ImGui.Text("Description");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextMultiline("##editdesc", ref _editDesc, 500, new Vector2(-1, 55));

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var canSave = !_busy && !string.IsNullOrWhiteSpace(_editTitle);
            if (!canSave) ImGui.BeginDisabled();
            if (ImGui.Button("Enregistrer", new Vector2(120, 0)))
                UpdateSession();
            if (!canSave) ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Annuler", new Vector2(80, 0)))
                _editing = false;
        }
    }
}

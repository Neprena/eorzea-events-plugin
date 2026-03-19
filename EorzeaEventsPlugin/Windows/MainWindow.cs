using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.Sheets;
using System.Linq;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class MainWindow : Window
{
    private readonly Configuration _config;

    // ─── Sessions en cours ────────────────────────────────────────────────────

    private List<RpSessionDto> _sessionsList      = [];
    private bool               _sessionsLoading   = false;
    private DateTime           _sessionsLastFetch = DateTime.MinValue;

    // ─── Événements ───────────────────────────────────────────────────────────

    private List<EventDto> _eventsList      = [];
    private bool           _eventsLoading   = false;
    private DateTime       _eventsLastFetch = DateTime.MinValue;

    // ─── Établissements ───────────────────────────────────────────────────────

    private List<EstablishmentDto> _estabList        = [];
    private bool                   _estabLoading     = false;
    private string                 _estabSearchInput = string.Empty;

    // ─────────────────────────────────────────────────────────────────────────

    public MainWindow(Configuration config) : base("Eorzea Events##main")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 460),
            MaximumSize = new Vector2(700, 800),
        };
        _config = config;
    }

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    private void OpenOnMap(RpSessionDto s)
    {
        if (s.TerritoryId is not { } terId || s.MapId is not { } mapId) return;
        if (s.PosX is not { } posX || s.PosZ is not { } posZ) return;

        var coords = MapHelper.WorldToMapCoords(posX, posZ, mapId);
        if (coords == null) return;

        // SeString.CreateMapLink construit le payload natif FFXIV (flag + navigation)
        var seStr   = SeString.CreateMapLink(terId, mapId, coords.Value.x, coords.Value.y);
        var payload = seStr.Payloads.OfType<MapLinkPayload>().FirstOrDefault();
        if (payload == null) return;

        Plugin.GameGui.OpenMapWithMapLink(payload);
    }

    private static readonly Dictionary<string, string> DistrictLabels = new()
    {
        ["brumee"]     = "Brumée",
        ["lavandiere"] = "Lavandière",
        ["coupe"]      = "La Coupe",
        ["shirogane"]  = "Shirogane",
        ["empyree"]    = "Empyrée",
    };

    private static string DistrictLabel(string slug) =>
        DistrictLabels.TryGetValue(slug, out var label) ? label : slug;

    // ─── Draw ─────────────────────────────────────────────────────────────────

    public override void Draw()
    {
        if (!ImGui.BeginTabBar("##maintabs")) return;

        if (ImGui.BeginTabItem("Session RP sauvage"))
        {
            DrawRpSauvageTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Evenements"))
        {
            DrawEventsTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Etablissements"))
        {
            DrawEstabTab();
            ImGui.EndTabItem();
        }

        if (ImGui.TabItemButton("Parametres", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
            Plugin.OpenConfig();

        ImGui.EndTabBar();
    }

    // ─── Tab: Session RP sauvage ──────────────────────────────────────────────

    private string? GetCurrentZoneName()
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var row   = sheet?.GetRowOrDefault(Plugin.ClientState.TerritoryType);
        return row?.PlaceName.Value.Name.ToString();
    }

    private void DrawRpSauvageTab()
    {
        ImGui.Spacing();

        // En-tête avec compteur + actualiser
        if (_sessionsLoading)
        {
            ImGui.TextDisabled("Chargement...");
        }
        else
        {
            var activeCount = _sessionsList.Count(s => s.EndedAt == null);
            ImGui.TextDisabled(activeCount == 0
                ? "Aucune session active en ce moment"
                : $"{activeCount} session(s) en cours");
            ImGui.SameLine();
            if (ImGui.SmallButton("Actualiser##sessions")) FetchSessions();
            ImGui.SameLine();
            if (ImGui.SmallButton("Voir en ligne##sessions"))
                OpenUrl(_config.BaseUrl + "/rp-live");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Liste des sessions (laisse de la place pour le bouton en bas)
        if (!_sessionsLoading)
        {
            var activeSessions = _sessionsList.Where(s => s.EndedAt == null).ToList();
            if (activeSessions.Count == 0)
            {
                ImGui.TextDisabled("Aucune session RP sauvage pour le moment.");
                ImGui.TextDisabled("Soyez le premier a en demarrer une !");
            }
            else
            {
                // Détection des sessions "proches" : même serveur et même zone
                var currentWorld = Plugin.ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString();
                var currentZone  = GetCurrentZoneName();

                List<RpSessionDto> nearby = [];
                List<RpSessionDto> others = [];
                foreach (var s in activeSessions)
                {
                    if (currentWorld != null && currentZone != null
                        && s.Server == currentWorld && s.Location == currentZone)
                        nearby.Add(s);
                    else
                        others.Add(s);
                }

                if (!ImGui.BeginChild("##sessionsscroll", new Vector2(-1, -110), false))
                    goto DrawButton;

                // ── Section "Dans votre zone" ──────────────────────────────────
                if (nearby.Count > 0)
                {
                    ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f),
                        $"✦  Dans votre zone ({currentZone})");
                    ImGui.Spacing();
                    foreach (var s in nearby)
                        DrawSessionEntry(s);

                    if (others.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled("── Autres serveurs ──────────────────────────────────");
                        ImGui.Spacing();
                    }
                }

                // ── Autres sessions ────────────────────────────────────────────
                foreach (var s in others)
                    DrawSessionEntry(s);

                ImGui.EndChild();
            }
        }

        DrawButton:
        // Bouton bas de page
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.HasActiveSession)
        {
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), "Votre session est en cours.");
            ImGui.SameLine();
            if (ImGui.Button("Gerer ma session"))
                Plugin.OpenMySession();
        }
        else
        {
            if (ImGui.Button("Nouvelle session RP sauvage", new Vector2(-1, 0)))
                Plugin.OpenMySession();
        }
    }

    private void DrawSessionEntry(RpSessionDto s)
    {
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), s.Title);
        ImGui.SameLine(0, 8);
        ImGui.TextDisabled($"— {s.Location} ({s.Server})");
        if (!string.IsNullOrEmpty(s.CharacterName))
            ImGui.TextDisabled($"  {s.CharacterName}");
        if (!string.IsNullOrEmpty(s.Description))
            ImGui.TextDisabled($"  {s.Description}");
        if (s.TerritoryId.HasValue && s.MapId.HasValue && s.PosX.HasValue && s.PosZ.HasValue)
        {
            var coords   = MapHelper.WorldToMapCoords(s.PosX.Value, s.PosZ.Value, s.MapId.Value);
            var btnWidth = ImGui.CalcTextSize("Carte").X + ImGui.GetStyle().FramePadding.X * 2;
            var rightX   = ImGui.GetWindowWidth() - btnWidth - ImGui.GetStyle().WindowPadding.X;
            if (coords.HasValue)
            {
                ImGui.TextDisabled($"  X {coords.Value.x:F1}  Y {coords.Value.y:F1}");
                ImGui.SameLine(rightX);
            }
            else
                ImGui.SetCursorPosX(rightX);
            if (ImGui.SmallButton($"Carte##map_{s.Id}"))
                Plugin.Framework.RunOnFrameworkThread(() => OpenOnMap(s));
        }
        if (!Plugin.HasActiveSession && Plugin.MySessionIds.Contains(s.Id))
        {
            if (ImGui.SmallButton($"Reprendre##claim_{s.Id}"))
                Plugin.ClaimSession(s);
        }
        ImGui.Separator();
    }

    // ─── Tab: Événements ──────────────────────────────────────────────────────

    private void DrawEventsTab()
    {
        if (!_eventsLoading && _eventsLastFetch == DateTime.MinValue)
            FetchEvents();

        ImGui.Spacing();
        if (ImGui.Button("Actualiser##events"))
            FetchEvents();
        ImGui.SameLine();
        if (ImGui.SmallButton("Voir en ligne##events"))
            OpenUrl(_config.BaseUrl + "/");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_eventsLoading) { ImGui.TextDisabled("Chargement..."); return; }

        if (_eventsList.Count == 0)
        {
            ImGui.TextDisabled("Aucun evenement dans les 14 prochains jours.");
            return;
        }

        ImGui.TextDisabled($"{_eventsList.Count} evenement(s)");
        ImGui.Spacing();

        if (!ImGui.BeginChild("##eventsscroll", new Vector2(-1, -1), false)) return;
        var now    = DateTime.UtcNow;
        var sorted = _eventsList
            .OrderByDescending(e => IsOngoing(e, now))
            .ThenBy(e => e.StartDate)
            .ToList();

        foreach (var ev in sorted)
        {
            var ongoing = IsOngoing(ev, now);
            if (ongoing)
            {
                ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), "EN COURS");
                ImGui.SameLine(0, 8);
                ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), ev.Title);
            }
            else
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1), ev.Title);

            if (ev.Establishment != null)
            {
                ImGui.SameLine(0, 8);
                ImGui.TextDisabled($"@ {ev.Establishment.Name}");
                if (!string.IsNullOrEmpty(ev.Establishment.Slug))
                {
                    ImGui.SameLine();
                    if (ImGui.SmallButton($"Ouvrir##{ev.Id}"))
                        OpenUrl(_config.BaseUrl + "/etablissements/" + ev.Establishment.Slug);
                }
            }

            var line = new List<string>();
            if (DateTime.TryParse(ev.StartDate, out var start))
            {
                var startStr = start.ToLocalTime().ToString("ddd dd MMM, HH:mm");
                if (!string.IsNullOrEmpty(ev.EndDate) && DateTime.TryParse(ev.EndDate, out var end))
                {
                    var endLocal = end.ToLocalTime();
                    startStr += endLocal.Date == start.ToLocalTime().Date
                        ? " → " + endLocal.ToString("HH:mm")
                        : " → " + endLocal.ToString("ddd dd MMM, HH:mm");
                }
                line.Add(startStr);
            }
            if (ev.IsRecurring) line.Add("recurrent");
            if (line.Count > 0)
                ImGui.TextDisabled("  " + string.Join("  -  ", line));
            if (!string.IsNullOrEmpty(ev.Description))
                ImGui.TextDisabled($"  {ev.Description}");
            ImGui.Separator();
        }
        ImGui.EndChild();
    }

    private static bool IsOngoing(EventDto ev, DateTime utcNow)
    {
        if (!DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            return false;
        if (utcNow < start) return false;
        if (string.IsNullOrEmpty(ev.EndDate)) return false;
        if (!DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
            return false;
        return utcNow <= end;
    }

    private void FetchEvents()
    {
        _eventsLoading = true;
        Task.Run(async () =>
        {
            try   { _eventsList = await Plugin.Api.GetUpcomingEventsAsync(); _eventsLastFetch = DateTime.UtcNow; }
            catch { _eventsList = []; }
            finally { _eventsLoading = false; }
        });
    }

    // ─── Tab: Établissements ──────────────────────────────────────────────────

    private void DrawEstabTab()
    {
        ImGui.Spacing();
        ImGui.SetNextItemWidth(-160);
        var enterPressed = ImGui.InputText("##estabsearch", ref _estabSearchInput, 100, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (ImGui.Button("Rechercher") || enterPressed)
            FetchEstablishments(_estabSearchInput.Trim());
        ImGui.SameLine();
        if (ImGui.SmallButton("Voir en ligne##estab"))
            OpenUrl(_config.BaseUrl + "/etablissements");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_estabLoading) { ImGui.TextDisabled("Chargement..."); return; }

        if (_estabList.Count == 0)
        {
            ImGui.TextDisabled("Recherchez par nom, serveur ou quartier.");
            return;
        }

        ImGui.TextDisabled($"{_estabList.Count} etablissement(s)");
        ImGui.Spacing();

        if (!ImGui.BeginChild("##estabscroll", new Vector2(-1, -1), false)) return;
        foreach (var e in _estabList)
        {
            ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1), e.Name);
            if (!string.IsNullOrEmpty(e.Slug))
            {
                ImGui.SameLine();
                if (ImGui.SmallButton($"Ouvrir##{e.Id}"))
                    OpenUrl(_config.BaseUrl + "/etablissements/" + e.Slug);
            }
            var info = new List<string>();
            if (!string.IsNullOrEmpty(e.Server))   info.Add(e.Server);
            if (!string.IsNullOrEmpty(e.District)) info.Add(DistrictLabel(e.District));
            if (e.Ward.HasValue)                   info.Add($"Quartier {e.Ward}");
            if (e.Plot.HasValue)                   info.Add($"Parcelle {e.Plot}");
            if (info.Count > 0)
                ImGui.TextDisabled("  " + string.Join("  -  ", info));
            ImGui.Separator();
        }
        ImGui.EndChild();
    }

    private void FetchEstablishments(string search)
    {
        _estabLoading = true;
        Task.Run(async () =>
        {
            try   { _estabList = await Plugin.Api.GetEstablishmentsAsync(string.IsNullOrEmpty(search) ? null : search); }
            catch { _estabList = []; }
            finally { _estabLoading = false; }
        });
    }

    /// <summary>Met à jour la liste depuis des données déjà chargées (ex: polling Plugin.cs).</summary>
    public void UpdateSessionsList(List<RpSessionDto> sessions)
    {
        _sessionsList      = sessions;
        _sessionsLastFetch = DateTime.UtcNow;
        _sessionsLoading   = false;
    }

    private void FetchSessions()
    {
        _sessionsLoading = true;
        Task.Run(async () =>
        {
            try   { _sessionsList = await Plugin.Api.GetActiveSessionsAsync(); _sessionsLastFetch = DateTime.UtcNow; }
            catch { _sessionsList = []; }
            finally { _sessionsLoading = false; }
        });
    }
}

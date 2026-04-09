using System.Threading.Tasks;
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

    // ─── Online count ─────────────────────────────────────────────────────────

    private int      _onlineCount      = 0;
    private DateTime _onlineLastFetch  = DateTime.MinValue;

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

        // posX/posZ sont déjà des coordonnées carte (1–42) — pas de conversion supplémentaire
        var seStr   = SeString.CreateMapLink(terId, mapId, posX, posZ);
        var payload = seStr.Payloads.OfType<MapLinkPayload>().FirstOrDefault();
        if (payload == null) return;

        Plugin.GameGui.OpenMapWithMapLink(payload);
    }

    private static string DistrictLabel(string slug)
    {
        var labels = Plugin.L.DistrictLabels;
        return labels.TryGetValue(slug, out var label) ? label : slug;
    }

    // ─── Draw ─────────────────────────────────────────────────────────────────

    public override void Draw()
    {
        var l = Plugin.L;

        if (Plugin.IsBlocked)
        {
            DrawBlockedScreen();
            return;
        }

        // Blocage token invalide
        if (Plugin.Api.HasToken && !Plugin.Api.IsTokenValid)
        {
            DrawTokenInvalidScreen();
            return;
        }

        if (!ImGui.BeginTabBar("##maintabs")) return;

        if (ImGui.BeginTabItem(l.TabRp))
        {
            DrawRpSauvageTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem(l.TabEvents))
        {
            DrawEventsTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem(l.TabEstabs))
        {
            DrawEstabTab();
            ImGui.EndTabItem();
        }

        if (ImGui.TabItemButton(l.TabSettings, ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
            Plugin.OpenConfig();

        ImGui.EndTabBar();

        DrawOnlineFooter();
    }

    private void DrawOnlineFooter()
    {
        // Rafraîchissement toutes les 60s
        if ((DateTime.UtcNow - _onlineLastFetch).TotalSeconds > 60)
        {
            _onlineLastFetch = DateTime.UtcNow;
            _ = Task.Run(async () =>
            {
                _onlineCount = await Plugin.Api.GetOnlineCountAsync();
            });
        }

        if (_onlineCount <= 0) return;

        ImGui.Separator();
        ImGui.Spacing();
        var text = string.Format(Plugin.L.PlayersOnline, _onlineCount);
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize(text).X - ImGui.GetStyle().WindowPadding.X);
        ImGui.TextDisabled(text);
        ImGui.Spacing();
    }

    // ─── Tab: Session RP sauvage ──────────────────────────────────────────────

    private string? GetCurrentZoneName()
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var row   = sheet?.GetRowOrDefault(Plugin.ClientState.TerritoryType);
        return row?.PlaceName.Value.Name.ToString();
    }

    private static void DrawTokenInvalidScreen()
    {
        var l          = Plugin.L;
        var windowSize = ImGui.GetContentRegionAvail();

        ImGui.SetCursorPosY((windowSize.Y - 180f) * 0.5f);

        var icon     = "⚠";
        var iconSize = ImGui.CalcTextSize(icon);
        ImGui.SetCursorPosX((windowSize.X - iconSize.X) * 0.5f);
        ImGui.TextColored(new Vector4(1f, 0.6f, 0.1f, 1f), icon);
        ImGui.Dummy(new Vector2(0, 6));

        var lines = new[] { l.TokenInvalidLine1, l.TokenInvalidLine2, l.TokenInvalidLine3 };
        foreach (var line in lines)
        {
            var sz = ImGui.CalcTextSize(line);
            ImGui.SetCursorPosX(Math.Max(12f, (windowSize.X - sz.X) * 0.5f));
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), line);
        }

        ImGui.Dummy(new Vector2(0, 14));

        var btnWidth = 200f;
        ImGui.SetCursorPosX((windowSize.X - btnWidth) * 0.5f);
        if (ImGui.Button(l.TokenReconfigure, new Vector2(btnWidth, 0)))
            Plugin.OpenSetup(tokenInvalid: true);
    }

    private static void DrawBlockedScreen()
    {
        var l           = Plugin.L;
        var windowSize  = ImGui.GetContentRegionAvail();
        var textPadding = 16f;

        ImGui.SetCursorPosY((windowSize.Y - 200f) * 0.5f);

        ImGui.SetCursorPosX((windowSize.X - 48f) * 0.5f);
        ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "  ⚠");
        ImGui.Dummy(new Vector2(0, 4));

        var lines = Plugin.BlockedMessage.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                ImGui.Dummy(new Vector2(0, 4));
                continue;
            }
            var textSize = ImGui.CalcTextSize(line);
            var textX    = (windowSize.X - textSize.X) * 0.5f;
            ImGui.SetCursorPosX(Math.Max(textPadding, textX));
            ImGui.TextWrapped(line);
        }

        ImGui.Dummy(new Vector2(0, 12));

        var hintSize = ImGui.CalcTextSize(l.BlockedHint);
        ImGui.SetCursorPosX((windowSize.X - hintSize.X) * 0.5f);
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), l.BlockedHint);
    }

    private void DrawRpSauvageTab()
    {
        var l = Plugin.L;
        ImGui.Spacing();

        if (_sessionsLoading)
        {
            ImGui.TextDisabled(l.Loading);
        }
        else
        {
            var activeCount = _sessionsList.Count(s => s.EndedAt == null);
            ImGui.TextDisabled(activeCount == 0
                ? l.RpNoSession
                : string.Format(l.RpSessionsActive, activeCount));
            ImGui.SameLine();
            if (ImGui.SmallButton(l.Refresh + "##sessions")) FetchSessions();
            ImGui.SameLine();
            if (ImGui.SmallButton(l.ViewOnline + "##sessions"))
                OpenUrl(_config.BaseUrl + "/rp-live");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (!_sessionsLoading)
        {
            var activeSessions = _sessionsList.Where(s => s.EndedAt == null).ToList();
            if (activeSessions.Count == 0)
            {
                ImGui.TextDisabled(l.RpNoSession);
                ImGui.TextDisabled(l.RpBeFirst);
            }
            else
            {
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

                if (nearby.Count > 0)
                {
                    ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f),
                        string.Format(l.RpInYourZone, currentZone));
                    ImGui.Spacing();
                    foreach (var s in nearby)
                        DrawSessionEntry(s);

                    if (others.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled(l.RpOtherServers);
                        ImGui.Spacing();
                    }
                }

                foreach (var s in others)
                    DrawSessionEntry(s);

                ImGui.EndChild();
            }
        }

        DrawButton:
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.HasActiveSession)
        {
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), l.RpYourSessionActive);
            ImGui.SameLine();
            if (ImGui.Button(l.RpManageSession))
                Plugin.OpenMySession();
        }
        else
        {
            if (ImGui.Button(l.RpNewSession, new Vector2(-1, 0)))
                Plugin.OpenMySession();
        }
    }

    private void DrawSessionEntry(RpSessionDto s)
    {
        var l = Plugin.L;
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), s.Title);
        ImGui.SameLine(0, 8);
        ImGui.TextDisabled($"— {s.Location} ({s.Server})");
        if (!string.IsNullOrEmpty(s.CharacterName))
            ImGui.TextDisabled($"  {s.CharacterName}");
        if (s.Ward.HasValue)
        {
            var housingInfo = s.Plot.HasValue
                ? string.Format(l.HousingWardPlot, s.Ward, s.Plot)
                : string.Format(l.HousingWard, s.Ward);
            ImGui.TextDisabled($"  {housingInfo}");
        }
        if (!string.IsNullOrEmpty(s.Description))
            ImGui.TextDisabled($"  {s.Description}");
        if (s.TerritoryId.HasValue && s.MapId.HasValue && s.PosX.HasValue && s.PosZ.HasValue)
        {
            var btnWidth = ImGui.CalcTextSize(l.Map).X + ImGui.GetStyle().FramePadding.X * 2;
            var rightX   = ImGui.GetWindowWidth() - btnWidth - ImGui.GetStyle().WindowPadding.X;
            ImGui.TextDisabled($"  X {s.PosX.Value:F1}  Y {s.PosZ.Value:F1}");
            ImGui.SameLine(rightX);
            if (ImGui.SmallButton($"{l.Map}##map_{s.Id}"))
                Plugin.Framework.RunOnFrameworkThread(() => OpenOnMap(s));
        }
        if (!Plugin.HasActiveSession && Plugin.MySessionIds.Contains(s.Id))
        {
            if (ImGui.SmallButton($"{l.RpResume}##claim_{s.Id}"))
                Plugin.ClaimSession(s);
        }
        ImGui.Separator();
    }

    // ─── Tab: Événements ──────────────────────────────────────────────────────

    private void DrawEventsTab()
    {
        var l = Plugin.L;

        if (!_eventsLoading && _eventsLastFetch == DateTime.MinValue)
            FetchEvents();

        ImGui.Spacing();
        if (ImGui.Button(l.Refresh + "##events"))
            FetchEvents();
        ImGui.SameLine();
        if (ImGui.SmallButton(l.ViewOnline + "##events"))
            OpenUrl(_config.BaseUrl + "/");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_eventsLoading) { ImGui.TextDisabled(l.Loading); return; }

        if (_eventsList.Count == 0)
        {
            ImGui.TextDisabled(l.EventsNoEvents);
            return;
        }

        var nowCount     = DateTime.UtcNow;
        var ongoingCount = _eventsList.Count(e => IsOngoing(e, nowCount));
        if (ongoingCount > 0)
        {
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), string.Format(l.EventsOngoing, ongoingCount));
            ImGui.SameLine(0, 8);
            ImGui.TextDisabled(string.Format(l.EventsTotal, _eventsList.Count));
        }
        else
            ImGui.TextDisabled(string.Format(l.EventsCount, _eventsList.Count));
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
                ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), l.Ongoing);
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
                    if (ImGui.SmallButton($"{l.Open}##{ev.Id}"))
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
            if (ev.IsRecurring) line.Add(l.Recurring);
            if (line.Count > 0)
                ImGui.TextDisabled("  " + string.Join("  -  ", line));
            if (!string.IsNullOrEmpty(ev.Description))
            {
                ImGui.SetNextItemOpen(false, ImGuiCond.Once);
                if (ImGui.TreeNode($"  {l.Description}##{ev.Id}"))
                {
                    var clean = StripMarkdown(ev.Description);
                    ImGui.PushTextWrapPos(0);
                    ImGui.TextDisabled(clean);
                    ImGui.PopTextWrapPos();
                    ImGui.TreePop();
                }
            }
            ImGui.Separator();
        }
        ImGui.EndChild();
    }

    private static string StripMarkdown(string text)
    {
        // Supprimer les blocs ***texte***, **texte**, *texte*, ___texte___, __texte__, _texte_
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,3}|_{1,3}", "");
        // Supprimer les shortcodes emoji :nom:
        text = System.Text.RegularExpressions.Regex.Replace(text, @":[\w+\-]+:", "");
        // Supprimer les liens [texte](url)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        // Supprimer les titres # ## ###
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Nettoyer les lignes vides multiples
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
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
        var l = Plugin.L;
        ImGui.Spacing();
        ImGui.SetNextItemWidth(-160);
        var enterPressed = ImGui.InputText("##estabsearch", ref _estabSearchInput, 100, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (ImGui.Button(l.Search) || enterPressed)
            FetchEstablishments(_estabSearchInput.Trim());
        ImGui.SameLine();
        if (ImGui.SmallButton(l.ViewOnline + "##estab"))
            OpenUrl(_config.BaseUrl + "/etablissements");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_estabLoading) { ImGui.TextDisabled(l.Loading); return; }

        if (_estabList.Count == 0)
        {
            ImGui.TextDisabled(l.EstabSearchHint);
            return;
        }

        ImGui.TextDisabled(string.Format(l.EstabCount, _estabList.Count));
        ImGui.Spacing();

        if (!ImGui.BeginChild("##estabscroll", new Vector2(-1, -1), false)) return;
        foreach (var e in _estabList)
        {
            ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1), e.Name);
            if (!string.IsNullOrEmpty(e.Slug))
            {
                ImGui.SameLine();
                if (ImGui.SmallButton($"{l.Open}##{e.Id}"))
                    OpenUrl(_config.BaseUrl + "/etablissements/" + e.Slug);
            }
            var info = new List<string>();
            if (!string.IsNullOrEmpty(e.Server))   info.Add(e.Server);
            if (!string.IsNullOrEmpty(e.District)) info.Add(DistrictLabel(e.District));
            if (e.Ward.HasValue)                   info.Add(string.Format(l.HousingWard, e.Ward));
            if (e.Plot.HasValue)                   info.Add(string.Format("{0} {1}", l.FieldPlot, e.Plot));
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

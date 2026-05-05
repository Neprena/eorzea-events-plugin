using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.Sheets;
using System.Linq;
using System.Numerics;
using System.Globalization;

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

    private List<EstablishmentDto>                              _estabList           = [];
    private bool                                                _estabLoading        = false;
    private bool                                                _estabInitialLoaded  = false;
    private string                                              _estabSearchInput    = string.Empty;
    private readonly Dictionary<string, Task<IDalamudTextureWrap?>> _estabBannerTasks = new();
    private readonly HttpClient                                 _bannerHttp       = new();

    // ─── Online count ─────────────────────────────────────────────────────────

    private int      _onlineCount      = 0;
    private DateTime _onlineLastFetch  = DateTime.MinValue;

#if DEBUG
    private string _debugStatus = string.Empty;
#endif

    // ─────────────────────────────────────────────────────────────────────────

    public MainWindow(Configuration config)
        : base("Eorzea Events##main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 500),
            MaximumSize = new Vector2(900, 900),
        };
        _config = config;
    }

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    private void OpenOnMap(RpSessionDto s)
    {
        if (s.TerritoryId is not { } terId || s.MapId is not { } mapId) return;
        if (s.PosX is not { } posX || s.PosZ is not { } posZ) return;

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

        if (Plugin.Api.HasToken && !Plugin.Api.IsTokenValid)
        {
            DrawTokenInvalidScreen();
            return;
        }

        if (!ImGui.BeginTabBar("##maintabs")) return;

        if (ImGui.BeginTabItem(l.TabRp))
        {
            DrawOpenRpTab();
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
#if DEBUG
        if (ImGui.BeginTabItem(l.TabDebug))
        {
            DrawDebugTab();
            ImGui.EndTabItem();
        }
#endif

        if (ImGui.TabItemButton(l.TabSettings, ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
            Plugin.OpenConfig();

        ImGui.EndTabBar();

        DrawOnlineFooter();
    }

    private void DrawOnlineFooter()
    {
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
        ImGui.TextColored(UiStyle.TextSubtle, text);
        ImGui.Spacing();
    }

    // ─── Tab: RP Ouvert ───────────────────────────────────────────────────────

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
        if (UiPrimitives.ColorButton(l.TokenReconfigure, new Vector2(btnWidth, 0),
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
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
        ImGui.TextColored(UiStyle.TextSubtle, l.BlockedHint);

        if (!string.IsNullOrWhiteSpace(Plugin.BlockedUpdateUrl))
        {
            ImGui.Dummy(new Vector2(0, 14));
            var btnWidth = 220f;
            ImGui.SetCursorPosX((windowSize.X - btnWidth) * 0.5f);
            if (UiPrimitives.ColorButton(l.BlockedOpenPluginPage, new Vector2(btnWidth, 0),
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                OpenUrl(Plugin.BlockedUpdateUrl);
        }
    }

    private void DrawOpenRpTab()
    {
        var l = Plugin.L;
        ImGui.Spacing();

        // Header : compteur chip + label + boutons
        if (!_sessionsLoading)
        {
            var activeCount = _sessionsList.Count(s => s.EndedAt == null);
            if (activeCount > 0)
            {
                UiPrimitives.DrawChip(activeCount.ToString(), UiStyle.ChipBgOpen);
                ImGui.SameLine(0, UiStyle.InlineSpacing);
                ImGui.TextColored(UiStyle.TextSection, l.TabRp);
                ImGui.SameLine(0, UiStyle.InlineSpacing);
            }
            else
            {
                ImGui.TextColored(UiStyle.TextSubtle, l.RpNoSession);
                ImGui.SameLine(0, UiStyle.InlineSpacing);
            }
        }
        else
        {
            ImGui.TextColored(UiStyle.TextSubtle, l.Loading);
            ImGui.SameLine(0, UiStyle.InlineSpacing);
        }

        if (ImGui.Button(l.Refresh + "##sessions", UiStyle.SmallButton)) FetchSessions();
        ImGui.SameLine(0, 4);
        if (ImGui.Button(l.ViewOnline + "##sessions", UiStyle.SmallButton))
            OpenUrl(_config.BaseUrl + "/rp-live");

        if (!_sessionsLoading)
        {
            var activeSessions = _sessionsList.Where(s => s.EndedAt == null).ToList();
            if (activeSessions.Count > 0)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
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
                    ImGui.TextColored(UiStyle.StatusOpen,
                        string.Format(l.RpInYourZone, currentZone));
                    ImGui.Spacing();
                    foreach (var s in nearby)
                        DrawSessionEntry(s);

                    if (others.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextColored(UiStyle.TextSubtle, l.RpOtherServers);
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

        var noSessions = !_sessionsLoading && _sessionsList.All(s => s.EndedAt != null);
        DrawAvailabilitySection(l, noSessions);

        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.HasActiveSession)
        {
            ImGui.TextColored(UiStyle.StatusOpen, l.RpYourSessionActive);
            ImGui.SameLine();
            if (UiPrimitives.ColorButton(l.RpManageSession, UiStyle.PrimaryButton,
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                Plugin.OpenMySession();
        }
        else
        {
            if (UiPrimitives.ColorButton(l.RpNewSession, new Vector2(-1, 0),
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                Plugin.OpenMySession();
        }
    }

    private void DrawAvailabilitySection(Loc l, bool noSessions = false)
    {
        // Bannière de prompt post-connexion
        if (Plugin.LoginPromptPending)
        {
            UiPrimitives.DrawAlert(UiStyle.StatusOpen, l.RpAvailableActiveStatus, l.RpLoginPrompt, () =>
            {
                if (UiPrimitives.ColorButton(l.RpLoginStay + "##stay", Vector2.Zero,
                    UiStyle.SuccessNormal, UiStyle.SuccessHovered, UiStyle.SuccessActive))
                {
                    Plugin.DismissLoginPrompt();
                    Plugin.ActivateRpAvailability();
                }
                ImGui.SameLine(0, 6);
                if (ImGui.Button(l.RpLoginDisable + "##disable", Vector2.Zero))
                {
                    Plugin.DismissLoginPrompt();
                    _ = Task.Run(Plugin.ClearRpAvailabilityAsync);
                }
            });
        }

        // Explication de la fonctionnalité
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(UiStyle.TextSubtle, l.RpAvailableDesc);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();

        // Toggle disponibilité
        var available = Plugin.Config.RpAvailabilityActive;
        if (ImGui.Checkbox(l.RpAvailableEnable + "##rpavailabletoggle", ref available))
        {
            // On est sur le framework thread ici (draw ImGui)
            if (available) Plugin.ActivateRpAvailability();
            else           _ = Task.Run(Plugin.ClearRpAvailabilityAsync);
        }

        ImGui.Spacing();

        // Option "demander à la reconnexion"
        var askOnLogin = Plugin.Config.RpAskOnLogin;
        if (ImGui.Checkbox(l.CfgRpAskOnLogin + "##askonlogin", ref askOnLogin))
        {
            Plugin.Config.RpAskOnLogin = askOnLogin;
            Plugin.Config.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button(l.RpProfileSetup + "##openwizard", Vector2.Zero))
            Plugin.OpenRpProfileWizard();

        if (noSessions)
        {
            ImGui.Spacing();
            ImGui.TextColored(UiStyle.TextSubtle, l.RpNoSession);
            ImGui.TextColored(UiStyle.TextSubtle, l.RpBeFirst);
        }

        ImGui.Spacing();
    }

    private void DrawSessionEntry(RpSessionDto s)
    {
        var l = Plugin.L;

        UiPrimitives.DrawCard(() =>
        {
            // Titre (or chaud)
            ImGui.TextColored(UiStyle.TextTitle, s.Title);

            // Zone • Serveur + bouton carte aligné à droite
            UiPrimitives.DrawIcon("");
            ImGui.SameLine(0, 4);
            ImGui.TextColored(UiStyle.TextMuted, $"{s.Location}  •  {s.Server}");
            if (s.TerritoryId.HasValue && s.MapId.HasValue && s.PosX.HasValue && s.PosZ.HasValue)
            {
                var btnX = ImGui.GetWindowWidth()
                    - ImGui.GetStyle().WindowPadding.X
                    - UiStyle.CardPadH
                    - UiStyle.SmallButton.X;
                ImGui.SameLine(btnX);
                if (UiPrimitives.ColorButton($"{l.Map}##map_{s.Id}", UiStyle.SmallButton,
                    UiStyle.SecondaryNormal, UiStyle.SecondaryHovered, UiStyle.SecondaryActive))
                    Plugin.Framework.RunOnFrameworkThread(() => OpenOnMap(s));
            }

            // Personnage
            if (!string.IsNullOrEmpty(s.CharacterName))
            {
                UiPrimitives.DrawIcon("");
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted, s.CharacterName);
            }

            // Housing
            if (s.Ward.HasValue)
            {
                var housingInfo = s.Room.HasValue
                    ? string.Format(l.HousingWardRoom, s.Ward, s.Room)
                    : s.Plot.HasValue
                        ? string.Format(l.HousingWardPlot, s.Ward, s.Plot)
                        : string.Format(l.HousingWard, s.Ward);
                UiPrimitives.DrawIcon("");
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted, housingInfo);
            }

            // Description
            if (!string.IsNullOrEmpty(s.Description))
            {
                ImGui.PushTextWrapPos(0);
                ImGui.TextColored(UiStyle.TextSubtle, s.Description);
                ImGui.PopTextWrapPos();
            }

            // Bouton "Reprendre" si session orpheline
            if (!Plugin.HasActiveSession && Plugin.MySessionIds.Contains(s.Id))
            {
                ImGui.Spacing();
                if (UiPrimitives.ColorButton($"{l.RpResume}##claim_{s.Id}", UiStyle.PrimaryButton,
                    UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                    Plugin.ClaimSession(s);
            }
        });

        ImGui.Dummy(new Vector2(0, UiStyle.CardSpacing));
    }

    // ─── Tab: Événements ──────────────────────────────────────────────────────

    private void DrawEventsTab()
    {
        var l = Plugin.L;

        if (!_eventsLoading && (_eventsLastFetch == DateTime.MinValue || (DateTime.UtcNow - _eventsLastFetch).TotalMinutes > 5))
            FetchEvents();

        ImGui.Spacing();
        if (ImGui.Button(l.Refresh + "##events", UiStyle.SmallButton))
            FetchEvents();
        ImGui.SameLine();
        if (ImGui.Button(l.ViewOnline + "##events", UiStyle.WideButton))
            OpenUrl(_config.BaseUrl + "/");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextSubtle, l.EventsHideHint);
        ImGui.Spacing();

        if (_eventsLoading) { ImGui.TextColored(UiStyle.TextSubtle, l.Loading); return; }

        var visibleEvents = GetVisibleEvents();

        if (visibleEvents.Count == 0)
        {
            ImGui.TextColored(UiStyle.TextSubtle, l.EventsNoEvents);
            DrawHiddenItemsSummary();
            return;
        }

        var nowCount     = DateTime.UtcNow;
        var ongoingCount = visibleEvents.Count(e => IsOngoing(e, nowCount));
        if (ongoingCount > 0)
        {
            ImGui.TextColored(UiStyle.StatusOpen, string.Format(l.EventsOngoing, ongoingCount));
            ImGui.SameLine(0, 8);
            ImGui.TextColored(UiStyle.TextSubtle, string.Format(l.EventsTotal, visibleEvents.Count));
        }
        else
            ImGui.TextColored(UiStyle.TextSubtle, string.Format(l.EventsCount, visibleEvents.Count));
        ImGui.Spacing();

        var now        = DateTime.UtcNow;
        var soonLimit  = now.AddHours(24);
        var ongoingEvents  = visibleEvents.Where(e => IsOngoing(e, now)).OrderBy(e => e.StartDate).ToList();
        var upcomingEvents = visibleEvents
            .Where(e => !IsOngoing(e, now) && GetStartDate(e) is DateTime start && start <= soonLimit)
            .OrderBy(e => e.StartDate)
            .ToList();
        var laterEvents = visibleEvents.Except(ongoingEvents).Except(upcomingEvents).OrderBy(e => e.StartDate).ToList();

        if (!ImGui.BeginChild("##eventsscroll", new Vector2(-1, -1), false)) return;

        DrawEventGroup(l.Ongoing,   ongoingEvents,  UiStyle.StatusOpen,  UiStyle.ChipBgOpen);
        DrawEventGroup("À venir",   upcomingEvents, UiStyle.StatusSoon,  UiStyle.ChipBgSoon);
        DrawEventGroup("Plus tard", laterEvents,    UiStyle.StatusLater, UiStyle.ChipBgLater);

        DrawHiddenItemsSummary();
        ImGui.EndChild();
    }

    private void DrawEventGroup(string title, List<EventDto> events, Vector4 headerColor, Vector4 chipBg)
    {
        if (events.Count == 0)
            return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(headerColor, title.ToUpper());
        ImGui.SameLine(0, 8);
        UiPrimitives.DrawChip(events.Count.ToString(), chipBg);
        ImGui.Spacing();

        foreach (var ev in events)
            DrawEventEntry(ev, headerColor);

        ImGui.Spacing();
    }

    private void DrawEventEntry(EventDto ev, Vector4 titleColor)
    {
        var l = Plugin.L;

        UiPrimitives.DrawCard(() =>
        {
            // Icône récurrence + titre coloré selon groupe
            if (ev.IsRecurring)
            {
                UiPrimitives.DrawIcon("", titleColor);
                ImGui.SameLine(0, 4);
            }
            // Date au-dessus du titre
            if (DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            {
                var localStart = start.ToLocalTime();
                var dayStr     = localStart.ToString("ddd dd MMM").ToUpper();
                string timeStr;

                if (!string.IsNullOrEmpty(ev.EndDate) && DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
                {
                    var endLocal = end.ToLocalTime();
                    timeStr = endLocal.Date == localStart.Date
                        ? localStart.ToString("HH:mm") + "  →  " + endLocal.ToString("HH:mm")
                        : localStart.ToString("HH:mm") + "  →  " + endLocal.ToString("ddd dd MMM HH:mm").ToUpper();
                }
                else
                {
                    timeStr = localStart.ToString("HH:mm");
                }

                UiPrimitives.DrawChip($"{dayStr}  {timeStr}", UiStyle.ChipBgAccent);
                if (ev.IsRecurring)
                {
                    ImGui.SameLine(0, UiStyle.InlineSpacing);
                    UiPrimitives.DrawChip(l.Recurring, UiStyle.ChipBgOpen);
                }
                ImGui.Spacing();
            }
            else if (ev.IsRecurring)
            {
                UiPrimitives.DrawChip(l.Recurring, UiStyle.ChipBgOpen);
                ImGui.Spacing();
            }

            ImGui.TextColored(titleColor, ev.Title);

            // Établissement + bouton "Plus d'info"
            if (ev.Establishment != null)
            {
                ImGui.TextColored(UiStyle.TextMuted, $"@ {ev.Establishment.Name}");
                var btnX = ImGui.GetContentRegionMax().X - UiStyle.CardPadH - UiStyle.SmallButton.X;
                ImGui.SameLine(btnX);
                if (UiPrimitives.ColorButton($"{l.MoreInfo}##{ev.Id}", UiStyle.SmallButton,
                    UiStyle.SecondaryNormal, UiStyle.SecondaryHovered, UiStyle.SecondaryActive))
                    Plugin.OpenEstabDetail(ev.Establishment);
            }

            // Description collapsible
            if (!string.IsNullOrEmpty(ev.Description))
            {
                ImGui.SetNextItemOpen(false, ImGuiCond.Once);
                if (ImGui.TreeNode($"  {l.Description}##{ev.Id}"))
                {
                    var clean = StripMarkdown(ev.Description);
                    ImGui.PushTextWrapPos(0);
                    ImGui.TextColored(UiStyle.TextSubtle, clean);
                    ImGui.PopTextWrapPos();
                    ImGui.TreePop();
                }
            }
        });

        ImGui.Dummy(new Vector2(0, UiStyle.CardSpacing));
    }

    private static string StripMarkdown(string text)
    {
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,3}|_{1,3}", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @":[\w+\-]+:", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    private static bool IsOngoing(EventDto ev, DateTime utcNow)
    {
        if (!DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            return false;
        if (utcNow < start) return false;
        DateTime end;
        if (string.IsNullOrEmpty(ev.EndDate) || !DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out end))
            end = start.AddHours(3);
        return utcNow <= end;
    }

    private static bool IsExpired(EventDto ev, DateTime utcNow)
    {
        if (!DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            return false;
        if (string.IsNullOrEmpty(ev.EndDate) || !DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
            end = start.AddHours(3);
        return end < utcNow;
    }

    private static DateTime? GetStartDate(EventDto ev)
    {
        if (!DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            return null;
        return start;
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

        // Chargement initial silencieux à la première ouverture de l'onglet
        if (!_estabInitialLoaded && !_estabLoading)
        {
            _estabInitialLoaded = true;
            FetchEstablishments(string.Empty);
        }

        ImGui.Spacing();
        ImGui.SetNextItemWidth(-(UiStyle.SmallButton.X + UiStyle.WideButton.X + 12f));
        var enterPressed = ImGui.InputText("##estabsearch", ref _estabSearchInput, 100, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (ImGui.Button(l.Search, UiStyle.SmallButton) || enterPressed)
            FetchEstablishments(_estabSearchInput.Trim());
        ImGui.SameLine();
        if (ImGui.Button(l.ViewOnline + "##estab", UiStyle.WideButton))
            OpenUrl(_config.BaseUrl + "/etablissements");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_estabLoading) { ImGui.TextColored(UiStyle.TextSubtle, l.Loading); return; }

        var visibleEstabs = GetVisibleEstablishments();

        if (visibleEstabs.Count == 0)
        {
            ImGui.TextColored(UiStyle.TextSubtle,
                _config.HiddenEstablishmentIds.Count > 0 ? l.EstabNoResults : l.EstabSearchHint);
            DrawHiddenEstablishmentsSection();
            return;
        }

        ImGui.TextColored(UiStyle.TextSubtle, string.Format(l.EstabCount, visibleEstabs.Count));
        ImGui.Spacing();

        if (!ImGui.BeginChild("##estabscroll", new Vector2(-1, -1), false)) return;

        string? toHide = null;
        foreach (var e in visibleEstabs)
        {
            bool hideThis = false;
            UiPrimitives.DrawCardWithBanner(GetBannerWrap(e.Banner), () =>
            {
                // Nom
                ImGui.TextColored(UiStyle.TextTitle, e.Name);

                // Chips de localisation
                var hasLocation = !string.IsNullOrEmpty(e.Server)
                               || !string.IsNullOrEmpty(e.District)
                               || e.Ward.HasValue
                               || e.Plot.HasValue;
                if (hasLocation)
                {
                    if (!string.IsNullOrEmpty(e.Server))
                    {
                        UiPrimitives.DrawChip(e.Server);
                        ImGui.SameLine(0, 4);
                    }
                    if (!string.IsNullOrEmpty(e.District))
                    {
                        UiPrimitives.DrawChip(DistrictLabel(e.District));
                        ImGui.SameLine(0, 4);
                    }
                    if (e.Ward.HasValue)
                    {
                        UiPrimitives.DrawChip(string.Format(l.HousingWard, e.Ward));
                        ImGui.SameLine(0, 4);
                    }
                    if (e.Plot.HasValue)
                        UiPrimitives.DrawChip(string.Format("{0} {1}", l.FieldPlot, e.Plot));
                }

                // Boutons d'action
                ImGui.Spacing();
                if (UiPrimitives.ColorButton($"{l.EstabDetail}##detail_{e.Id}", UiStyle.SmallButton,
                    UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                    Plugin.OpenEstabDetail(e);
                ImGui.SameLine(0, 4);
                if (UiPrimitives.ColorButton($"{l.Hide}##hide_est_{e.Id}", UiStyle.SmallButton,
                    UiStyle.SecondaryNormal, UiStyle.SecondaryHovered, UiStyle.SecondaryActive))
                    hideThis = true;
            });

            ImGui.Dummy(new Vector2(0, UiStyle.CardSpacing));

            if (hideThis) { toHide = e.Id; break; }
        }
        if (toHide != null) HideEstablishment(toHide);

        DrawHiddenEstablishmentsSection();
        ImGui.EndChild();
    }

    private List<EventDto> GetVisibleEvents()
    {
        var now = DateTime.UtcNow;
        return _eventsList
            .Where(e => !e.IsOfficial)
            .Where(e => !IsExpired(e, now))
            .Where(e => !_config.HiddenEventIds.Contains(e.Id))
            .Where(e => string.IsNullOrEmpty(e.Establishment?.Id) || !_config.HiddenEstablishmentIds.Contains(e.Establishment.Id))
            .ToList();
    }

    private List<EstablishmentDto> GetVisibleEstablishments()
    {
        return _estabList
            .Where(e => !_config.HiddenEstablishmentIds.Contains(e.Id))
            .ToList();
    }

    private void HideEvent(string eventId)
    {
        if (_config.HiddenEventIds.Contains(eventId))
            return;
        _config.HiddenEventIds.Add(eventId);
        _config.Save();
    }

    private void HideEstablishment(string establishmentId)
    {
        if (_config.HiddenEstablishmentIds.Contains(establishmentId))
            return;
        _config.HiddenEstablishmentIds.Add(establishmentId);
        _config.Save();
    }

    private void ShowEvent(string eventId)
    {
        if (_config.HiddenEventIds.Remove(eventId))
            _config.Save();
    }

    private void ShowEstablishment(string establishmentId)
    {
        if (_config.HiddenEstablishmentIds.Remove(establishmentId))
            _config.Save();
    }

    private void DrawHiddenItemsSummary()
    {
        var hiddenEvents = _eventsList.Where(e => _config.HiddenEventIds.Contains(e.Id)).OrderBy(e => e.StartDate).ToList();
        var hiddenEstabs = GetKnownHiddenEstablishments();

        if (hiddenEvents.Count == 0 && hiddenEstabs.Count == 0)
            return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextSubtle, $"{Plugin.L.Hide}: {hiddenEvents.Count} events, {hiddenEstabs.Count} lieux");

        foreach (var ev in hiddenEvents)
        {
            ImGui.TextColored(UiStyle.TextSubtle, $"  {ev.Title}");
            ImGui.SameLine();
            if (ImGui.Button($"{Plugin.L.Show}##show_event_{ev.Id}", UiStyle.SmallButton))
                ShowEvent(ev.Id);
        }

        foreach (var est in hiddenEstabs)
        {
            ImGui.TextColored(UiStyle.TextSubtle, $"  {est.Name}");
            ImGui.SameLine();
            if (ImGui.Button($"{Plugin.L.Show}##show_est_from_events_{est.Id}", UiStyle.SmallButton))
                ShowEstablishment(est.Id);
        }
    }

    private void DrawHiddenEstablishmentsSection()
    {
        var hiddenEstabs = GetKnownHiddenEstablishments();
        if (hiddenEstabs.Count == 0)
            return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextSubtle, $"{Plugin.L.Hide}: {hiddenEstabs.Count} lieu(x)");
        foreach (var est in hiddenEstabs)
        {
            ImGui.TextColored(UiStyle.TextSubtle, $"  {est.Name}");
            ImGui.SameLine();
            if (ImGui.Button($"{Plugin.L.Show}##show_est_{est.Id}", UiStyle.SmallButton))
                ShowEstablishment(est.Id);
        }
    }

    private List<EstablishmentSummaryDto> GetKnownHiddenEstablishments()
    {
        var known = new Dictionary<string, EstablishmentSummaryDto>();

        foreach (var est in _estabList)
            known[est.Id] = new EstablishmentSummaryDto { Id = est.Id, Name = est.Name, Slug = est.Slug };

        foreach (var ev in _eventsList)
        {
            if (!string.IsNullOrEmpty(ev.Establishment?.Id))
                known[ev.Establishment.Id] = new EstablishmentSummaryDto
                {
                    Id = ev.Establishment.Id,
                    Name = ev.Establishment.Name,
                    Slug = ev.Establishment.Slug,
                };
        }

        return _config.HiddenEstablishmentIds
            .Select(id => known.TryGetValue(id, out var est)
                ? est
                : new EstablishmentSummaryDto { Id = id, Name = id })
            .OrderBy(e => e.Name)
            .ToList();
    }

    private IDalamudTextureWrap? GetBannerWrap(string? bannerUrl)
    {
        if (string.IsNullOrEmpty(bannerUrl)) return null;
        if (!_estabBannerTasks.TryGetValue(bannerUrl, out var task))
        {
            task = FetchBannerTextureAsync(bannerUrl);
            _estabBannerTasks[bannerUrl] = task;
        }
        return task.IsCompletedSuccessfully ? task.Result : null;
    }

    private async Task<IDalamudTextureWrap?> FetchBannerTextureAsync(string url)
    {
        try
        {
            var bytes = await _bannerHttp.GetByteArrayAsync(url);
            return await Plugin.TextureProvider.CreateFromImageAsync(
                new ReadOnlyMemory<byte>(bytes), null, default);
        }
        catch { return null; }
    }

    private void DisposeBannerCache()
    {
        foreach (var (_, task) in _estabBannerTasks)
            if (task.IsCompletedSuccessfully) task.Result?.Dispose();
        _estabBannerTasks.Clear();
    }

    private void FetchEstablishments(string search)
    {
        _estabLoading = true;
        DisposeBannerCache();
        Task.Run(async () =>
        {
            try
            {
                var list = await Plugin.Api.GetEstablishmentsAsync(string.IsNullOrEmpty(search) ? null : search);
                var rng  = new Random();
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                _estabList = list;
            }
            catch { _estabList = []; }
            finally { _estabLoading = false; }
        });
    }

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

#if DEBUG
    private void DrawDebugTab()
    {
        var l = Plugin.L;
        var snapshot = LocationDebugSnapshot.Collect();

        ImGui.Spacing();
        if (ImGui.Button(l.DebugCopy, UiStyle.WideButton))
        {
            ImGui.SetClipboardText(snapshot.ToDebugDump());
            _debugStatus = l.DebugCopied;
        }
        if (!string.IsNullOrEmpty(_debugStatus))
        {
            ImGui.SameLine();
            ImGui.TextColored(UiStyle.TextSubtle, _debugStatus);
        }

        ImGui.SameLine(0, 8);
        if (ImGui.Button("Preview wizard profil RP", UiStyle.WideButton))
            Plugin.OpenRpProfileWizard();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (!ImGui.BeginChild("##debugscroll", new Vector2(-1, -1), false))
            return;

        DrawDebugSection(l.DebugSectionPlayer, new (string, string)[]
        {
            ("Character", snapshot.CharacterName),
            ("World", snapshot.WorldName),
        });

        DrawDebugSection(l.DebugSectionTerritory, new (string, string)[]
        {
            ("TerritoryId", snapshot.TerritoryId.ToString(CultureInfo.InvariantCulture)),
            ("TerritoryName", snapshot.TerritoryName),
            ("MapId", snapshot.MapId.ToString(CultureInfo.InvariantCulture)),
            ("MapRowId", FormatNullable(snapshot.MapRowId)),
            ("MapPlaceNameRowId", FormatNullable(snapshot.PlaceNameRowId)),
            ("MapPlaceName", snapshot.PlaceName),
            ("MapSizeFactor", FormatNullable(snapshot.SizeFactor)),
            ("MapOffsetX", FormatNullable(snapshot.OffsetX)),
            ("MapOffsetY", FormatNullable(snapshot.OffsetY)),
            ("OriginalHouseTerritoryTypeId", FormatNullable(snapshot.OriginalHouseTerritoryTypeId)),
        });

        DrawDebugSection(l.DebugSectionWorldPos, new (string, string)[]
        {
            ("WorldPosition", FormatVector3(snapshot.WorldPosition)),
        });

        DrawDebugSection(l.DebugSectionMapPos, new (string, string)[]
        {
            ("MapUtil.GetMapCoordinates", FormatVector3(snapshot.DisplayMapPosition)),
            ("FallbackMapHelper", FormatMap2(snapshot.FallbackMapPosition)),
        });

        DrawDebugSection(l.DebugSectionHousing, new (string, string)[]
        {
            ("HasHousingManager", snapshot.HasHousingManager.ToString()),
            ("RawWard", FormatNullable(snapshot.RawWard)),
            ("RawPlot", FormatNullable(snapshot.RawPlot)),
            ("RawRoom", FormatNullable(snapshot.RawRoom)),
            ("Ward", FormatNullable(snapshot.Ward)),
            ("Plot", FormatNullable(snapshot.Plot)),
            ("Room", FormatNullable(snapshot.Room)),
        });

        DrawDebugSection(l.DebugSectionDerived, new (string, string)[]
        {
            ("Wing", FormatNullable(snapshot.Wing)),
            ("HasHousingContext", snapshot.HasHousingContext.ToString()),
            ("HasPlot", snapshot.HasPlot.ToString()),
            ("HasRoom", snapshot.HasRoom.ToString()),
            ("HousingGuess", snapshot.HousingGuess),
            ("HousingSummary", snapshot.HousingSummary),
        });

        ImGui.EndChild();
    }

    private void DrawDebugSection(string title, IReadOnlyList<(string key, string value)> rows)
    {
        if (!ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen))
            return;

        foreach (var (key, value) in rows)
            ImGui.TextColored(UiStyle.TextSubtle, $"{key}: {value}");

        ImGui.Spacing();
    }

    private static string FormatNullable<T>(T? value) where T : struct
        => value.HasValue ? Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? Plugin.L.DebugUnavailable : Plugin.L.DebugUnavailable;

    private static string FormatVector3(Vector3? value)
        => value.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "X={0:F2}, Y={1:F2}, Z={2:F2}", value.Value.X, value.Value.Y, value.Value.Z)
            : Plugin.L.DebugUnavailable;

    private static string FormatMap2((float x, float y)? value)
        => value.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "X={0:F2}, Y={1:F2}", value.Value.x, value.Value.y)
            : Plugin.L.DebugUnavailable;
#endif
}

using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Pages;
using EorzeaEventsPlugin.Ui.Shell;
using EorzeaEventsPlugin.Api;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.Sheets;
using System.Linq;
using System.Numerics;
using System.Globalization;

namespace EorzeaEventsPlugin.Windows;

public class MainWindow : ThemedWindow, IDisposable
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

    /// <summary>Filtres de l'agenda, appliqués côté client : l'API ne pagine pas.</summary>
    private string      _eventsQuery  = string.Empty;
    private EventOrigin _eventsOrigin = EventOrigin.All;

    private enum EventOrigin { All, Official, Community }

    // ─── Établissements ───────────────────────────────────────────────────────

    private List<EstablishmentDto>                              _estabList           = [];
    private bool                                                _estabLoading        = false;
    private bool                                                _estabInitialLoaded  = false;
    private string                                              _estabSearchInput    = string.Empty;

#if DEBUG
    private string _debugStatus = string.Empty;
#endif

    // ─────────────────────────────────────────────────────────────────────────

    private readonly AppShell     _shell;
    private readonly SettingsPage  _settings;
    private readonly RpProfilePage _rpProfile;
    private readonly AroundPage    _around = new();
    private readonly FriendsPage   _friends = new();

    // Le nom de la fenêtre est la clé de persistance de imgui.ini : le changer
    // réinitialiserait position et taille chez tous les utilisateurs.
    public MainWindow(Configuration config)
        : base("Eorzea Events##main",
               ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        ShowCloseButton = false; // la barre de titre maison porte la fermeture
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(780, 560),
            MaximumSize = new Vector2(1100, 1000),
        };
        _config   = config;
        _settings = new SettingsPage(config);
        _rpProfile = new RpProfilePage(config);

        _shell = new AppShell(
        [
            new ShellPage
            {
                Id    = "rp",
                Icon  = Icons.RpLive,
                Label = () => Plugin.L.TabRp,
                Draw  = DrawOpenRpTab,
                // Les sessions terminées restent dans la liste jusqu'au
                // rafraîchissement suivant : les compter laissait une pastille
                // sur une page vide.
                Badge = () => _sessionsList.Count(s => s.EndedAt == null),
            },
            // Sa propre fiche vient tôt : c'est la page qu'on ouvre le plus
            // souvent après « RP ouvert », et celle qui porte les réglages de
            // confidentialité.
            new ShellPage
            {
                Id    = "profile",
                Icon  = Icons.Profile,
                Label = () => Plugin.L.RpProfileTitle,
                Draw  = _rpProfile.Draw,
            },
            new ShellPage
            {
                Id    = "around",
                Icon  = Icons.Around,
                Label = () => Plugin.L.TabAround,
                Draw  = _around.Draw,
                Badge = () => Plugin.AvailableEntries.Count,
            },
            new ShellPage
            {
                Id    = "friends",
                Icon  = Icons.Friend,
                Label = () => Plugin.L.TabFriends,
                Draw  = _friends.Draw,
                Badge = () => Plugin.Friends.Count,
            },
            new ShellPage
            {
                Id    = "events",
                Icon  = Icons.Events,
                Label = () => Plugin.L.TabEvents,
                Draw  = DrawEventsTab,
            },
            new ShellPage
            {
                Id    = "venues",
                Icon  = Icons.Venues,
                Label = () => Plugin.L.TabEstabs,
                Draw  = DrawEstabTab,
            },
#if DEBUG
            new ShellPage
            {
                Id     = "debug",
                Icon   = Icons.Debug,
                Label  = () => Plugin.L.TabDebug,
                Draw   = DrawDebugTab,
                Pinned = true,
            },
#endif
            new ShellPage
            {
                Id     = "settings",
                Icon   = Icons.Settings,
                Label  = () => Plugin.L.TabSettings,
                Draw   = _settings.Draw,
                Pinned = true,
            },
        ], initialId: "rp");
    }

    /// <summary>
    /// Les images sont désormais détenues par <see cref="Textures"/>, libéré au
    /// déchargement du plugin : cette fenêtre n'a plus rien à libérer.
    /// </summary>
    public void Dispose() => GC.SuppressFinalize(this);

    /// <summary>Ouvre la fenêtre sur une page donnée.</summary>
    public void OpenAt(string pageId)
    {
        _shell.Navigate(pageId);
        IsOpen = true;
    }

    /// <summary>La coque peint elle-même les bords, sans marge de fenêtre.</summary>
    protected override bool Chromeless => true;

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

    /// <summary>
    /// Adresse compacte : quartier et parcelle pour une maison, quartier et
    /// numéro pour un appartement.
    /// </summary>
    private static string FormatAddress(EstablishmentDto e, Loc l)
    {
        var ward = e.Ward.HasValue ? string.Format(l.HousingWard, e.Ward) : string.Empty;

        if (e.ApartmentNumber.HasValue)
            return $"{ward}  ·  {l.EstabApartment} {e.ApartmentNumber}".TrimStart(' ', '·');

        if (e.Plot.HasValue)
            return $"{ward}  ·  {l.FieldPlot} {e.Plot}".TrimStart(' ', '·');

        return ward.Length > 0 ? ward : l.EstabApartment;
    }

    // ─── Draw ─────────────────────────────────────────────────────────────────

    public override void Draw()
    {
        // Les écrans bloquants court-circuitent la navigation, mais conservent
        // la barre de titre : sans elle, la fenêtre ne serait ni déplaçable ni
        // refermable.
        System.Action? fullScreen =
            Plugin.IsBlocked                                ? DrawBlockedScreen
          : Plugin.Api.HasToken && !Plugin.Api.IsTokenValid ? DrawTokenInvalidScreen
          : null;

        _shell.Draw(out var close, fullScreen);
        if (close) IsOpen = false;
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

                var bottomH = 10f * ImGui.GetFrameHeightWithSpacing();
                if (!ImGui.BeginChild("##sessionsscroll", new Vector2(-1, -bottomH), false))
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
                    Plugin.SetRpAvailability(true);
                }
                ImGui.SameLine(0, 6);
                if (ImGui.Button(l.RpLoginDisable + "##disable", Vector2.Zero))
                {
                    Plugin.DismissLoginPrompt();
                    Plugin.SetRpAvailability(false);
                }
            });
        }

        // Explication de la fonctionnalité
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(UiStyle.TextSubtle, l.RpAvailableDesc);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();

        // Toggle disponibilité — désactivé si le perso actuel n'est pas lié
        var player     = Plugin.ObjectTable.LocalPlayer;
        var charLinked = player != null
            && Plugin.Config.FindCharacterToken(player.Name.TextValue, (int)player.HomeWorld.RowId) != null;

        if (!charLinked)
        {
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), l.RpAvailableNoToken);
            ImGui.Spacing();
            if (ImGui.Button("Lier ce personnage##linkfromrp", Vector2.Zero))
                Plugin.OpenSetup(migration: Plugin.Config.CharacterTokens.Count == 0
                    && !string.IsNullOrWhiteSpace(Plugin.Config.ApiToken));
        }
        else
        {
            var available = Plugin.CurrentCharacterAvailable;
            if (ImGui.Checkbox(l.RpAvailableEnable + "##rpavailabletoggle", ref available))
                Plugin.SetRpAvailability(available);
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
            ImGui.TextColored(UiStyle.TextTitle, Glyphs.Safe(s.Title));

            // Zone • Serveur + bouton carte aligné à droite
            UiPrimitives.DrawIcon(Icons.Location);
            ImGui.SameLine(0, 4);
            ImGui.TextColored(UiStyle.TextMuted, Glyphs.Safe($"{s.Location}  •  {s.Server}"));
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
                UiPrimitives.DrawIcon(Icons.Character);
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted, Glyphs.Safe(s.CharacterName));
            }

            // Housing
            if (s.Ward.HasValue)
            {
                var housingInfo = s.Room.HasValue
                    ? string.Format(l.HousingWardRoom, s.Ward, s.Room)
                    : s.Plot.HasValue
                        ? string.Format(l.HousingWardPlot, s.Ward, s.Plot)
                        : string.Format(l.HousingWard, s.Ward);
                UiPrimitives.DrawIcon(Icons.Housing);
                ImGui.SameLine(0, 4);
                ImGui.TextColored(UiStyle.TextMuted, housingInfo);
            }

            // Description
            if (!string.IsNullOrEmpty(s.Description))
            {
                ImGui.PushTextWrapPos(0);
                ImGui.TextColored(UiStyle.TextSubtle, Glyphs.Safe(s.Description));
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
    }

    // ─── Tab: Événements ──────────────────────────────────────────────────────

    private void DrawEventsTab()
    {
        var l = Plugin.L;

        if (!_eventsLoading && (_eventsLastFetch == DateTime.MinValue || (DateTime.UtcNow - _eventsLastFetch).TotalMinutes > 5))
            FetchEvents();

        DrawEventsToolbar(l);

        if (_eventsLoading) { Feedback.SkeletonCards(3); return; }

        var visibleEvents = GetVisibleEvents();

        if (visibleEvents.Count == 0)
        {
            Feedback.EmptyState(Icons.Events, l.EventsNoEvents, l.EventsHideHint,
                                l.ViewOnline, () => OpenUrl(_config.BaseUrl + "/"));
            DrawHiddenItemsSummary();
            return;
        }

        var matching = FilterEvents(visibleEvents);

        if (matching.Count == 0)
        {
            Feedback.EmptyState(Icons.Search, l.EventsNoMatch, null,
                                l.EventsClearFilter, () =>
                                {
                                    _eventsQuery  = string.Empty;
                                    _eventsOrigin = EventOrigin.All;
                                });
            return;
        }

        if (!ImGui.BeginChild("##eventsscroll", new Vector2(-1, -1), false)) return;

        var now = DateTime.UtcNow;

        // Les événements déjà commencés n'ont pas leur place dans le jour où ils
        // ont débuté : ce qui compte est qu'on peut les rejoindre maintenant.
        var ongoing = matching.Where(e => IsOngoing(e, now)).OrderBy(e => e.StartDate).ToList();
        DrawEventGroup(l.Ongoing, ongoing, UiStyle.StatusOpen, Icons.RpLive);

        var byDay = matching
            .Where(e => !IsOngoing(e, now))
            .Select(e => (Event: e, Start: GetStartDate(e)))
            .Where(x => x.Start.HasValue)
            .GroupBy(x => x.Start!.Value.ToLocalTime().Date)
            .OrderBy(g => g.Key);

        foreach (var day in byDay)
        {
            DrawEventGroup(DayLabel(day.Key, l),
                           day.OrderBy(x => x.Start).Select(x => x.Event).ToList(),
                           DayTone(day.Key), Icons.Events);
        }

        DrawHiddenItemsSummary();
        ImGui.EndChild();
    }

    /// <summary>
    /// Barre de l'agenda : recherche, provenance et actions. Les filtres portent
    /// sur la liste déjà chargée, aucun appel réseau n'en découle.
    /// </summary>
    private void DrawEventsToolbar(Loc l)
    {
        var refreshWidth = Btn.Measure(l.Refresh, Icons.Refresh);
        var onlineWidth  = Btn.Measure(l.ViewOnline, Icons.External);
        var actionsWidth = refreshWidth + onlineWidth + Theme.S(Theme.GapS);

        Inputs.SearchBar("##eventsearch", ref _eventsQuery, l.EventsSearchHint,
                         ImGui.GetContentRegionAvail().X - Card.RightInset
                         - actionsWidth - Theme.S(Theme.GapM));

        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        if (Btn.Draw(l.Refresh, BtnTone.Secondary, BtnSize.Medium, Icons.Refresh, id: "ev_refresh"))
            FetchEvents();

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.ViewOnline, BtnTone.Ghost, BtnSize.Medium, Icons.External, id: "ev_online"))
            OpenUrl(_config.BaseUrl + "/");

        Layout.Spacer(Theme.GapS);

        OriginFilter(l.EventsFilterAll,  EventOrigin.All);
        ImGui.SameLine(0f, Theme.S(Theme.GapXs));
        OriginFilter(l.EventsOfficial,   EventOrigin.Official);
        ImGui.SameLine(0f, Theme.S(Theme.GapXs));
        OriginFilter(l.EventsCommunity,  EventOrigin.Community);

        DrawEventsCounter(l);
        Layout.Divider(Theme.GapS);

        void OriginFilter(string label, EventOrigin origin)
        {
            var active = _eventsOrigin == origin;
            if (Btn.Draw(label, active ? BtnTone.Primary : BtnTone.Ghost, BtnSize.Small,
                         id: $"ev_origin_{origin}"))
                _eventsOrigin = origin;
        }
    }

    /// <summary>
    /// Décompte aligné à droite de la barre de filtres. Il porte sur la liste
    /// visible complète, pas sur le résultat filtré : c'est un repère stable.
    /// </summary>
    private void DrawEventsCounter(Loc l)
    {
        var total   = GetVisibleEvents();
        var ongoing = total.Count(e => IsOngoing(e, DateTime.UtcNow) && !e.Cancelled);

        var text = ongoing > 0
            ? $"{string.Format(l.EventsOngoing, ongoing)}  {string.Format(l.EventsTotal, total.Count)}"
            : string.Format(l.EventsCount, total.Count);

        using var font = Fonts.PushSmall();

        var width = ImGui.CalcTextSize(text).X;
        if (ImGui.GetContentRegionAvail().X - Card.RightInset <= width) return;

        ImGui.SameLine();
        Layout.RightAlign(width);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(Theme.TextFaint, text);
    }

    /// <summary>Applique recherche et provenance à la liste déjà visible.</summary>
    private List<EventDto> FilterEvents(List<EventDto> events)
    {
        var query = _eventsQuery.Trim();

        return events.Where(e =>
        {
            if (_eventsOrigin == EventOrigin.Official  && !e.IsOfficial) return false;
            if (_eventsOrigin == EventOrigin.Community &&  e.IsOfficial) return false;

            if (query.Length == 0) return true;

            return Contains(e.Title) || Contains(e.Description) || Contains(e.Establishment?.Name);
        }).ToList();

        bool Contains(string? haystack) =>
            haystack != null && haystack.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>« Aujourd'hui », « Demain », puis la date en toutes lettres.</summary>
    private static string DayLabel(DateTime day, Loc l)
    {
        var today = DateTime.Now.Date;

        if (day == today)             return l.EventsToday;
        if (day == today.AddDays(1))  return l.EventsTomorrow;

        var label = day.ToString("dddd d MMMM", l.Culture);
        return char.ToUpper(label[0], l.Culture) + label[1..];
    }

    /// <summary>Les deux jours à venir se démarquent du reste de la semaine.</summary>
    private static Vector4 DayTone(DateTime day)
    {
        var today = DateTime.Now.Date;
        return day <= today.AddDays(1) ? UiStyle.StatusSoon : UiStyle.StatusLater;
    }

    private void DrawEventGroup(string title, List<EventDto> events, Vector4 headerColor,
                                FontAwesomeIcon icon)
    {
        if (events.Count == 0)
            return;

        Layout.Spacer(Theme.GapL);
        Layout.SectionHeader(title, icon, events.Count, headerColor);

        foreach (var ev in events)
            DrawEventEntry(ev, headerColor);
    }

    private void DrawEventEntry(EventDto ev, Vector4 titleColor)
    {
        var l = Plugin.L;

        DrawEventCard(ev, () =>
        {
            // Horaire, au-dessus du titre. Le jour est porté par l'en-tête de
            // groupe de l'agenda : le répéter sur chaque carte serait du bruit.
            if (DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            {
                var localStart = start.ToLocalTime();
                string timeStr;

                if (!string.IsNullOrEmpty(ev.EndDate) && DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
                {
                    var endLocal = end.ToLocalTime();
                    timeStr = endLocal.Date == localStart.Date
                        ? localStart.ToString("HH:mm") + "  →  " + endLocal.ToString("HH:mm")
                        : localStart.ToString("HH:mm") + "  →  " + endLocal.ToString("ddd d MMM HH:mm", l.Culture);
                }
                else
                {
                    timeStr = localStart.ToString("HH:mm");
                }

                Chip.Draw(timeStr, ChipTone.Accent, Icons.Clock);

                if (ev.IsOfficial)
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(l.EventsOfficial, ChipTone.Gold, Icons.Sparkle);
                }

                if (ev.Cancelled)
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(l.EventCancelled, ChipTone.Danger);
                }
                else if (ev.IsRecurring)
                {
                    // La règle iCalendar dit « chaque mercredi » là où le
                    // libellé générique ne disait que « récurrent ».
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(Recurrence.Describe(ev.RecurrenceRule, l.Recurring),
                              ChipTone.Success, Icons.Recurring);
                }
                ImGui.Spacing();
            }
            else if (ev.Cancelled)
            {
                Chip.Draw(l.EventCancelled, ChipTone.Danger);
                ImGui.Spacing();
            }
            else if (ev.IsRecurring)
            {
                Chip.Draw(Recurrence.Describe(ev.RecurrenceRule, l.Recurring),
                          ChipTone.Success, Icons.Recurring);
                ImGui.Spacing();
            }

            // Titre de l'événement : niveau de titre, pas du corps de texte.
            var displayColor = ev.Cancelled ? UiStyle.TextSubtle : titleColor;
            using (Fonts.PushH2())
            {
                if (ev.Cancelled) Text.Strikethrough(ev.Title, displayColor);
                else              ImGui.TextColored(displayColor, Glyphs.Safe(ev.Title));
            }

            // Résumé de la description, visible sans avoir à déplier.
            if (!string.IsNullOrWhiteSpace(ev.Description))
            {
                Layout.Spacer(Theme.GapXs);
                using (Fonts.PushBody())
                {
                    ImGui.PushTextWrapPos(0f);
                    ImGui.TextColored(Theme.TextMuted, Glyphs.Summarize(ev.Description, 150));
                    ImGui.PopTextWrapPos();
                }
            }

            // Lieu et localisation.
            if (ev.Establishment is { } venue)
            {
                Layout.Spacer(Theme.GapS);

                Chip.Draw(venue.Name, ChipTone.Neutral, Icons.Venues);

                if (!string.IsNullOrEmpty(venue.District))
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(DistrictLabel(venue.District), ChipTone.Neutral, Icons.Location);
                }
                if (venue.Ward.HasValue)
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(venue.Plot.HasValue
                                  ? string.Format(l.HousingWardPlot, venue.Ward, venue.Plot)
                                  : string.Format(l.HousingWard, venue.Ward),
                              ChipTone.Neutral, Icons.Housing);
                }

                Layout.Spacer(Theme.GapS);
                if (Btn.Draw(l.MoreInfo, BtnTone.Primary, BtnSize.Medium,
                             Icons.External, id: $"info_{ev.Id}"))
                    Plugin.OpenEstabDetail(venue);

                TravelButton.Draw(venue, $"ev_{ev.Id}", sameLine: true);
            }

        });
    }

    /// <summary>
    /// Carte d'événement. L'affiche propre à l'événement sert de bannière quand
    /// elle existe ; à défaut la carte reste sobre, la bannière du lieu étant
    /// déjà visible dans l'onglet des établissements.
    /// </summary>
    private void DrawEventCard(EventDto ev, System.Action content)
    {
        using var card = Card.Begin($"event_{ev.Id}", CardTone.Interactive,
                                    banner: Textures.Get(ev.Image),
                                    bannerHeight: 84f);
        content();
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

        Layout.Spacer(Theme.GapXs);

        var onlineWidth = Btn.Measure(l.ViewOnline, Icons.External);
        var searchWidth = ImGui.GetContentRegionAvail().X - onlineWidth - Theme.S(Theme.GapM);
        if (Inputs.SearchBar("##estabsearch", ref _estabSearchInput, l.Search, searchWidth))
            FetchEstablishments(_estabSearchInput.Trim());

        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        if (Btn.Draw(l.ViewOnline, BtnTone.Ghost, BtnSize.Medium, Icons.External, id: "estab_online"))
            OpenUrl(_config.BaseUrl + "/etablissements");

        Layout.Spacer(Theme.GapS);

        if (_estabLoading)
        {
            Feedback.SkeletonCards();
            return;
        }

        var visibleEstabs = GetVisibleEstablishments();

        if (visibleEstabs.Count == 0)
        {
            Feedback.EmptyState(Icons.Venues,
                _config.HiddenEstablishmentIds.Count > 0 ? l.EstabNoResults : l.EstabSearchHint);
            DrawHiddenEstablishmentsSection();
            return;
        }

        Layout.SectionHeader(l.TabEstabs, Icons.Venues, visibleEstabs.Count);

        if (!ImGui.BeginChild("##estabscroll", new Vector2(-1, -1), false)) return;

        string? toHide = null;
        foreach (var e in visibleEstabs)
        {
            bool hideThis = false;
            UiPrimitives.DrawCardWithBanner(Textures.Get(e.Banner), () =>
            {
                // ── Titre, teinté de la couleur choisie par le gérant ──────────
                var accent = Theme.TryParseHex(e.AccentColor) is { } custom
                    ? Theme.EnsureReadable(custom)
                    : Theme.Accent;

                Text.Title(e.Name, accent);

                if (e.IsFeatured)
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapS));
                    Chip.Draw(l.EstabFeatured, ChipTone.Gold, Icons.Sparkle);
                }

                // ── Catégories, chacune dans sa propre couleur ─────────────────
                if (e.Categories is { Count: > 0 })
                {
                    Layout.Spacer(Theme.GapXs);
                    var firstCategory = true;
                    foreach (var link in e.Categories)
                    {
                        if (link.Category is not { } category) continue;
                        if (!firstCategory) ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                        Chip.Colored(category.Name,
                                     Theme.TryParseHex(category.Color) ?? Theme.BgRaised,
                                     tooltip: category.Group);
                        firstCategory = false;
                    }
                }

                // ── Résumé de la description ──────────────────────────────────
                if (!string.IsNullOrWhiteSpace(e.Description))
                {
                    Layout.Spacer(Theme.GapS);
                    using (Fonts.PushBody())
                    {
                        ImGui.PushTextWrapPos(0f);
                        ImGui.TextColored(Theme.TextMuted, Glyphs.Summarize(e.Description, 170));
                        ImGui.PopTextWrapPos();
                    }
                }

                // ── Localisation et nature du lieu ────────────────────────────
                Layout.Spacer(Theme.GapS);

                if (!string.IsNullOrEmpty(e.Server))
                {
                    Chip.Draw(e.Server, ChipTone.Neutral, Icons.World,
                              tooltip: e.Datacenter);
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                }
                if (!string.IsNullOrEmpty(e.District))
                {
                    Chip.Draw(DistrictLabel(e.District), ChipTone.Neutral, Icons.Location);
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                }
                Chip.Draw(FormatAddress(e, l), ChipTone.Neutral, Icons.Housing);

                if (e.Counts is { Events: > 0 })
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(e.Counts.Events.ToString(), ChipTone.Accent, Icons.Events,
                              tooltip: l.TabEvents);
                }
                if (e.RpType == "semi_rp")
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(l.EstabSemiRp, ChipTone.Warning);
                }
                if (e.IsNsfw)
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw("18+", ChipTone.Danger);
                }

                // ── Actions ───────────────────────────────────────────────────
                Layout.Spacer(Theme.GapM);
                if (Btn.Draw(l.EstabDetail, BtnTone.Primary, BtnSize.Medium,
                             Icons.Info, id: $"detail_{e.Id}"))
                    Plugin.OpenEstabDetail(e);

                TravelButton.Draw(e, $"estab_list_{e.Id}", sameLine: true);

                ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                if (Btn.Draw(l.Hide, BtnTone.Ghost, BtnSize.Medium,
                             Icons.Hide, id: $"hide_est_{e.Id}"))
                    hideThis = true;

                // Raccourcis externes, alignés à droite.
                var shortcuts = 0f;
                if (!string.IsNullOrEmpty(e.DiscordInvite)) shortcuts += ImGui.GetFrameHeight() + Theme.S(Theme.GapXs);
                if (!string.IsNullOrEmpty(e.Website))       shortcuts += ImGui.GetFrameHeight() + Theme.S(Theme.GapXs);

                if (shortcuts > 0f)
                {
                    ImGui.SameLine();
                    Layout.RightAlign(shortcuts);

                    if (!string.IsNullOrEmpty(e.DiscordInvite))
                    {
                        if (Btn.Icon(Icons.Language, $"discord_{e.Id}", BtnTone.Ghost, l.EstabDiscord))
                            OpenUrl($"https://discord.gg/{e.DiscordInvite}");
                        ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    }
                    if (!string.IsNullOrEmpty(e.Website))
                    {
                        if (Btn.Icon(Icons.External, $"web_{e.Id}", BtnTone.Ghost, e.Website))
                            OpenUrl(e.Website);
                    }
                }
            });

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
            ImGui.TextColored(UiStyle.TextSubtle, $"  {Glyphs.Safe(ev.Title)}");
            ImGui.SameLine();
            if (ImGui.Button($"{Plugin.L.Show}##show_event_{ev.Id}", UiStyle.SmallButton))
                ShowEvent(ev.Id);
        }

        foreach (var est in hiddenEstabs)
        {
            ImGui.TextColored(UiStyle.TextSubtle, $"  {Glyphs.Safe(est.Name)}");
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
            ImGui.TextColored(UiStyle.TextSubtle, $"  {Glyphs.Safe(est.Name)}");
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

    private void FetchEstablishments(string search)
    {
        _estabLoading = true;
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

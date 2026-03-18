using Dalamud.Game.Command;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.ImGuiNotification;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Windows;

namespace EorzeaEventsPlugin;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Eorzea Events";
    private const string CommandMain = "/eorzea";

    // Services Dalamud — injectés via attributs
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager         CommandManager  { get; private set; } = null!;
    [PluginService] internal static IClientState            ClientState     { get; private set; } = null!;
    [PluginService] internal static IDataManager            DataManager     { get; private set; } = null!;
    [PluginService] internal static ITextureProvider        TextureProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog              Log             { get; private set; } = null!;
    [PluginService] internal static IFramework              Framework       { get; private set; } = null!;
    [PluginService] internal static INotificationManager    NotificationMgr { get; private set; } = null!;
    [PluginService] internal static IObjectTable            ObjectTable     { get; private set; } = null!;
    [PluginService] internal static IDtrBar                 DtrBar          { get; private set; } = null!;
    [PluginService] internal static IChatGui                ChatGui         { get; private set; } = null!;

    internal static Configuration Config { get; private set; } = null!;
    internal static ApiClient     Api    { get; private set; } = null!;

    private readonly WindowSystem     _windowSystem = new("EorzeaEvents");
    private static   MainWindow?      _mainWindow;
    private static   MySessionWindow? _sessionWindow;
    private static   ConfigWindow?    _configWindow;
    private static   SetupWindow?     _setupWindow;

    // DTR bar
    private IDtrBarEntry? _dtrRp;
    private IDtrBarEntry? _dtrEvents;

    // Notification + DTR polling
    private HashSet<string> _knownSessionIds  = [];
    private DateTime        _lastNotifCheck   = DateTime.MinValue;
    private bool            _notifInitialized = false;

    // Events DTR polling (moins fréquent)
    private DateTime _lastEventsCheck = DateTime.MinValue;
    private const int EventsPollIntervalSeconds = 5;

    // Surveillance tag RP
    private uint       _lastRpStatus    = 0;
    private const uint RpOnlineStatusId = 22; // "Role-playing" dans FFXIV

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Api    = new ApiClient(Config.BaseUrl, Config.ApiToken);

        _mainWindow    = new MainWindow(Config);
        _sessionWindow = new MySessionWindow(Config);
        _configWindow  = new ConfigWindow(Config);
        _setupWindow   = new SetupWindow(Config);
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_sessionWindow);
        _windowSystem.AddWindow(_configWindow);
        _windowSystem.AddWindow(_setupWindow);

        CommandManager.AddHandler(CommandMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Ouvre le panneau Eorzea Events. Utilisez /eorzea config pour configurer.",
        });

        PluginInterface.UiBuilder.Draw         += _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi   += OpenMain;
        Framework.Update                        += OnFrameworkUpdate;
        ClientState.TerritoryChanged            += OnTerritoryChanged;

        // DTR bar entries
        _dtrRp = DtrBar.Get("EorzeaEvents_RP");
        _dtrRp.Tooltip = new SeStringBuilder().AddText("Sessions RP sauvage en cours\nCliquez pour ouvrir").Build();
        _dtrRp.OnClick = _ => OpenMain();
        _dtrRp.Shown   = true;
        SetDtrRp(0);

        _dtrEvents = DtrBar.Get("EorzeaEvents_Ouv");
        _dtrEvents.Tooltip = new SeStringBuilder().AddText("Evenements en cours\nCliquez pour ouvrir").Build();
        _dtrEvents.OnClick = _ => OpenMain();
        _dtrEvents.Shown   = true;
        SetDtrEvents(0);

        if (string.IsNullOrWhiteSpace(Config.ApiToken))
            _setupWindow.IsOpen = true;

        if (!string.IsNullOrWhiteSpace(Config.ActiveSessionId))
            RestoreSession();
    }

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
            OpenConfig();
        else
            OpenMain();
    }

    internal static void OpenConfig()     { if (_configWindow  != null) _configWindow.IsOpen  = true; }
    internal static void OpenMain()       { if (_mainWindow    != null) _mainWindow.IsOpen    = true; }
    internal static void OpenMySession()  { if (_sessionWindow != null) _sessionWindow.IsOpen = true; }
    internal static bool HasActiveSession => _sessionWindow?.HasActiveSession ?? false;

    internal static void ClaimSession(RpSessionDto session)
    {
        if (_sessionWindow == null) return;
        _sessionWindow.SetActiveSession(session);
        Config.ActiveSessionId = session.Id;
        Config.Save();
        _sessionWindow.IsOpen = true;
    }

    internal static void RebuildApiClient()
    {
        Api.Dispose();
        Api = new ApiClient(Config.BaseUrl, Config.ApiToken);
    }

    // ─── DTR helpers ─────────────────────────────────────────────────────────────

    // Couleurs UIForeground : 3 = vert vif, 17 = jaune, 0 = défaut
    private const ushort ColorActive  = 43;  // vert
    private const ushort ColorDefault = 0;

    // Icones DTR — ajustez BitmapFontIcon selon vos préférences visuelles en jeu
    private const BitmapFontIcon IconRp     = BitmapFontIcon.CrossWorld;
    private const BitmapFontIcon IconEvents = BitmapFontIcon.Alarm;

    private void SetDtrRp(int count)
    {
        if (_dtrRp == null) return;
        var sb = new SeStringBuilder();
        sb.AddIcon(IconRp);
        sb.AddText(" · ");
        if (count > 0) sb.AddUiForeground(ColorActive);
        sb.AddText(count.ToString());
        if (count > 0) sb.AddUiForegroundOff();
        _dtrRp.Text = sb.Build();
    }

    private void SetDtrEvents(int count)
    {
        if (_dtrEvents == null) return;
        var sb = new SeStringBuilder();
        sb.AddIcon(IconEvents);
        sb.AddText(" · ");
        if (count > 0) sb.AddUiForeground(ColorActive);
        sb.AddText(count.ToString());
        if (count > 0) sb.AddUiForegroundOff();
        _dtrEvents.Text = sb.Build();
    }

    // ─── Polling ──────────────────────────────────────────────────────────────────

    private void OnFrameworkUpdate(IFramework fw)
    {
        var now = DateTime.UtcNow;

        // Sessions RP (5s)
        if ((Config.NotifyRpLive || Config.NotifyRpLiveChat) && (now - _lastNotifCheck).TotalSeconds >= 5)
        {
            _lastNotifCheck = now;
            var currentWorld = ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString();
            Task.Run(async () => await CheckNewSessionsAsync(currentWorld));
        }

        // Evénements en cours (5 min)
        if ((now - _lastEventsCheck).TotalSeconds >= EventsPollIntervalSeconds)
        {
            _lastEventsCheck = now;
            Task.Run(async () => await CheckOngoingEventsAsync());
        }

        // Polling session active (fenêtre ouverte ou non)
        _sessionWindow?.PollSessionStatus();

        // Surveillance tag RP (chaque frame, lecture uint = négligeable)
        if (Config.AlertOnRpTagRemoved && _sessionWindow is { HasActiveSession: true })
        {
            var player = ObjectTable.LocalPlayer;
            if (player != null) // null = écran de chargement, on ignore
            {
                var current = player.OnlineStatus.RowId;
                if (_lastRpStatus == RpOnlineStatusId && current != RpOnlineStatusId)
                {
                    _sessionWindow.OnRpTagRemoved();
                    _sessionWindow.IsOpen = true;
                }
                _lastRpStatus = current; // player != null suffit à exclure les écrans de chargement
            }
        }
    }

    private async Task CheckNewSessionsAsync(string? currentWorld)
    {
        try
        {
            var sessions = await Api.GetActiveSessionsAsync();
            var ids      = sessions.Select(s => s.Id).ToHashSet();

            // Mise à jour DTR sessions + liste MainWindow (réutilise les données, pas de 2e appel API)
            var activeCount = sessions.Count(s => s.EndedAt == null);
            SetDtrRp(activeCount);
            _mainWindow?.UpdateSessionsList(sessions);

            if (!_notifInitialized)
            {
                _knownSessionIds  = ids;
                _notifInitialized = true;
                return;
            }

            foreach (var session in sessions)
            {
                if (_knownSessionIds.Contains(session.Id)) continue;
                if (Config.NotifyMyWorld && currentWorld != null && session.Server != currentWorld) continue;

                if (Config.NotifyRpLive)
                    NotificationMgr.AddNotification(new Notification
                    {
                        Title           = "Nouvelle session RP Live",
                        Content         = $"{session.Title} — {session.Location} ({session.Server})",
                        Type            = NotificationType.Info,
                        InitialDuration = TimeSpan.FromSeconds(6),
                    });

                if (Config.NotifyRpLiveChat)
                    ChatGui.Print(new SeStringBuilder()
                        .AddUiForeground(32)
                        .AddText("[Eorzea Events] ")
                        .AddUiForegroundOff()
                        .AddText($"Nouvelle session RP : {session.Title} — {session.Location} ({session.Server})")
                        .Build());
            }

            _knownSessionIds = ids;
        }
        catch { /* silencieux */ }
    }

    private async Task CheckOngoingEventsAsync()
    {
        try
        {
            var events = await Api.GetUpcomingEventsAsync(1);
            var now    = DateTime.UtcNow;
            var count  = events.Count(e =>
            {
                if (!DateTime.TryParse(e.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
                    return false;
                if (now < start) return false;
                if (string.IsNullOrEmpty(e.EndDate)) return false;
                if (!DateTime.TryParse(e.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
                    return false;
                return now <= end;
            });
            SetDtrEvents(count);
        }
        catch { /* silencieux */ }
    }

    private void RestoreSession()
    {
        var id = Config.ActiveSessionId!;
        Task.Run(async () =>
        {
            try
            {
                var session = await Api.GetSessionAsync(id);
                if (session != null && session.EndedAt == null)
                    _sessionWindow?.SetActiveSession(session);
                else
                {
                    Config.ActiveSessionId = null;
                    Config.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[EorzeaEvents] Impossible de restaurer la session: {ex.Message}");
            }
        });
    }

    private void OnTerritoryChanged(ushort territory)
    {
        if (Config.AlertOnZoneChange && _sessionWindow is { HasActiveSession: true })
        {
            _sessionWindow.OnZoneChanged();
            _sessionWindow.IsOpen = true;
        }
    }

    public void Dispose()
    {
        ClientState.TerritoryChanged            -= OnTerritoryChanged;
        Framework.Update                        -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw         -= _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi   -= OpenMain;
        CommandManager.RemoveHandler(CommandMain);
        _dtrRp?.Remove();
        _dtrEvents?.Remove();
        Api.Dispose();
    }
}

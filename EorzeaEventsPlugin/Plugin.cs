using Dalamud.Game.Command;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.ImGuiNotification;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Windows;
using Lumina.Excel.Sheets;

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
    [PluginService] internal static IToastGui               ToastGui        { get; private set; } = null!;
    [PluginService] internal static IGameGui                GameGui         { get; private set; } = null!;

    internal static Configuration Config { get; private set; } = null!;
    internal static ApiClient     Api    { get; private set; } = null!;

    /// <summary>Retourne la localisation active (auto-détection ou choix manuel).</summary>
    internal static Loc L
    {
        get
        {
            var lang = Config.Language;
            if (lang == PluginLanguage.Auto)
                lang = ClientState.ClientLanguage == Dalamud.Game.ClientLanguage.French
                    ? PluginLanguage.French
                    : PluginLanguage.English;
            return lang == PluginLanguage.French ? Loc.Fr : Loc.En;
        }
    }

    private readonly WindowSystem     _windowSystem = new("EorzeaEvents");
    private static   MainWindow?      _mainWindow;
    private static   MySessionWindow? _sessionWindow;
    private static   ConfigWindow?    _configWindow;
    private static   SetupWindow?     _setupWindow;

    // DTR bar
    private static IDtrBarEntry? _dtrRp;
    private static IDtrBarEntry? _dtrEvents;

    // Notification + DTR polling
    private HashSet<string> _knownSessionIds  = [];
    private DateTime        _lastNotifCheck   = DateTime.MinValue;
    private bool            _notifInitialized = false;

    // Events DTR polling (moins fréquent)
    private DateTime _lastEventsCheck = DateTime.MinValue;
    private const int EventsPollIntervalSeconds = 5;

    // Heartbeat plugin (toutes les 60 s, seulement si token configuré)
    private DateTime _lastHeartbeat = DateTime.MinValue;
    private const int HeartbeatIntervalSeconds = 60;

    // Heartbeat présence en venue (toutes les 60 s, seulement si dans un quartier résidentiel)
    private DateTime _lastPresenceHeartbeat = DateTime.MinValue;
    private const int PresenceHeartbeatIntervalSeconds = 60;

    // Surveillance tag RP
    private uint       _lastRpStatus    = 0;
    private const uint RpOnlineStatusId = 22; // "Role-playing" dans FFXIV

    // Zone courante (mise à jour au changement de territoire)
    internal static string? CurrentZone { get; private set; }

    // IDs des sessions appartenant à l'utilisateur courant (rafraîchi toutes les 30 s)
    internal static HashSet<string> MySessionIds { get; private set; } = [];
    private DateTime _lastMySessionsCheck = DateTime.MinValue;
    private const int MySessionsIntervalSeconds = 30;

    // Version gate — bloque le plugin si la version est trop ancienne
    internal static bool   IsBlocked      { get; private set; } = false;
    internal static string BlockedMessage { get; private set; } = string.Empty;

    // Token invalide — notification envoyée une seule fois jusqu'au prochain renouvellement
    private bool _tokenInvalidNotified = false;

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
        _dtrRp.Tooltip = new SeStringBuilder().AddText(L.DtrRpTooltip).Build();
        _dtrRp.OnClick = _ => OpenMain();
        _dtrRp.Shown   = Config.ShowDtrRp;
        SetDtrRp(0);

        _dtrEvents = DtrBar.Get("EorzeaEvents_Ouv");
        _dtrEvents.Tooltip = new SeStringBuilder().AddText(L.DtrEventsTooltip).Build();
        _dtrEvents.OnClick = _ => OpenMain();
        _dtrEvents.Shown   = Config.ShowDtrEvents;
        SetDtrEvents(0);

        if (string.IsNullOrWhiteSpace(Config.ApiToken))
            OpenSetup();

        if (!string.IsNullOrWhiteSpace(Config.ActiveSessionId))
            RestoreSession();

        // Charger les sessions de l'utilisateur dès le démarrage + vérifier la validité du token
        if (!string.IsNullOrWhiteSpace(Config.ApiToken))
            Task.Run(async () =>
            {
                MySessionIds = await Api.GetMySessionIdsAsync();
                CheckTokenValidity();
            });

        // Vérifier la version minimale requise
        Task.Run(async () => await CheckMinimumVersionAsync());

        // Initialiser la zone courante
        CurrentZone = ResolveTerritoryName(ClientState.TerritoryType);
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
    internal static void OpenSetup(bool tokenInvalid = false)
    {
        // Fermer toutes les autres fenêtres avant de rouvrir l'assistant
        if (_mainWindow    != null) _mainWindow.IsOpen    = false;
        if (_sessionWindow != null) _sessionWindow.IsOpen = false;
        if (_configWindow  != null) _configWindow.IsOpen  = false;
        _setupWindow?.Restart(tokenInvalid);
    }
    internal static bool HasActiveSession => _sessionWindow?.HasActiveSession ?? false;

    internal static void ClaimSession(RpSessionDto session)
    {
        if (_sessionWindow == null) return;
        _sessionWindow.SetActiveSession(session);
        Config.ActiveSessionId = session.Id;
        Config.Save();
        _sessionWindow.IsOpen = true;
    }

    internal static void ApplyDtrVisibility()
    {
        if (_dtrRp     != null) _dtrRp.Shown     = Config.ShowDtrRp;
        if (_dtrEvents != null) _dtrEvents.Shown = Config.ShowDtrEvents;
    }

    internal static void RebuildApiClient()
    {
        Api.Dispose();
        Api = new ApiClient(Config.BaseUrl, Config.ApiToken);
    }

    // ─── DTR helpers ─────────────────────────────────────────────────────────────

    // Couleurs UIGlow : 32 = bleu, 17 = jaune (glow autour du texte blanc)
    private const ushort GlowActive = 32;  // bleu
    private const ushort GlowIdle   = 17;  // jaune

    private void SetDtrRp(int count)
    {
        if (_dtrRp == null) return;
        var sb = new SeStringBuilder();
        sb.AddText("RP: ");
        sb.AddUiGlow(count > 0 ? GlowActive : GlowIdle);
        sb.AddText(count.ToString());
        sb.AddUiGlowOff();
        _dtrRp.Text = sb.Build();
    }

    private void SetDtrEvents(int count)
    {
        if (_dtrEvents == null) return;
        var sb = new SeStringBuilder();
        sb.AddText("Events: ");
        sb.AddUiGlow(count > 0 ? GlowActive : GlowIdle);
        sb.AddText(count.ToString());
        sb.AddUiGlowOff();
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

        // Evénements en cours (5 s)
        if ((now - _lastEventsCheck).TotalSeconds >= EventsPollIntervalSeconds)
        {
            _lastEventsCheck = now;
            Task.Run(async () => await CheckOngoingEventsAsync());
        }

        // Heartbeat (60 s) — seulement si token configuré
        if (!string.IsNullOrWhiteSpace(Config.ApiToken)
            && (now - _lastHeartbeat).TotalSeconds >= HeartbeatIntervalSeconds)
        {
            _lastHeartbeat = now;
            Task.Run(async () =>
            {
                var v = PluginInterface.Manifest.AssemblyVersion;
                await Api.HeartbeatAsync($"{v.Major}.{v.Minor}.{v.Build}");
                CheckTokenValidity();
            });
        }

        // Réinitialise le flag si le token a été renouvelé et est redevenu valide
        if (_tokenInvalidNotified && Api.IsTokenValid)
            _tokenInvalidNotified = false;

        // Sessions de l'utilisateur courant (30 s) — seulement si token configuré
        if (!string.IsNullOrWhiteSpace(Config.ApiToken)
            && (now - _lastMySessionsCheck).TotalSeconds >= MySessionsIntervalSeconds)
        {
            _lastMySessionsCheck = now;
            Task.Run(async () => { MySessionIds = await Api.GetMySessionIdsAsync(); });
        }

        // Présence en venue (60 s) — toujours actif si joueur connecté (pas de token requis)
        if (ClientState.IsLoggedIn
            && (now - _lastPresenceHeartbeat).TotalSeconds >= PresenceHeartbeatIntervalSeconds)
        {
            _lastPresenceHeartbeat = now;
            var territory = ClientState.TerritoryType;
            var world     = ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString();
            if (territory > 0 && !string.IsNullOrWhiteSpace(world))
                Task.Run(async () => await Api.PresenceHeartbeatAsync(territory, world, Config.ClientId));
        }

        // Polling session active (fenêtre ouverte ou non)
        _sessionWindow?.PollSessionStatus();

        // Surveillance tag RP (chaque frame, lecture uint = négligeable)
        var rpPlayer = ObjectTable.LocalPlayer;
        if (rpPlayer != null) // null = écran de chargement, on ignore
        {
            var current = rpPlayer.OnlineStatus.RowId;

            // Tag RP activé sans session en cours → proposer de démarrer une session
            if (Config.SuggestSessionOnRpTag && _sessionWindow is { HasActiveSession: false }
                && _lastRpStatus != RpOnlineStatusId && current == RpOnlineStatusId)
            {
                _sessionWindow.OnRpTagActivated();
                _sessionWindow.IsOpen = true;
            }

            // Tag RP retiré avec session en cours → proposer de terminer
            if (Config.AlertOnRpTagRemoved && _sessionWindow is { HasActiveSession: true }
                && _lastRpStatus == RpOnlineStatusId && current != RpOnlineStatusId)
            {
                _sessionWindow.OnRpTagRemoved();
                _sessionWindow.IsOpen = true;
            }

            _lastRpStatus = current;
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

                var isNearby = currentWorld != null && CurrentZone != null
                    && session.Server == currentWorld && session.Location == CurrentZone;

                // Alerte "dans votre zone" — ShowQuest pour le son + style doré
                if (isNearby && Config.NotifyNearbyZone)
                {
                    ToastGui.ShowQuest(
                        string.Format(L.NotifNearbyRp, session.Title),
                        new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true, DisplayCheckmark = false });
                }
                // Notifications globales (filtrées si "mon monde" est coché)
                else
                {
                    if (Config.NotifyMyWorld && currentWorld != null && session.Server != currentWorld) continue;

                    if (Config.NotifyRpLiveScreen)
                        ToastGui.ShowNormal(
                            string.Format(L.NotifNewRpScreen, session.Title, session.Location, session.Server),
                            new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow });

                    if (Config.NotifyRpLive)
                        NotificationMgr.AddNotification(new Notification
                        {
                            Title           = L.NotifNewRpTitle,
                            Content         = $"{session.Title} — {session.Location} ({session.Server})",
                            Type            = NotificationType.Info,
                            InitialDuration = TimeSpan.FromSeconds(6),
                        });

                    if (Config.NotifyRpLiveChat)
                        ChatGui.Print(new SeStringBuilder()
                            .AddUiForeground(32)
                            .AddText("[Eorzea Events] ")
                            .AddUiForegroundOff()
                            .AddText(string.Format(L.NotifNewRpChat, session.Title, session.Location, session.Server))
                            .Build());
                }
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

    private void CheckTokenValidity()
    {
        if (Api.IsTokenValid || _tokenInvalidNotified) return;
        _tokenInvalidNotified = true;

        NotificationMgr.AddNotification(new Notification
        {
            Title           = L.NotifTokenTitle,
            Content         = L.NotifTokenContent,
            Type            = NotificationType.Warning,
            InitialDuration = TimeSpan.FromSeconds(12),
        });

        ChatGui.Print(new SeStringBuilder()
            .AddUiForeground(17) // jaune
            .AddText("[Eorzea Events] ")
            .AddUiForegroundOff()
            .AddText(L.NotifTokenContent)
            .Build());

        Log.Warning("[EorzeaEvents] Token API invalide — 401 reçu sur le heartbeat.");

        OpenSetup(tokenInvalid: true);
    }

    private static async Task CheckMinimumVersionAsync()
    {
        try
        {
            var info = await Api.GetVersionInfoAsync();
            if (info == null) return;

            var current     = PluginInterface.Manifest.AssemblyVersion;
            var minimumStr  = PluginInterface.IsTesting ? info.TestingMinimum : info.Minimum;
            if (!Version.TryParse(minimumStr, out var minimum)) return;
            if (current >= minimum) return;

            IsBlocked      = true;
            BlockedMessage = $"Le plugin nécessite une mise à jour.\n\n" +
                             $"Version installée : {current.Major}.{current.Minor}.{current.Build}\n" +
                             $"Version minimale  : {minimum.Major}.{minimum.Minor}.{minimum.Build}\n\n" +
                             $"Ouvre le gestionnaire de plugins pour mettre à jour Eorzea Events.";

            Log.Warning($"[EorzeaEvents] Plugin bloqué — version {current} < minimum {minimum}");
        }
        catch (Exception ex)
        {
            Log.Warning($"[EorzeaEvents] Impossible de vérifier la version minimale : {ex.Message}");
        }
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

    private static string? ResolveTerritoryName(ushort territoryId)
    {
        var sheet = DataManager.GetExcelSheet<TerritoryType>();
        var row   = sheet?.GetRowOrDefault(territoryId);
        return row?.PlaceName.Value.Name.ToString();
    }

    private void OnTerritoryChanged(ushort territory)
    {
        CurrentZone = ResolveTerritoryName(territory);

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

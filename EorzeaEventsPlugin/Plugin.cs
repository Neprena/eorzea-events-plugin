using Dalamud.Game.Command;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.ImGuiNotification;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Windows;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace EorzeaEventsPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private enum PluginGateMode
    {
        None,
        UpdateRequired,
        EmergencyBlock,
    }

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
    [PluginService] internal static INamePlateGui           NamePlateGui    { get; private set; } = null!;
    [PluginService] internal static IChatGui                ChatGui         { get; private set; } = null!;
    [PluginService] internal static IToastGui               ToastGui        { get; private set; } = null!;
    [PluginService] internal static IGameGui                GameGui         { get; private set; } = null!;
    [PluginService] internal static IContextMenu            ContextMenu     { get; private set; } = null!;

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

    /// <summary>
    /// Version installée sous la forme <c>major.minor.build</c>, celle que
    /// l'utilisateur voit dans le gestionnaire de plugins. Le quatrième segment
    /// de l'AssemblyVersion vaut toujours 0 et n'apporte rien.
    /// </summary>
    internal static string VersionLabel()
    {
        var v = PluginInterface.Manifest.AssemblyVersion;
        return $"{v.Major}.{v.Minor}.{v.Build}";
    }

    private readonly WindowSystem     _windowSystem = new("EorzeaEvents");
    private static   MainWindow?        _mainWindow;
    private static   MySessionWindow?   _sessionWindow;
    private static   SetupWindow?       _setupWindow;
    private static   EstabDetailWindow? _estabDetailWindow;
    private static   RpProfileWindow?      _rpProfileWindow;
    private static   RpAnnouncementWindow? _announcementWindow;
    private static   WhatsNewWindow?       _whatsNewWindow;

    /// <summary>Voyage assisté vers une parcelle, si Lifestream est présent.</summary>
    internal static Ipc.LifestreamIpc Lifestream { get; private set; } = null!;

    // RP Availability — nameplate indicators (name+world → (level, approachMode))
    private static Dictionary<(string Name, string World), (string? Level, string? ApproachMode)> _availablePlayers = [];

    /// <summary>
    /// Joueurs actuellement déclarés disponibles pour du RP, tels que renvoyés
    /// par l'API publique. Alimente la page « Autour de moi » et le menu
    /// contextuel, qui ont besoin de la fiche et non du seul couple
    /// niveau / mode d'approche retenu pour les nameplates.
    /// </summary>
    internal static IReadOnlyList<Api.RpAvailabilityEntryDto> AvailableEntries { get; private set; } = [];

    // Prompt "rester disponible ?" affiché au prochain rendu de l'onglet RP
    internal static bool LoginPromptPending { get; private set; } = false;

    /// <summary>
    /// Personnage actuellement connecté, ou null hors du jeu. Sert de clé à
    /// tout ce qui lui est propre : fiche RP et disponibilité.
    /// </summary>
    internal static (string Name, int WorldId)? CurrentCharacter
    {
        get
        {
            // La table d'objets n'est interrogeable que depuis le thread de
            // jeu et lève « Not on main thread! » ailleurs. Or le constructeur
            // du plugin, qui initialise la barre de statut, s'exécute sur un
            // thread de pool : l'accès est donc protégé plutôt qu'interdit,
            // l'appelant obtenant simplement « aucun personnage » à ce moment.
            try
            {
                if (!ClientState.IsLoggedIn) return null;

                var player = ObjectTable.LocalPlayer;
                return player == null
                    ? null
                    : (player.Name.TextValue, (int)player.HomeWorld.RowId);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Monde sur lequel le personnage se trouve actuellement, ou null hors du
    /// jeu. C'est le monde courant et non le monde d'origine : un joueur en
    /// voyage doit voir les disponibilités de là où il est.
    ///
    /// Même précaution que <see cref="CurrentCharacter"/> : la table d'objets
    /// n'est interrogeable que depuis le thread de jeu.
    /// </summary>
    internal static string? CurrentWorldName()
    {
        try
        {
            if (!ClientState.IsLoggedIn) return null;
            return ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Genre du personnage connecté, pour accorder les libellés qui parlent de
    /// lui à la première personne. Retourne false hors du jeu, le masculin
    /// servant alors de forme par défaut.
    ///
    /// Même convention que les titres de plaque de nom (voir
    /// <c>OnNamePlateUpdate</c>) : <c>Customize[1]</c> vaut 0 au masculin.
    /// </summary>
    internal static bool CurrentCharacterIsFemale()
    {
        try
        {
            if (!ClientState.IsLoggedIn) return false;
            return ObjectTable.LocalPlayer?.Customize[1] is { } gender && gender != 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Disponibilité RP du personnage connecté. Remplace l'ancien indicateur
    /// commun au compte, qui déclarait tous les personnages d'un joueur dès que
    /// l'un d'eux se disait disponible.
    /// </summary>
    internal static bool CurrentCharacterAvailable
    {
        get => CurrentCharacter is { } c && Config.IsAvailable(c.Name, c.WorldId);
        set
        {
            if (CurrentCharacter is not { } c) return;
            Config.SetAvailable(c.Name, c.WorldId, value);
        }
    }

    /// <summary>
    /// Publie la disponibilité du personnage connecté auprès du serveur.
    /// Appelé depuis les réglages comme depuis la fiche RP.
    /// </summary>
    internal static void PublishAvailability(bool available)
    {
        if (!available)
        {
            _ = Task.Run(async () => await Api.ClearRpAvailabilityAsync());
            return;
        }

        var player = ObjectTable.LocalPlayer;
        if (player == null) return;

        var request = new Api.SetRpAvailableRequest
        {
            CharacterName = player.Name.TextValue,
            Server        = player.HomeWorld.Value.Name.ToString(),
            Zone          = CurrentZone,
            TerritoryId   = (int)ClientState.TerritoryType > 0
                                ? (int)ClientState.TerritoryType
                                : null,
        };
        _ = Task.Run(async () => await Api.SetRpAvailableAsync(request));
    }

    internal static bool IsLocalPlayerAvailable()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null) return false;
        var name  = player.Name.TextValue;
        var world = player.HomeWorld.Value.Name.ToString().ToLowerInvariant();
        return _availablePlayers.ContainsKey((name, world));
    }
    private DateTime _lastAvailabilityCheck = DateTime.MinValue;
    private const int AvailabilityPollIntervalSeconds = 5;

    // DTR bar
    private static IDtrBarEntry? _dtrRp;
    private static IDtrBarEntry? _dtrEvents;
    private static IDtrBarEntry? _dtrRpAvail;

    // Notification + DTR polling
    private HashSet<string> _knownSessionIds  = [];
    private DateTime        _lastNotifCheck   = DateTime.MinValue;
    private bool            _notifInitialized = false;

    // Events DTR polling (moins fréquent)
    private DateTime _lastEventsCheck = DateTime.MinValue;
    private const int EventsPollIntervalSeconds = 5;
    private HashSet<string> _knownOngoingEventKeys = [];
    private bool _eventsNotifInitialized = false;

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
    internal static string BlockedUpdateUrl { get; private set; } = string.Empty;
    private static PluginGateMode _gateMode = PluginGateMode.None;
    private DateTime _lastVersionCheck = DateTime.MinValue;
    private const int VersionCheckIntervalSeconds = 10;

    // Token invalide — notification envoyée une seule fois jusqu'au prochain renouvellement
    private bool _tokenInvalidNotified = false;

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (Config.Version < 2)
        {
            Config.NotifyEventStartChat = true;
            Config.Version = 2;
            Config.Save();
        }

        // La fiche RP et la disponibilité deviennent propres à chaque
        // personnage : reporter l'ancien état commun au compte.
        Config.MigrateToPerCharacter();
        Api    = new ApiClient(Config.BaseUrl, Config.ApiToken);

        // Les abonnements IPC sont inertes tant que Lifestream n'est pas là :
        // les créer tôt évite de tester sa présence à chaque affichage.
        Lifestream = new Ipc.LifestreamIpc(PluginInterface);

        // Avant la création des fenêtres : l'atlas se construit en tâche de fond,
        // les premières frames retombent sur la police Dalamud.
        Ui.Fonts.Build(PluginInterface);

        _mainWindow        = new MainWindow(Config);
        _sessionWindow     = new MySessionWindow(Config);
        _setupWindow       = new SetupWindow(Config);
        _estabDetailWindow = new EstabDetailWindow(Config);
        _rpProfileWindow    = new RpProfileWindow(Config);
        _announcementWindow = new RpAnnouncementWindow(Config);
        _whatsNewWindow     = new WhatsNewWindow(Config);
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_sessionWindow);
        _windowSystem.AddWindow(_setupWindow);
        _windowSystem.AddWindow(_estabDetailWindow);
        _windowSystem.AddWindow(_rpProfileWindow);
        _windowSystem.AddWindow(_announcementWindow);
        _windowSystem.AddWindow(_whatsNewWindow);

        CommandManager.AddHandler(CommandMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Ouvre le panneau. /eorzea config = paramètres. /eorzea link = lier le personnage actuel.",
        });

        PluginInterface.UiBuilder.Draw         += _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi   += OpenMain;
        Framework.Update                        += OnFrameworkUpdate;
        ClientState.TerritoryChanged            += OnTerritoryChanged;
        NamePlateGui.OnNamePlateUpdate          += OnNamePlateUpdate;
        ClientState.Login                       += OnLogin;
        ContextMenu.OnMenuOpened                += OnMenuOpened;

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

        _dtrRpAvail = DtrBar.Get("EorzeaEvents_RpAvail");
        _dtrRpAvail.Tooltip = new SeStringBuilder().AddText(L.DtrRpAvailTooltip).Build();
        _dtrRpAvail.OnClick = e =>
        {
            if (CurrentCharacterAvailable)
            {
                Task.Run(ClearRpAvailabilityAsync);
                return;
            }

            // Capturer les données du joueur sur le framework thread avant Task.Run
            var player = ObjectTable.LocalPlayer;
            if (player == null) return;
            var req = new Api.SetRpAvailableRequest
            {
                CharacterName = player.Name.TextValue,
                Server        = player.HomeWorld.Value.Name.ToString(),
                Zone          = CurrentZone,
                TerritoryId   = (int)ClientState.TerritoryType > 0 ? (int?)ClientState.TerritoryType : null,
            };
            Task.Run(async () =>
            {
                var ok = await Api.SetRpAvailableAsync(req);
                if (ok)
                {
                    CurrentCharacterAvailable = true;
                    Config.Save();
                    _ = Framework.RunOnFrameworkThread(UpdateDtrRpAvail);
                }
            });
        };
        _dtrRpAvail.Shown = Config.ShowDtrRpAvail;
        UpdateDtrRpAvail();

#if DEBUG
        if (string.IsNullOrWhiteSpace(Config.ApiToken))
        {
            var devToken = Environment.GetEnvironmentVariable("EE_DEV_TOKEN");
            if (!string.IsNullOrWhiteSpace(devToken))
            {
                Config.ApiToken = devToken.Trim();
                Config.Save();
                RebuildApiClient();
            }
        }
#endif

        // Si le plugin est rechargé en cours de jeu, ouvrir les fenêtres immédiatement.
        // Sinon, OnLogin() s'en chargera quand le joueur sélectionnera un personnage.
        if (ClientState.IsLoggedIn)
            DoFirstRunCheck();

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

    private void DoFirstRunCheck()
    {
        if (string.IsNullOrWhiteSpace(Config.ApiToken) && Config.CharacterTokens.Count == 0)
            OpenSetup();
        else if (!string.IsNullOrWhiteSpace(Config.ApiToken)
            && Config.CharacterTokens.Count == 0
            && !Config.MigrationNoticeSeen)
            _setupWindow?.Restart(migration: true);

        if (!Config.RpAnnouncementSeen && !string.IsNullOrWhiteSpace(Config.ApiToken))
            if (_announcementWindow != null) _announcementWindow.IsOpen = true;
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Equals("config", StringComparison.OrdinalIgnoreCase))
            OpenConfig();
        else if (trimmed.Equals("link", StringComparison.OrdinalIgnoreCase))
            _ = StartCharacterLinkAsync();
        else
            OpenMain();
    }

    /// <summary>
    /// Ouvre les réglages. Ils vivent désormais dans la coque principale, ce
    /// que l'engrenage de la liste des plugins doit aussi atteindre.
    /// </summary>
    internal static void OpenConfig()
    {
        if (IsBlocked)
        {
            OpenMain();
            return;
        }
        _mainWindow?.OpenAt("settings");
    }
    internal static void OpenMain()       { if (_mainWindow    != null) _mainWindow.IsOpen    = true; }

    /// <summary>Rouvre les nouveautés à la demande, depuis les réglages.</summary>
    internal static void OpenWhatsNew() => _whatsNewWindow?.Open();

    internal static void OpenEstabDetail(EorzeaEventsPlugin.Api.EstablishmentDto estab)
        { _estabDetailWindow?.Open(estab); }
    internal static void OpenEstabDetail(EorzeaEventsPlugin.Api.EstablishmentSummaryDto estab)
        { _estabDetailWindow?.Open(estab); }
    internal static void OpenMySession()
    {
        if (IsBlocked)
        {
            OpenMain();
            return;
        }
        if (_sessionWindow != null) _sessionWindow.IsOpen = true;
    }
    /// <summary>
    /// Ouvre la page « Mon profil RP » de la coque, qui remplace l'ancien
    /// assistant en fenêtre séparée.
    /// </summary>
    internal static void OpenRpProfileWizard()
    {
        if (IsBlocked) { OpenMain(); return; }
        _mainWindow?.OpenAt("profile");
    }

    /// <summary>
    /// Ouvre sa propre fiche telle que les autres la voient, en passant par la
    /// route publique plutôt qu'en simulant la redaction côté plugin.
    /// </summary>
    internal static void OpenRpProfilePreview(string characterId, string characterName, string? server)
    {
        if (IsBlocked)
        {
            OpenMain();
            return;
        }

        _rpProfileWindow?.OpenPreview(characterId, characterName, server);
    }

    internal static void OpenRpProfileViewer(Api.RpAvailabilityEntryDto entry)
    {
        // Même garde que OpenConfig et OpenSetup : quand le plugin est bloqué par
        // le gate de version, seule la fenêtre principale doit s'ouvrir, pour y
        // afficher le message de mise à jour.
        if (IsBlocked)
        {
            OpenMain();
            return;
        }

        _rpProfileWindow?.OpenViewer(entry);
    }

    internal static void OpenSetup(bool tokenInvalid = false, bool migration = false)
    {
        if (IsBlocked)
        {
            OpenMain();
            return;
        }
        // Fermer toutes les autres fenêtres avant de rouvrir l'assistant
        if (_mainWindow    != null) _mainWindow.IsOpen    = false;
        if (_sessionWindow != null) _sessionWindow.IsOpen = false;
        _setupWindow?.Restart(tokenInvalid, migration);
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
        if (_dtrRp      != null) _dtrRp.Shown      = Config.ShowDtrRp;
        if (_dtrEvents  != null) _dtrEvents.Shown  = Config.ShowDtrEvents;
        if (_dtrRpAvail != null) _dtrRpAvail.Shown = Config.ShowDtrRpAvail;
    }

    internal static void UpdateDtrRpAvail()
    {
        if (_dtrRpAvail == null) return;
        var sb = new SeStringBuilder();
        if (CurrentCharacterAvailable)
        {
            sb.AddUiGlow(52);
            sb.AddText("♦");
            sb.AddUiGlowOff();
        }
        else
        {
            sb.AddUiGlow(GlowIdle);
            sb.AddText("◇");
            sb.AddUiGlowOff();
        }
        _dtrRpAvail.Text = sb.Build();
    }

    internal static void RebuildApiClient()
    {
        Api.Dispose();
        Api = new ApiClient(Config.BaseUrl, Config.ApiToken);
        _lastTokenAppliedKey = null; // force la ré-application après reconstruction
    }

    // ─── Sélection auto du token selon le perso connecté ────────────────────

    /// <summary>
    /// Clé du dernier token appliqué au client API. Permet d'éviter de re-set
    /// le token à chaque framework update si le perso n'a pas changé.
    /// Format : "{characterName}@{worldId}" ou "legacy" ou "none".
    /// </summary>
    private static string? _lastTokenAppliedKey;

    /// <summary>
    /// Synchronise le token utilisé par <see cref="Api"/> avec le perso actuellement
    /// connecté in-game. À appeler depuis le framework thread (lit ObjectTable).
    /// Sélectionne :
    ///  - le CharacterApiToken si lié à (name, worldId),
    ///  - sinon, le token legacy (Config.ApiToken),
    ///  - sinon, aucun token.
    /// </summary>
    private static void EnsureTokenForActivePlayer()
    {
        string key;
        string tokenToApply;
        var player = ObjectTable.LocalPlayer;
        if (player != null)
        {
            var name = player.Name.TextValue;
            var worldId = (int)player.HomeWorld.RowId;
            var contentId = GetLocalContentId();
            // Identité stable d'abord (survit au rename), repli sur name+world pour
            // les entrées legacy — qu'on backfill alors avec le ContentId connu.
            var entry = Config.FindCharacterTokenByContentId(contentId);
            if (entry == null)
            {
                entry = Config.FindCharacterToken(name, worldId);
                if (entry != null && contentId != 0 && entry.ContentId == 0)
                {
                    entry.ContentId = contentId;
                    Config.Save();
                }
            }
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Token))
            {
                key = contentId != 0 ? $"cid:{contentId}" : $"{name}@{worldId}";
                tokenToApply = entry.Token;
            }
            else if (!string.IsNullOrWhiteSpace(Config.ApiToken))
            {
                key = "legacy";
                tokenToApply = Config.ApiToken;
            }
            else
            {
                key = "none";
                tokenToApply = string.Empty;
            }
        }
        else if (!string.IsNullOrWhiteSpace(Config.ApiToken))
        {
            key = "legacy";
            tokenToApply = Config.ApiToken;
        }
        else
        {
            key = "none";
            tokenToApply = string.Empty;
        }

        if (key == _lastTokenAppliedKey) return;
        _lastTokenAppliedKey = key;
        Api.SetToken(string.IsNullOrEmpty(tokenToApply) ? null : tokenToApply);
        Log.Debug($"[EorzeaEvents] Token API basculé sur '{key}'.");
    }

    /// <summary>
    /// ContentId du personnage local (identité stable, survit au rename). 0 si non
    /// connecté. IClientState.LocalContentId n'existe plus en SDK 15 → on lit le champ
    /// ContentId via FFXIVClientStructs. À appeler sur le framework thread.
    /// </summary>
    private static unsafe ulong GetLocalContentId()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null || player.Address == IntPtr.Zero) return 0;
        return ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address)->ContentId;
    }

    // ─── Workflow de couplage d'un personnage (web-link) ────────────────────

    /// <summary>
    /// État courant d'une session de couplage en cours (lue par l'UI ConfigWindow).
    /// </summary>
    internal sealed class CharacterLinkState
    {
        public string CharacterName { get; init; } = string.Empty;
        public int    WorldId       { get; init; }
        public string WorldName     { get; init; } = string.Empty;
        public string LinkUrl       { get; init; } = string.Empty;
        public DateTime ExpiresAt   { get; init; }
        public string Status        { get; set; } = "pending"; // pending | bound | expired | error
        public string? ErrorMessage { get; set; }
    }

    internal static CharacterLinkState? ActiveLinkState { get; private set; }

    /// <summary>
    /// Démarre le couplage du perso actuellement connecté. Le secret est généré
    /// localement et hashé pour le serveur. L'URL de confirmation est ouverte
    /// dans le navigateur et le plugin poll en arrière-plan jusqu'à obtenir
    /// le token, puis le sauvegarde dans la config.
    /// </summary>
    internal static async Task StartCharacterLinkAsync()
    {
        // Lit les données du perso sur le framework thread.
        var (name, worldId, worldName, contentId) = await Framework.RunOnFrameworkThread(() =>
        {
            var p = ObjectTable.LocalPlayer;
            if (p == null) return (string.Empty, 0, string.Empty, 0UL);
            return (p.Name.TextValue, (int)p.HomeWorld.RowId, p.HomeWorld.Value.Name.ToString(), GetLocalContentId());
        });

        if (string.IsNullOrWhiteSpace(name) || worldId <= 0)
        {
            ChatGui.PrintError("[Eorzea Events] Aucun personnage connecté. Connectez-vous in-game et réessayez.");
            return;
        }

        // Génère un secret cryptographiquement aléatoire (32 bytes) + son hash SHA256.
        var secretBytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(secretBytes);
        var plainSecret = Convert.ToHexString(secretBytes).ToLowerInvariant();
        var hashedSecret = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(plainSecret))
        ).ToLowerInvariant();

        var req = new LinkStartRequest
        {
            CharacterName = name,
            WorldId       = worldId,
            WorldName     = worldName,
            ContentId     = contentId != 0 ? contentId.ToString() : null,
            HashedSecret  = hashedSecret,
        };

        var resp = await Api.StartLinkAsync(req);
        if (resp == null)
        {
            ChatGui.PrintError("[Eorzea Events] Impossible de démarrer le couplage (le serveur a refusé la requête).");
            return;
        }

        DateTime expiresAt;
        if (!DateTime.TryParse(resp.ExpiresAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out expiresAt))
            expiresAt = DateTime.UtcNow.AddMinutes(10);

        // On construit l'URL de confirmation localement à partir du BaseUrl configuré
        // (dev/prod selon Config.BaseUrl). On ignore resp.LinkUrl qui dépend des
        // variables d'environnement côté serveur et peut pointer ailleurs.
        var linkUrl = Config.BaseUrl.TrimEnd('/') + "/plugin/link/" + resp.SessionId;

        ActiveLinkState = new CharacterLinkState
        {
            CharacterName = name,
            WorldId       = worldId,
            WorldName     = worldName,
            LinkUrl       = linkUrl,
            ExpiresAt     = expiresAt,
            Status        = "pending",
        };

        // Ouvre la page de confirmation dans le navigateur de l'utilisateur.
        try { Dalamud.Utility.Util.OpenLink(linkUrl); }
        catch (Exception ex) { Log.Warning(ex, "[EorzeaEvents] OpenLink échoué — l'utilisateur devra copier l'URL manuellement."); }

        ChatGui.Print(new SeStringBuilder()
            .AddUiForeground(45) // bleu
            .AddText("[Eorzea Events] ")
            .AddUiForegroundOff()
            .AddText($"Ouverture de la page de confirmation pour {name}@{worldName}. Validez dans votre navigateur.")
            .Build());

        // Poll en arrière-plan : 5 s d'intervalle pendant max 10 min.
        const int maxAttempts = 600 / 5;
        var consecutiveErrors = 0;
        for (var i = 0; i < maxAttempts; i++)
        {
            if (DateTime.UtcNow >= expiresAt)
            {
                ActiveLinkState.Status = "expired";
                ChatGui.PrintError("[Eorzea Events] Session de couplage expirée.");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            var (result, payload) = await Api.PollLinkAsync(resp.SessionId, plainSecret);
            if (result == LinkPollResult.Pending)
            {
                consecutiveErrors = 0;
                continue;
            }
            if (result == LinkPollResult.Error)
            {
                consecutiveErrors++;
                Log.Warning($"[EorzeaEvents] Poll link erreur #{consecutiveErrors} (sessionId={resp.SessionId[..8]}…)");
                if (consecutiveErrors >= 3)
                    ActiveLinkState.ErrorMessage = "Erreur de communication avec le serveur";
                continue;
            }
            if (result == LinkPollResult.Bound && !string.IsNullOrWhiteSpace(payload?.Token))
            {
                // On a le token — on le sauvegarde dans la config. On ancre sur le
                // ContentId (stable) avec repli sur name+world pour les entrées legacy.
                var existing = Config.FindCharacterTokenByContentId(contentId)
                            ?? Config.FindCharacterToken(name, worldId);
                if (existing != null)
                {
                    existing.CharacterName = name;
                    existing.WorldId = worldId;
                    existing.WorldName = worldName;
                    existing.ContentId = contentId;
                    existing.Token = payload.Token!;
                    existing.LinkedAt = DateTime.UtcNow;
                }
                else
                {
                    Config.CharacterTokens.Add(new CharacterTokenEntry
                    {
                        CharacterName = name,
                        WorldId       = worldId,
                        WorldName     = worldName,
                        ContentId     = contentId,
                        Token         = payload.Token!,
                        LinkedAt      = DateTime.UtcNow,
                    });
                }
                Config.Save();
                _lastTokenAppliedKey = null; // force re-sélection au prochain tick

                ActiveLinkState.Status = "bound";
                ChatGui.Print(new SeStringBuilder()
                    .AddUiForeground(43) // vert
                    .AddText("[Eorzea Events] ")
                    .AddUiForegroundOff()
                    .AddText($"Personnage {name}@{worldName} lié avec succès.")
                    .Build());
                return;
            }
            if (result == LinkPollResult.Expired)
            {
                ActiveLinkState.Status = "expired";
                ChatGui.PrintError("[Eorzea Events] Session de couplage expirée ou déjà utilisée.");
                return;
            }
            // result == Error → on retente au prochain tick
        }

        ActiveLinkState.Status = "expired";
        ChatGui.PrintError("[Eorzea Events] Délai dépassé sans confirmation.");
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

        if ((now - _lastVersionCheck).TotalSeconds >= VersionCheckIntervalSeconds)
        {
            _lastVersionCheck = now;
            Task.Run(async () => await CheckMinimumVersionAsync());
        }

        if (IsBlocked)
        {
            _sessionWindow?.PollSessionStatus();
            return;
        }

        // Sessions RP (5 s). Volontairement inconditionnel : ce relevé alimente
        // aussi la barre de statut et la liste de la fenêtre, qui se figeaient
        // dès qu'on coupait les notifications.
        if ((now - _lastNotifCheck).TotalSeconds >= 5)
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

        // Maintient le token Bearer en phase avec le perso connecté in-game.
        // (lit ObjectTable, donc doit rester sur le framework thread)
        EnsureTokenForActivePlayer();

        // Heartbeat (60 s) — déclenché dès qu'on a au moins un token (legacy OU perso)
        var hasAnyToken = !string.IsNullOrWhiteSpace(Config.ApiToken) || Config.CharacterTokens.Count > 0;
        if (hasAnyToken
            && (now - _lastHeartbeat).TotalSeconds >= HeartbeatIntervalSeconds)
        {
            _lastHeartbeat = now;
            var territory = ClientState.TerritoryType;
            var world     = ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString();
            var charName  = ObjectTable.LocalPlayer?.Name.TextValue;
            var contentId = GetLocalContentId();
            var housing   = ClientState.IsLoggedIn ? GetCurrentHousingForHeartbeat() : null;
            Task.Run(async () =>
            {
                await Api.HeartbeatAsync(
                    version:       VersionLabel(),
                    territoryId:   territory > 0 ? territory : null,
                    worldName:     !string.IsNullOrWhiteSpace(world) ? world : null,
                    ward:          housing?.Ward,
                    plot:          housing?.Plot,
                    room:          housing?.Room,
                    characterName: !string.IsNullOrWhiteSpace(charName) ? charName : null,
                    contentId:     contentId != 0 ? contentId.ToString() : null);
                CheckTokenValidity();
            });
        }

        // Réinitialise le flag si le token a été renouvelé et est redevenu valide
        if (_tokenInvalidNotified && Api.IsTokenValid)
            _tokenInvalidNotified = false;

        // Sessions de l'utilisateur courant (30 s) — déclenché dès qu'on a un token quelconque
        if (hasAnyToken
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
            {
                var housing = GetCurrentHousingForHeartbeat();
                Task.Run(async () => await Api.PresenceHeartbeatAsync(
                    territory, world, Config.ClientId,
                    ward: housing?.Ward, plot: housing?.Plot, room: housing?.Room));
            }
        }

        // Polling session active (fenêtre ouverte ou non)
        _sessionWindow?.PollSessionStatus();

        // Mise à jour automatique de la position (5 min, silencieuse, sans propagation Discord)
        _sessionWindow?.AutoRefreshPositionIfDue();

        // Disponibilités RP. Le rafraîchissement n'est plus conditionné à
        // l'indicateur de nameplate : la page « Autour de moi » et le menu
        // contextuel s'appuient sur la même liste, et resteraient vides pour qui
        // a désactivé le marqueur.
        if ((now - _lastAvailabilityCheck).TotalSeconds >= AvailabilityPollIntervalSeconds)
        {
            _lastAvailabilityCheck = now;
            Task.Run(async () => await RefreshAvailablePlayersAsync());
        }

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

                // Anti-spam : tant que la session n'a pas atteint le délai serveur (5 min),
                // on ne notifie pas ET on ne la marque pas connue → ré-évaluée au prochain poll.
                if (!session.NotifyEligible) continue;

                var isNearby = currentWorld != null && CurrentZone != null
                    && session.Server == currentWorld && session.Location == CurrentZone;

                // Filtre langue (si activé, ignore les sessions dont la locale ne correspond pas)
                if (Config.NotifyRpLanguageFilter)
                {
                    var sessionLang = session.Author?.Locale;
                    if (sessionLang != null)
                    {
                        var pluginLang = L == Loc.Fr ? "fr" : "en";
                        if (sessionLang != pluginLang) continue;
                    }
                }

                // Filtre « mon monde ». Sans objet pour une session de ma zone,
                // qui est par construction sur mon monde.
                if (!isNearby && Config.NotifyMyWorld
                    && currentWorld != null && session.Server != currentWorld) continue;

                // Un seul bandeau à l'écran : celui de proximité, avec son et
                // style doré, remplace le bandeau générique quand il s'applique.
                if (isNearby && Config.NotifyNearbyZone)
                {
                    ToastGui.ShowQuest(
                        string.Format(L.NotifNearbyRp, session.Title),
                        new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true, DisplayCheckmark = false });
                }
                else if (Config.NotifyRpLiveScreen)
                {
                    ToastGui.ShowNormal(
                        string.Format(L.NotifNewRpScreen, session.Title, session.Location, session.Server),
                        new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow });
                }

                // Bulle Dalamud et message de chat s'appliquent dans les deux
                // cas. Ils étaient auparavant enfermés dans la branche « pas
                // dans ma zone », si bien qu'une session ouverte à côté de soi,
                // le cas le plus intéressant, n'écrivait rien dans le chat.
                if (Config.NotifyRpLive)
                {
                    // La valeur de retour était jetée : la conserver permet
                    // de rendre la bulle cliquable, ce qui évite d'avoir à
                    // retrouver la session à la main.
                    var active = NotificationMgr.AddNotification(new Notification
                    {
                        Title           = L.NotifNewRpTitle,
                        Content         = $"{session.Title} - {session.Location} ({session.Server})",
                        Type            = NotificationType.Info,
                        InitialDuration = TimeSpan.FromSeconds(6),
                    });

                    active.Click += _ =>
                    {
                        _mainWindow?.OpenAt("rp");
                        active.DismissNow();
                    };
                }

                if (Config.NotifyRpLiveChat)
                    ChatGui.Print(new SeStringBuilder()
                        .AddUiForeground(32)
                        .AddText("[Eorzea Events] ")
                        .AddUiForegroundOff()
                        .AddText(string.Format(L.NotifNewRpChat, session.Title, session.Location, session.Server))
                        .Build());
            }

            // Marquer connues : les sessions déjà connues encore présentes + toutes celles
            // désormais éligibles (notifiées ou écartées par un filtre). Les sessions trop
            // jeunes restent « inconnues » pour être ré-évaluées une fois le délai écoulé.
            _knownSessionIds = sessions
                .Where(s => _knownSessionIds.Contains(s.Id) || s.NotifyEligible)
                .Select(s => s.Id)
                .ToHashSet();
        }
        catch { /* silencieux */ }
    }

    private async Task CheckOngoingEventsAsync()
    {
        try
        {
            var events = await Api.GetUpcomingEventsAsync(1);
            var now = DateTime.UtcNow;
            var visibleEvents = events.Where(e => IsVisibleEventForNotifications(e, now)).ToList();
            var ongoingEvents = visibleEvents.Where(e => IsOngoingEvent(e, now)).ToList();
            var ongoingKeys = ongoingEvents.Select(GetEventOccurrenceKey).ToHashSet();

            SetDtrEvents(ongoingEvents.Count);

            if (!_eventsNotifInitialized)
            {
                _knownOngoingEventKeys = ongoingKeys;
                _eventsNotifInitialized = true;
                return;
            }

            if (Config.NotifyEventStartDalamud || Config.NotifyEventStartChat)
            {
                foreach (var ev in ongoingEvents)
                {
                    var key = GetEventOccurrenceKey(ev);
                    if (_knownOngoingEventKeys.Contains(key)) continue;
                    NotifyEventStarted(ev);
                }
            }

            _knownOngoingEventKeys = ongoingKeys;
        }
        catch { /* silencieux */ }
    }

    private static bool IsVisibleEventForNotifications(EventDto ev, DateTime utcNow)
    {
        if (ev.IsOfficial) return false;
        if (ev.Cancelled)  return false;
        if (Config.HiddenEventIds.Contains(ev.Id)) return false;
        if (!string.IsNullOrEmpty(ev.Establishment?.Id) && Config.HiddenEstablishmentIds.Contains(ev.Establishment.Id))
            return false;
        return !IsExpiredEvent(ev, utcNow);
    }

    private static bool IsOngoingEvent(EventDto ev, DateTime utcNow)
    {
        if (ev.Cancelled) return false;
        if (!DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            return false;
        if (utcNow < start) return false;
        DateTime end;
        if (string.IsNullOrEmpty(ev.EndDate) || !DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out end))
            end = start.AddHours(3);
        return utcNow <= end;
    }

    private static bool IsExpiredEvent(EventDto ev, DateTime utcNow)
    {
        if (!DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
            return false;
        if (string.IsNullOrEmpty(ev.EndDate) || !DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
            end = start.AddHours(3);
        return end < utcNow;
    }

    private static string GetEventOccurrenceKey(EventDto ev)
        => $"{ev.Id}:{ev.StartDate}";

    private static string GetEventChatContent(EventDto ev)
    {
        var venueName = !string.IsNullOrWhiteSpace(ev.Establishment?.Name)
            ? ev.Establishment.Name
            : ev.Title;

        var parts = new List<string> { $"{venueName} — {ev.Title}" };

        if (DateTime.TryParse(ev.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
        {
            var timeRange = start.ToLocalTime().ToString("HH:mm");
            if (!string.IsNullOrWhiteSpace(ev.EndDate)
                && DateTime.TryParse(ev.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
            {
                timeRange += $" → {end.ToLocalTime():HH:mm}";
            }
            parts.Add(timeRange);
        }

        if (!string.IsNullOrWhiteSpace(ev.Establishment?.Server))
            parts.Add(ev.Establishment.Server);

        var address = GetEventAddress(ev);
        if (!string.IsNullOrWhiteSpace(address))
            parts.Add(address);

        return string.Format(L.NotifEventStartChat, string.Join(" | ", parts));
    }

    private static string GetEventScreenContent(EventDto ev)
    {
        var venueName = !string.IsNullOrWhiteSpace(ev.Establishment?.Name)
            ? ev.Establishment.Name
            : ev.Title;
        return string.Format(L.NotifEventStartScreen, ev.Title, venueName);
    }

    private static string? GetEventAddress(EventDto ev)
    {
        if (ev.Establishment == null)
            return null;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(ev.Establishment.District))
        {
            var district = L.DistrictLabels.TryGetValue(ev.Establishment.District, out var label)
                ? label
                : ev.Establishment.District;
            parts.Add(district);
        }

        if (ev.Establishment.Ward.HasValue)
            parts.Add(string.Format(L.HousingWard, ev.Establishment.Ward.Value));

        if (ev.Establishment.Plot.HasValue)
            parts.Add($"{L.FieldPlot} {ev.Establishment.Plot.Value}");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    private static void NotifyEventStarted(EventDto ev)
    {
        if (Config.NotifyEventStartDalamud)
        {
            ToastGui.ShowNormal(
                GetEventScreenContent(ev),
                new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow });
        }

        if (Config.NotifyEventStartChat)
        {
            ChatGui.Print(new SeStringBuilder()
                .AddUiForeground(32)
                .AddText("[Eorzea Events] ")
                .AddUiForegroundOff()
                .AddText(GetEventChatContent(ev))
                .Build());
        }
    }

    // ── RP Availability nameplate ─────────────────────────────────────────────

    private static async Task RefreshAvailablePlayersAsync()
    {
        try
        {
            var entries = await Api.GetRpAvailabilitiesAsync();

            // La liste brute est conservée : les nameplates n'ont besoin que du
            // niveau et du mode d'approche, mais la page « Autour de moi » et le
            // menu contextuel veulent la fiche entière.
            AvailableEntries = entries;

            // GroupBy plutôt que ToDictionary : deux personnages homonymes sur le
            // même monde lèveraient sur clé dupliquée, et le catch silencieux
            // laisserait alors la liste vide sans que rien ne le signale.
            _availablePlayers = entries
                .GroupBy(e => (e.CharacterName, e.Server.ToLowerInvariant()))
                .ToDictionary(
                    g => g.Key,
                    g => (g.First().Profile?.RpLevel, g.First().Profile?.ApproachMode));
        }
        catch { /* silencieux */ }
    }

    /// <summary>
    /// Joueur disponible correspondant au nom et au monde donnés, ou null.
    ///
    /// Volontairement limité à la liste publique des disponibilités : le plugin
    /// ne cherche jamais un personnage par son nom côté serveur, ce qui
    /// reviendrait à exposer un annuaire de joueurs.
    /// </summary>
    internal static Api.RpAvailabilityEntryDto? FindAvailableEntry(string? name, string? world)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(world)) return null;

        return AvailableEntries.FirstOrDefault(e =>
            string.Equals(e.CharacterName, name, StringComparison.Ordinal)
            && string.Equals(e.Server, world, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ajoute « Voir la fiche RP » au menu contextuel d'un joueur.
    ///
    /// L'entrée n'apparaît que pour les joueurs figurant déjà dans la liste
    /// publique des disponibilités, à laquelle ils ont consenti. C'est une
    /// contrainte, pas une simplification : résoudre un nom arbitraire côté
    /// serveur reviendrait à offrir un annuaire de joueurs, ce que
    /// <c>src/lib/rp-relations.ts</c> proscrit explicitement. Effet de bord
    /// heureux, un clic droit sur quelqu'un qui n'a pas consenti ne révèle même
    /// pas qu'il possède une fiche.
    /// </summary>
    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (args.Target is not MenuTargetDefault target) return;
        if (string.IsNullOrEmpty(target.TargetName)) return;

        // Même rapprochement nom + monde que les nameplates.
        var entry = FindAvailableEntry(target.TargetName, target.TargetHomeWorld.Value.Name.ToString());
        if (entry == null) return;

        args.AddMenuItem(new MenuItem
        {
            Name        = L.MenuViewRpProfile,
            PrefixChar  = 'E',
            PrefixColor = 52, // même teinte que le titre de nameplate
            OnClicked   = _ => OpenRpProfileViewer(entry),
        });
    }

    private void OnNamePlateUpdate(
        INamePlateUpdateContext context,
        IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!Config.ShowRpAvailableIndicator || _availablePlayers.Count == 0) return;

        foreach (var handler in handlers)
        {
            if (handler.NamePlateKind != NamePlateKind.PlayerCharacter) continue;

            var character = handler.PlayerCharacter;
            if (character == null) continue;

            var name  = character.Name.TextValue;
            var world = character.HomeWorld.Value.Name.ToString().ToLowerInvariant();
            if (!_availablePlayers.TryGetValue((name, world), out var data)) continue;

            var l = Plugin.L;

            // Genre : Customize[1] → 0 = masculin, 1 = féminin
            var isFemale = character.Customize[1] != 0;

            var qualifier = data.ApproachMode switch
            {
                "come_to_me" => " - " + l.RpNameplateTimide,
                "i_approach" => " - " + (isFemale ? l.RpNameplateExtravertie : l.RpNameplateExtraverti),
                _            => string.Empty,
            };

            handler.TitleParts.Text = new SeStringBuilder()
                .AddText(l.RpNameplateBase + qualifier)
                .Build();
            handler.TitleParts.TextWrap = (
                new SeStringBuilder().AddUiForeground(52).Build(),
                new SeStringBuilder().AddUiForegroundOff().Build());
            handler.DisplayTitle  = true;
            handler.IsPrefixTitle = false;
        }
    }

    // ── RP Availability helpers ───────────────────────────────────────────────

    /// <summary>
    /// Doit être appelé sur le framework thread — lit LocalPlayer puis lance la tâche.
    /// </summary>
    internal static void ActivateRpAvailability()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null) return;
        var req = new Api.SetRpAvailableRequest
        {
            CharacterName = player.Name.TextValue,
            Server        = player.HomeWorld.Value.Name.ToString(),
            Zone          = CurrentZone,
            TerritoryId   = (int)ClientState.TerritoryType > 0 ? (int?)ClientState.TerritoryType : null,
        };
        // Capturer le profil local pour le synchro simultanément
        Api.SaveRpProfileRequest? profileReq = null;
        if (!string.IsNullOrEmpty(Config.RpProfileLevel) && !string.IsNullOrEmpty(Config.RpProfileApproachMode))
        {
            var langs = new List<string>();
            if (Config.RpProfileLanguages?.Contains("\"fr\"") == true) langs.Add("fr");
            if (Config.RpProfileLanguages?.Contains("\"en\"") == true) langs.Add("en");
            if (langs.Count == 0) langs.Add("fr");

            profileReq = new Api.SaveRpProfileRequest
            {
                RpLevel       = Config.RpProfileLevel,
                ApproachMode  = Config.RpProfileApproachMode,
                Languages     = [.. langs],
                ContactMode   = Config.RpProfileContactMode,
                SessionLength = Config.RpProfileSessionLength,
            };
        }

        Task.Run(async () =>
        {
            // Synchro profil avant activation pour que le GET retourne les données complètes
            if (profileReq != null)
                await Api.SaveRpProfileAsync(profileReq);

            var ok = await Api.SetRpAvailableAsync(req);
            if (ok)
            {
                CurrentCharacterAvailable = true;
                Config.Save();
                _ = Framework.RunOnFrameworkThread(UpdateDtrRpAvail);
            }
        });
    }

    // Gardé pour compat avec les appels Task.Run existants dans ConfigWindow
    internal static async Task SetRpAvailableFromLocalPlayerAsync()
    {
        await Framework.RunOnFrameworkThread(ActivateRpAvailability);
    }

    internal static async Task ClearRpAvailabilityAsync()
    {
        await Api.ClearRpAvailabilityAsync();
        CurrentCharacterAvailable = false;
        Config.Save();
        _ = Framework.RunOnFrameworkThread(UpdateDtrRpAvail);
    }

    internal static void DismissLoginPrompt() => LoginPromptPending = false;

    private void OnLogin()
    {
        DoFirstRunCheck();
        _tokenInvalidNotified = false;
        if (Config.RpAskOnLogin && CurrentCharacterAvailable)
            LoginPromptPending = true;
    }

    private void CheckTokenValidity()
    {
        if (Api.IsTokenValid || _tokenInvalidNotified) return;
        if (!ClientState.IsLoggedIn) return;
        _tokenInvalidNotified = true;

        // Un jeton invalide se règle en relançant le couplage : la bulle y mène
        // directement plutôt que de laisser l'utilisateur chercher.
        var notification = NotificationMgr.AddNotification(new Notification
        {
            Title           = L.NotifTokenTitle,
            Content         = L.NotifTokenContent,
            Type            = NotificationType.Warning,
            InitialDuration = TimeSpan.FromSeconds(12),
        });

        notification.Click += _ =>
        {
            OpenSetup(tokenInvalid: true);
            notification.DismissNow();
        };

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

            var current      = PluginInterface.Manifest.AssemblyVersion;
            var currentLabel = VersionLabel();
            var minimumStr   = PluginInterface.IsTesting ? info.TestingMinimum : info.Minimum;
            var updateUrl    = string.IsNullOrWhiteSpace(info.UpdateUrl)
                ? Config.BaseUrl.TrimEnd('/') + "/plugin"
                : info.UpdateUrl.Trim();

            if (info.EmergencyBlock)
            {
                ApplyBlockedState(
                    PluginGateMode.EmergencyBlock,
                    info.Message,
                    updateUrl,
                    $"[EorzeaEvents] Plugin bloqué via kill-switch serveur — version {currentLabel}");
                return;
            }

            if (!Version.TryParse(minimumStr, out var minimum))
            {
                ClearBlockedState();
                return;
            }

            if (current < minimum)
            {
                var defaultMessage =
                    $"Le plugin nécessite une mise à jour.\n\n" +
                    $"Version installée : {currentLabel}\n" +
                    $"Version minimale  : {minimum.Major}.{minimum.Minor}.{minimum.Build}\n\n" +
                    $"Ouvre le gestionnaire de plugins pour mettre à jour Eorzea Events.";
                ApplyBlockedState(
                    PluginGateMode.UpdateRequired,
                    string.IsNullOrWhiteSpace(info.Message) ? defaultMessage : info.Message,
                    updateUrl,
                    $"[EorzeaEvents] Plugin bloqué — version {current} < minimum {minimum}");
                return;
            }

            ClearBlockedState();
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

    private static string? ResolveTerritoryName(uint territoryId)
    {
        var sheet = DataManager.GetExcelSheet<TerritoryType>();
        var row   = sheet?.GetRowOrDefault(territoryId);
        return row?.PlaceName.Value.Name.ToString();
    }

    private void OnTerritoryChanged(uint territory)
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
        NamePlateGui.OnNamePlateUpdate          -= OnNamePlateUpdate;
        ClientState.Login                       -= OnLogin;
        ContextMenu.OnMenuOpened                -= OnMenuOpened;
        PluginInterface.UiBuilder.Draw         -= _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi   -= OpenMain;
        CommandManager.RemoveHandler(CommandMain);
        _dtrRp?.Remove();
        _dtrEvents?.Remove();
        _dtrRpAvail?.Remove();

        // Après le retrait de UiBuilder.Draw : plus aucune frame ne peut
        // référencer l'atlas ni les textures pendant leur libération.
        _windowSystem.RemoveAllWindows();
        _mainWindow?.Dispose();
        _estabDetailWindow?.Dispose();
        Ui.Fonts.Dispose();
        Ui.Textures.Dispose();

        Api.Dispose();
    }

    private static void ApplyBlockedState(PluginGateMode mode, string? message, string updateUrl, string logMessage)
    {
        _gateMode = mode;
        IsBlocked = true;
        BlockedMessage = string.IsNullOrWhiteSpace(message)
            ? "Le plugin est temporairement bloqué."
            : message.Trim();
        BlockedUpdateUrl = updateUrl;

        if (_sessionWindow != null) _sessionWindow.IsOpen = false;
        if (_setupWindow != null) _setupWindow.IsOpen = false;
        if (_mainWindow != null) _mainWindow.IsOpen = true;

        Log.Warning(logMessage);
    }

    private static void ClearBlockedState()
    {
        _gateMode = PluginGateMode.None;
        IsBlocked = false;
        BlockedMessage = string.Empty;
        BlockedUpdateUrl = string.Empty;
    }

    private static unsafe (int Ward, int? Plot, int? Room)? GetCurrentHousingForHeartbeat()
    {
        var hm = HousingManager.Instance();
        if (hm == null) return null;
        var rawWard = hm->GetCurrentWard();
        if (rawWard < 0) return null;
        var rawPlot = hm->GetCurrentPlot();
        var rawRoom = hm->GetCurrentRoom();
        return (
            Ward: rawWard + 1,
            Plot: rawPlot >= 0 ? rawPlot + 1 : null,
            Room: rawRoom > 0 ? rawRoom : null
        );
    }
}

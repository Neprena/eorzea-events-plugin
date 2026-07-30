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
    private static   PortraitZoomWindow?   _portraitZoomWindow;

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
    /// Déclare ou retire la disponibilité RP du personnage connecté.
    ///
    /// Seul chemin d'écriture : la barre de statut, la fiche RP, les réglages et
    /// la fenêtre principale passent tous par ici. Ils avaient chacun leur
    /// version, avec trois comportements différents, et surtout deux d'entre eux
    /// oubliaient de rafraîchir la barre de statut : elle affichait donc l'état
    /// précédent, et le clic suivant faisait l'inverse de ce qu'on croyait.
    ///
    /// L'état local est écrit tout de suite pour que l'interface réagisse au
    /// geste, puis rétabli si le serveur refuse. Les échecs sont dits dans le
    /// chat : sans personnage lié, la requête part en 401 et il ne se passait
    /// rien, sans un mot.
    ///
    /// À appeler depuis le framework thread : lit le joueur local.
    /// </summary>
    internal static void SetRpAvailability(bool available)
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null || CurrentCharacter is not { } character)
        {
            ChatGui.PrintError($"[Eorzea Events] {L.RpAvailableNoCharacter}");
            return;
        }

        if (Config.FindCharacterToken(character.Name, character.WorldId) == null
            && string.IsNullOrWhiteSpace(Config.ApiToken))
        {
            ChatGui.PrintError($"[Eorzea Events] {L.RpAvailableNoToken}");
            return;
        }

        var request = new Api.SetRpAvailableRequest
        {
            CharacterName = player.Name.TextValue,
            Server        = player.HomeWorld.Value.Name.ToString(),
            Zone          = CurrentZone,
            TerritoryId   = (int)ClientState.TerritoryType > 0
                                ? (int)ClientState.TerritoryType
                                : null,
        };

        var previous = CurrentCharacterAvailable;
        CurrentCharacterAvailable = available;
        _availabilityTouchedAt    = DateTime.UtcNow;
        UpdateDtrRpAvail();

        // Aucune synchronisation de fiche ici. Se déclarer disponible envoyait
        // jusqu'ici un PUT de fiche construit à partir de la seule config locale,
        // or ce PUT remplace la fiche entière côté serveur : tout ce qui ne se
        // règle pas en jeu (portrait, identité, apparence, personnalité, histoire,
        // limites, thèmes) repartait à vide. Le serveur a déjà le niveau et le
        // mode d'approche, il n'a pas besoin qu'on les lui redonne.
        Task.Run(async () =>
        {
            var ok = available
                ? await Api.SetRpAvailableAsync(request)
                : await Api.ClearRpAvailabilityAsync();

            await Framework.RunOnFrameworkThread(() =>
            {
                // Réaffirmé même en cas de succès : la réponse arrive après le
                // clic, et l'état ne doit pas dépendre de ce que la liste publique
                // savait entre les deux.
                CurrentCharacterAvailable = ok ? available : previous;
                _availabilityTouchedAt    = DateTime.UtcNow;

                if (!ok) ChatGui.PrintError($"[Eorzea Events] {L.RpAvailableFailed}");

                UpdateDtrRpAvail();
            });
        });
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

    /// <summary>
    /// État et personnage sur lesquels l'entrée de disponibilité a été peinte.
    ///
    /// Le constructeur du plugin tourne hors du thread de jeu, où
    /// <see cref="CurrentCharacter"/> vaut toujours null : l'entrée démarrait donc
    /// systématiquement sur « indisponible », et rien ne la reprenait ensuite. Ces
    /// deux témoins permettent de la redessiner dès que l'état réel diffère,
    /// changement de personnage compris.
    /// </summary>
    private static bool?                  _dtrRpAvailShown;
    private static (string Name, int WorldId)? _dtrRpAvailCharacter;

    /// <summary>
    /// Dernier changement de disponibilité demandé par le joueur, et dernière
    /// liste publique reçue.
    ///
    /// La liste ne devient une source de vérité qu'une fois établie après le
    /// changement : sinon, la frame qui suit le clic voit un personnage encore
    /// absent de la liste et défait aussitôt ce que le joueur vient de demander,
    /// l'état ne se posant qu'au rafraîchissement suivant.
    /// </summary>
    private static DateTime _availabilityTouchedAt = DateTime.MinValue;
    private static DateTime _availabilityListAt    = DateTime.MinValue;

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
    /// <summary>
    /// Intervalle du contrôle de version.
    ///
    /// Il était de dix secondes, pour une valeur qui change quelques fois par an :
    /// chaque client interrogeait donc le site six fois par minute. Cinq minutes
    /// suffisent largement, y compris pour un blocage d'urgence, qui n'a pas
    /// besoin de prendre effet à la seconde près.
    /// </summary>
    private const int VersionCheckIntervalSeconds = 300;

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
        _rpProfileWindow    = new RpProfileWindow();
        _announcementWindow = new RpAnnouncementWindow(Config);
        _whatsNewWindow     = new WhatsNewWindow(Config);
        _portraitZoomWindow = new PortraitZoomWindow();
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_sessionWindow);
        _windowSystem.AddWindow(_setupWindow);
        _windowSystem.AddWindow(_estabDetailWindow);
        _windowSystem.AddWindow(_rpProfileWindow);
        _windowSystem.AddWindow(_announcementWindow);
        _windowSystem.AddWindow(_whatsNewWindow);
        _windowSystem.AddWindow(_portraitZoomWindow);

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
        // Même chemin que le toggle de la fiche RP et des réglages, gardes et
        // messages d'erreur compris.
        _dtrRpAvail.OnClick = _ => SetRpAvailability(!CurrentCharacterAvailable);
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
                // Null vaut « je n'ai pas pu demander » : on garde la liste
                // connue, sans quoi une panne ferait disparaître le bouton de
                // reprise de session.
                if (await Api.GetMySessionIdsAsync() is { } ids) MySessionIds = ids;
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

    /// <summary>
    /// Portrait RP en grand. Pas de garde <see cref="IsBlocked"/> ici : la fenêtre
    /// n'affiche qu'une image déjà chargée et n'ouvre aucun accès à l'API.
    /// </summary>
    internal static void OpenPortraitZoom(string portraitUrl, string characterName) =>
        _portraitZoomWindow?.Open(portraitUrl, characterName);

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

    /// <summary>
    /// Losange et libellé de l'entrée « disponibilité » de la barre de statut.
    ///
    /// Le libellé accompagne le symbole : seul, le losange n'apprenait rien sans
    /// passer la souris dessus, et il ne rappelait pas le vocabulaire du plugin.
    /// </summary>
    internal static void UpdateDtrRpAvail()
    {
        if (_dtrRpAvail == null) return;

        var available = CurrentCharacterAvailable;
        var sb        = new SeStringBuilder();

        sb.AddUiGlow(available ? (ushort)52 : GlowIdle);
        sb.AddText(available ? "♦ " : "◇ ");
        sb.AddText(L.DtrRpAvailLabel);
        sb.AddUiGlowOff();

        _dtrRpAvail.Text = sb.Build();

        _dtrRpAvailShown     = available;
        _dtrRpAvailCharacter = CurrentCharacter;
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

    // ─── Amis RP ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Personnages à qui la fiche du personnage courant est ouverte.
    ///
    /// Liste d'accès et non liste sociale : elle n'apprend rien sur les autres,
    /// et le serveur ne dit jamais qui nous a ajouté. Gardée en mémoire parce que
    /// le menu contextuel s'exécute sur le thread de jeu et doit répondre sans
    /// attendre le réseau. Seul son propriétaire la modifie, donc aucun sondage
    /// périodique : elle est relue à la connexion, au changement de personnage et
    /// après chaque ajout ou retrait.
    /// </summary>
    internal static IReadOnlyList<Api.RpFriendDto> Friends { get; private set; } = [];

    private static HashSet<string> _friendIds     = [];
    private static HashSet<string> _friendHashes  = [];
    private static string?         _friendsLoadedFor;

    /// <summary>Ce personnage figure-t-il dans ma liste d'accès ?</summary>
    internal static bool IsFriend(string? characterId) =>
        characterId is { Length: > 0 } id && _friendIds.Contains(id);

    /// <summary>
    /// Variante par ContentId haché, seule information dont dispose le menu
    /// contextuel sur un joueur visé.
    /// </summary>
    internal static bool IsFriendByContentId(ulong contentId) =>
        contentId != 0 && _friendHashes.Contains(HashContentId(contentId));

    /// <summary>
    /// SHA256 de l'identifiant, dans la même forme que le serveur (chaîne
    /// décimale, hexadécimal minuscule). L'identifiant brut d'un tiers ne quitte
    /// jamais la machine : seul son haché est transmis.
    /// </summary>
    internal static string HashContentId(ulong contentId)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(contentId.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Recharge la liste depuis le serveur, en gardant l'ancienne si l'appel échoue.</summary>
    internal static void RefreshFriends(bool force = false)
    {
        if (CurrentCharacter is not { } character) return;

        var key = Configuration.CharacterKey(character.Name, character.WorldId);
        if (!force && _friendsLoadedFor == key) return;
        _friendsLoadedFor = key;

        Task.Run(async () =>
        {
            var friends = await Api.GetRpFriendsAsync();
            if (friends == null) return; // échec réseau : on garde ce qu'on a

            await Framework.RunOnFrameworkThread(() =>
            {
                Friends       = friends;
                _friendIds    = [.. friends.Select(f => f.CharacterId)];
                _friendHashes = [.. friends
                    .Select(f => f.ContentIdHash)
                    .Where(h => !string.IsNullOrEmpty(h))
                    .Select(h => h!)];
            });
        });
    }

    /// <summary>
    /// Ouvre sa fiche à un personnage, désigné par son identifiant serveur ou par
    /// le haché de son ContentId.
    ///
    /// Le message de retour dit ce qui se passe vraiment : l'autre pourra voir
    /// nos sections « amis ». Personne ne l'apprendra de notre part, et cela ne
    /// nous donne aucun accès à sa fiche à lui.
    /// </summary>
    internal static void AddFriend(string? characterId, ulong contentId, string label)
    {
        if (CurrentCharacter is null)
        {
            ChatGui.PrintError($"[Eorzea Events] {L.RpAvailableNoCharacter}");
            return;
        }

        var request = new Api.AddRpFriendRequest
        {
            CharacterId   = characterId is { Length: > 0 } ? characterId : null,
            ContentIdHash = characterId is { Length: > 0 } || contentId == 0
                                ? null
                                : HashContentId(contentId),
        };

        if (request.CharacterId == null && request.ContentIdHash == null) return;

        Task.Run(async () =>
        {
            var result = await Api.AddRpFriendAsync(request);
            await Framework.RunOnFrameworkThread(() =>
            {
                switch (result)
                {
                    case EorzeaEventsPlugin.Api.ApiClient.AddFriendResult.Added:
                        ChatGui.Print(string.Format(L.RpFriendAdded, label));
                        break;
                    case EorzeaEventsPlugin.Api.ApiClient.AddFriendResult.NotFound:
                        ChatGui.PrintError($"[Eorzea Events] {L.RpFriendAddNotFound}");
                        break;
                    case EorzeaEventsPlugin.Api.ApiClient.AddFriendResult.LimitReached:
                        ChatGui.PrintError($"[Eorzea Events] {L.RpFriendAddLimit}");
                        break;
                    case EorzeaEventsPlugin.Api.ApiClient.AddFriendResult.NoCharacterToken:
                        ChatGui.PrintError($"[Eorzea Events] {L.RpFriendNoToken}");
                        break;
                    default:
                        ChatGui.PrintError($"[Eorzea Events] {L.RpFriendAddFailed}");
                        break;
                }

                RefreshFriends(force: true);
            });
        });
    }

    /// <summary>
    /// Aide-mémoire privé attaché à un ami. Personne d'autre ne le lit : ni la
    /// personne concernée, ni le reste du site.
    /// </summary>
    internal static void SetFriendNote(string characterId, string? note)
    {
        Task.Run(async () =>
        {
            await Api.SetRpFriendNoteAsync(characterId, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
            await Framework.RunOnFrameworkThread(() => RefreshFriends(force: true));
        });
    }

    /// <summary>Referme sa fiche à un personnage.</summary>
    internal static void RemoveFriend(string characterId, string label)
    {
        Task.Run(async () =>
        {
            var ok = await Api.RemoveRpFriendAsync(characterId);
            await Framework.RunOnFrameworkThread(() =>
            {
                if (ok) ChatGui.Print(string.Format(L.RpFriendRemoved, label));
                else    ChatGui.PrintError($"[Eorzea Events] {L.RpFriendAddFailed}");

                RefreshFriends(force: true);
            });
        });
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
        sb.AddText($"{L.DtrRpLabel}: ");
        sb.AddUiGlow(count > 0 ? GlowActive : GlowIdle);
        sb.AddText(count.ToString());
        sb.AddUiGlowOff();
        _dtrRp.Text = sb.Build();
    }

    private void SetDtrEvents(int count)
    {
        if (_dtrEvents == null) return;
        var sb = new SeStringBuilder();
        sb.AddText($"{L.DtrEventsLabel}: ");
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
            Task.Run(async () =>
            {
                if (await Api.GetMySessionIdsAsync() is { } ids) MySessionIds = ids;
            });
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

        SyncRpAvailabilityDisplay(now);

        // Liste d'accès : rechargée au premier passage et à chaque changement de
        // personnage, jamais en boucle, puisque seul son propriétaire la modifie.
        RefreshFriends();

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

        // Un appartement n'a pas de parcelle : sans son numéro, l'adresse
        // annoncée s'arrêtait au quartier.
        if (ev.Establishment.ApartmentNumber.HasValue)
            parts.Add($"{L.EstabApartment} {ev.Establishment.ApartmentNumber.Value}");

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

    /// <summary>
    /// Garde l'entrée de la barre de statut en phase avec l'état réel, et remet
    /// l'état local d'aplomb quand la liste publique le contredit.
    ///
    /// Deux situations que rien ne rattrapait : l'entrée peinte au chargement du
    /// plugin, hors du thread de jeu, où aucun personnage n'est visible ; et une
    /// disponibilité retirée depuis le site ou tombée avec le heartbeat, qui
    /// laissait le plugin afficher « disponible » sans l'être.
    ///
    /// Appelé à chaque frame : deux comparaisons, aucun coût. Le redessin n'a lieu
    /// que si l'état affiché ne correspond plus.
    /// </summary>
    private void SyncRpAvailabilityDisplay(DateTime now)
    {
        var character = CurrentCharacter;
        var local     = CurrentCharacterAvailable;

        if (character is { } c
            && (Config.FindCharacterToken(c.Name, c.WorldId) != null
                || !string.IsNullOrWhiteSpace(Config.ApiToken)))
        {
            var onServer = IsLocalPlayerAvailable();

            // La liste ne tranche que si elle a été reçue après le dernier
            // changement demandé, publication comprise. Sans cette condition, la
            // frame qui suit le clic défait le clic.
            var listIsAuthoritative =
                _availabilityListAt > _availabilityTouchedAt + TimeSpan.FromSeconds(2);

            // Et une absence de la liste ne compte que si le serveur a de nos
            // nouvelles : il en écarte les présences sans heartbeat depuis cinq
            // minutes, et la liste est rafraîchie sans lien avec le heartbeat.
            var sinceHeartbeat = now - _lastHeartbeat;
            var trustAbsence   = sinceHeartbeat > TimeSpan.FromSeconds(15)
                              && sinceHeartbeat < TimeSpan.FromMinutes(4);

            if (listIsAuthoritative && onServer != local && (onServer || trustAbsence))
            {
                CurrentCharacterAvailable = onServer;
                local                     = onServer;
            }
        }

        if (_dtrRpAvailShown != local || _dtrRpAvailCharacter != character)
            UpdateDtrRpAvail();
    }

    private static async Task RefreshAvailablePlayersAsync()
    {
        try
        {
            var entries = await Api.GetRpAvailabilitiesAsync();

            // Requête en échec : garder la liste précédente. La remplacer par une
            // liste vide viderait « Autour de moi » et les nameplates, et ferait
            // conclure à tort que le personnage n'est plus déclaré disponible.
            if (entries == null) return;

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

            _availabilityListAt = DateTime.UtcNow;
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

        if (entry != null)
        {
            args.AddMenuItem(new MenuItem
            {
                Name        = L.MenuViewRpProfile,
                PrefixChar  = 'E',
                PrefixColor = 52, // même teinte que le titre de nameplate
                OnClicked   = _ => OpenRpProfileViewer(entry),
            });
        }

        // Ajout en ami : contrairement à la consultation, il ne demande pas que
        // le joueur soit déclaré disponible à cet instant. Le ContentId, lisible
        // sur un joueur qu'on a sous les yeux, suffit au serveur pour retrouver
        // un personnage dont la fiche est visible en jeu ; les autres cas
        // reçoivent le même refus, sans dire s'ils ont un compte.
        var contentId = target.TargetContentId;
        var already   = IsFriendByContentId(contentId)
                     || IsFriend(entry?.Profile?.CharacterId);

        if (contentId == 0 || already) return;
        if (CurrentCharacter is not { } self) return;
        if (string.Equals(target.TargetName, self.Name, StringComparison.Ordinal)) return;

        var label = target.TargetName;
        args.AddMenuItem(new MenuItem
        {
            Name        = L.RpFriendAdd,
            PrefixChar  = 'E',
            PrefixColor = 52,
            OnClicked   = _ => AddFriend(entry?.Profile?.CharacterId, contentId, label),
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

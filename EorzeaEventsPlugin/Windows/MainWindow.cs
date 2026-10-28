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
    /// <summary>
    /// Le dernier chargement a échoué.
    ///
    /// Vider la liste sur une exception faisait passer une panne pour « rien à
    /// afficher » : l'onglet se présentait vide, sans un mot, et effaçait au
    /// passage ce qui était déjà à l'écran. On garde donc le contenu précédent
    /// et on dit ce qui s'est passé.
    /// </summary>
    private bool               _sessionsFailed    = false;
    private DateTime           _sessionsLastFetch = DateTime.MinValue;

    // ─── Événements ───────────────────────────────────────────────────────────

    private List<EventDto> _eventsList      = [];
    private bool           _eventsLoading   = false;
    private bool           _eventsFailed    = false;
    private DateTime       _eventsLastFetch = DateTime.MinValue;

    /// <summary>Filtres de l'agenda, appliqués côté client : l'API ne pagine pas.</summary>
    private string      _eventsQuery  = string.Empty;
    private EventOrigin _eventsOrigin = EventOrigin.All;

    private enum EventOrigin { All, Official, Community }

    // ─── Établissements ───────────────────────────────────────────────────────

    private List<EstablishmentDto>                              _estabList           = [];
    private bool                                                _estabLoading        = false;
    private bool                                                _estabFailed         = false;
    private bool                                                _estabInitialLoaded  = false;
    private string                                              _estabSearchInput    = string.Empty;

#if DEBUG
    private string _debugStatus = string.Empty;
#endif

    // ─────────────────────────────────────────────────────────────────────────

    private static readonly WindowSizeConstraints OpenConstraints = new()
    {
        MinimumSize = new Vector2(780, 560),
        MaximumSize = new Vector2(1100, 1000),
    };

    // ─── Réduction à la barre de titre ────────────────────────────────────────

    /// <summary>Clé de l'animation de repli, entre 0 (ouverte) et 1 (réduite).</summary>
    private const uint FoldKey = 0xEE5D0010;

    /// <summary>La fenêtre est réduite à sa barre, ou va l'être.</summary>
    private bool _folded;

    /// <summary>Position et taille de la fenêtre ouverte, à retrouver en la restaurant.</summary>
    private Vector2 _openPos;
    private Vector2 _openSize;

    /// <summary>Fermée réduite : la taille ouverte est à rétablir à la réouverture.</summary>
    private bool _restoreOnOpen;

    /// <summary>Drapeaux de la fenêtre ouverte, que la réduction complète puis rend.</summary>
    private ImGuiWindowFlags _openFlags;

    /// <summary>Avancement du repli à la dernière image, entre 0 et 1.</summary>
    private float _foldAmount;

    private readonly AppShell     _shell;
    private readonly SettingsPage  _settings;
    private readonly RpProfilePage _rpProfile;
    private readonly AroundPage    _around = new();
    private readonly FriendsPage   _friends = new();
    private readonly EncountersPage _encounters = new();
    private readonly NewsPage       _news = new();

    // Le nom de la fenêtre est la clé de persistance de imgui.ini : le changer
    // réinitialiserait position et taille chez tous les utilisateurs.
    public MainWindow(Configuration config)
        : base("Eorzea Events##main",
               ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        ShowCloseButton = false; // la barre de titre maison porte la fermeture
        LogicalSizeConstraints = OpenConstraints;
        _config   = config;
        _settings = new SettingsPage(config);
        _rpProfile = new RpProfilePage(config);

        _shell = new AppShell(
        [
            // ── Jouer : ce qui se passe maintenant, autour de soi ─────────────
            new ShellPage
            {
                Id      = "rp",
                Icon    = Icons.RpLive,
                Label   = () => Plugin.L.TabRp,
                Section = () => Plugin.L.NavSectionPlay,
                Draw    = DrawOpenRpTab,
                // Les sessions terminées restent dans la liste jusqu'au
                // rafraîchissement suivant : les compter laissait une pastille
                // sur une page vide.
                Badge = () => _sessionsList.Count(s => s.EndedAt == null),
            },
            new ShellPage
            {
                Id      = "around",
                Icon    = Icons.Around,
                Label   = () => Plugin.L.TabAround,
                Section = () => Plugin.L.NavSectionPlay,
                Draw    = _around.Draw,
                Badge   = () => Plugin.AvailableEntries.Count,
            },
            new ShellPage
            {
                Id      = "encounters",
                Icon    = Icons.History,
                Label   = () => Plugin.L.TabEncounters,
                Section = () => Plugin.L.NavSectionPlay,
                Draw    = _encounters.Draw,
            },

            // ── Communauté : sa fiche et ses liens ────────────────────────────
            new ShellPage
            {
                Id      = "profile",
                Icon    = Icons.Profile,
                Label   = () => Plugin.L.RpProfileTitle,
                Section = () => Plugin.L.NavSectionCommunity,
                Draw    = _rpProfile.Draw,
                // Les demandes reçues attendent une réponse : c'est la seule
                // chose de la fiche qui appelle un geste, et le compteur ne suit
                // rien d'autre.
                Badge  = () => Plugin.RelationRequestsReceived.Count,
                Locked = () => !Plugin.Api.HasToken,
            },
            new ShellPage
            {
                Id      = "friends",
                Icon    = Icons.Friend,
                Label   = () => Plugin.L.TabFriends,
                Section = () => Plugin.L.NavSectionCommunity,
                Draw    = _friends.Draw,
                Badge   = () => _friends.VisibleCount,
                Locked  = () => !Plugin.Api.HasToken,
            },

            // ── Agenda : ce qui est prévu ─────────────────────────────────────
            new ShellPage
            {
                Id      = "events",
                Icon    = Icons.Events,
                Label   = () => Plugin.L.TabEvents,
                Section = () => Plugin.L.NavSectionAgenda,
                Draw    = DrawEventsTab,
            },
            new ShellPage
            {
                Id      = "venues",
                Icon    = Icons.Venues,
                Label   = () => Plugin.L.TabEstabs,
                Section = () => Plugin.L.NavSectionAgenda,
                Draw    = DrawEstabTab,
            },
            // Ancrées en bas avec les réglages : elles ne servent pas à jouer,
            // et la pastille suffit à signaler une parution.
            new ShellPage
            {
                Id     = "news",
                Icon   = Icons.News,
                Label  = () => Plugin.L.TabNews,
                Draw   = _news.Draw,
                Badge  = () => _news.UnreadCount,
                Pinned = true,
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
        ], initialId: "rp")
        {
            // La panne du site d'abord : elle explique pourquoi tout ce qui suit
            // a cessé de se mettre à jour.
            Banner = activeId =>
            {
                ServerNotice.Draw();
                _news.DrawBanners(activeId == "news");
                Ui.Seasons.YearRecap.Draw(activeId, () => OpenAt("encounters"));
                Ui.Seasons.Souvenir.Draw(activeId, () => OpenAt("encounters"));
            },
        };

        Plugin.ServerBack += OnServerBack;
    }

    /// <summary>
    /// Les images sont détenues par <see cref="Textures"/>, libéré au
    /// déchargement du plugin : il ne reste à rendre que l'abonnement au retour
    /// du site.
    /// </summary>
    public void Dispose()
    {
        Plugin.ServerBack -= OnServerBack;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Le site revient : ce que la panne a fait échouer se relit, sans attendre
    /// un clic sur « Actualiser » ni le relevé suivant.
    /// </summary>
    private void OnServerBack()
    {
        if (_eventsFailed   && !_eventsLoading)   FetchEvents();
        if (_estabFailed    && !_estabLoading)    FetchEstablishments(_estabSearchInput.Trim());
        if (_sessionsFailed && !_sessionsLoading) FetchSessions();
        _rpProfile.OnServerBack();
        _news.OnServerBack();
    }

    /// <summary>
    /// Point d'accroche de l'ouverture, appelé par Dalamud à la transition
    /// fermé → ouvert, quelle qu'en soit l'origine (commande, icône, OpenAt).
    ///
    /// Les pages qui lisent des données modifiables ailleurs qu'en jeu s'y
    /// abonnent : sans ça, elles gardent jusqu'au déchargement du plugin ce
    /// qu'elles ont lu la première fois. C'est un simple signal, chaque page reste
    /// juge de l'appel réseau, qu'elle ne déclenche qu'une fois réellement
    /// affichée.
    /// </summary>
    public override void OnOpen()
    {
        _rpProfile.NotifyWindowOpened();
        _news.NotifyWindowOpened();
    }

    /// <summary>Ouvre la fenêtre sur une page donnée.</summary>
    public void OpenAt(string pageId)
    {
        _shell.Navigate(pageId);
        IsOpen = true;
    }

    /// <summary>Ouvre « Rencontres » sur la note d'un personnage.</summary>
    public void FocusEncounterNote(string characterId)
    {
        _encounters.FocusNote(characterId);
        OpenAt("encounters");
    }

    /// <summary>La coque peint elle-même les bords, sans marge de fenêtre.</summary>
    protected override bool Chromeless => true;

    protected override bool SeasonalBackdrop => true;

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

    protected override void DrawContents()
    {
        // Relevé des actualités : la pastille et le bandeau épinglé ne se
        // voient que fenêtre ouverte, inutile de les tenir à jour ailleurs.
        _news.Tick();

        // Les écrans bloquants court-circuitent la navigation, mais conservent
        // la barre de titre : sans elle, la fenêtre ne serait ni déplaçable ni
        // refermable.
        System.Action? fullScreen =
            Plugin.IsBlocked                                ? DrawBlockedScreen
          : Plugin.Api.HasToken && Plugin.Api.IsTokenRevoked ? DrawTokenInvalidScreen
          : null;

        // Retenues pour la frame suivante : c'est avant Begin, dans PreDraw, que
        // la réduction pose taille et position.
        _lastPos  = ImGui.GetWindowPos();
        _lastSize = ImGui.GetWindowSize();

        // Un écran bloquant (version obsolète, jeton révoqué) doit se voir :
        // réduite, la fenêtre n'en montrerait que la barre.
        if (fullScreen != null && _folded) ToggleFold();

        switch (_shell.Draw(_folded, fullScreen))
        {
            case TitleAction.Close:
                IsOpen = false;
                break;

            case TitleAction.ToggleFold:
                ToggleFold();
                break;

            case TitleAction.OpenAround:
                ToggleFold();
                _shell.Navigate("around");
                break;
        }
    }

    // ─── Réduction à la barre de titre ────────────────────────────────────────

    /// <summary>
    /// Taille et position de la réduction, posées avant <c>Begin</c>.
    ///
    /// Les imposer depuis le rendu ne marche pas : ImGui déplace alors le
    /// contenu mais pas le fond déjà peint ni le rognage, et la barre se
    /// dessinait décalée à chaque image de l'animation. Ici, la fenêtre naît
    /// directement à la bonne place.
    ///
    /// Pendant la réduction, elle doit aussi pouvoir descendre sous sa taille
    /// minimale ; les contraintes ne reviennent qu'une fois rouverte en entier.
    /// </summary>
    public override void PreDraw()
    {
        var folding = _folded || _foldAmount > 0f || _restoreOnOpen;

        if (folding)
        {
            LogicalSizeConstraints = null;
            SizeConstraints        = null;
        }
        else
        {
            LogicalSizeConstraints = OpenConstraints;
        }

        // La police d'abord : la largeur de la barre réduite se mesure avec.
        base.PreDraw();

        if (_restoreOnOpen)
        {
            _restoreOnOpen = false;
            ImGui.SetNextWindowPos(_openPos);
            ImGui.SetNextWindowSize(_openSize);
            return;
        }

        var amount = Motion.Approach(FoldKey, _folded ? 1f : 0f, Motion.Emphasised);

        if (amount == _foldAmount && amount is 0f or 1f)
        {
            if (amount == 1f) FitFoldedBar();
            return;
        }

        _foldAmount = amount;

        var folded = new Vector2(TitleBar.FoldedWidth(), Theme.S(Theme.TitleBarHeight));
        var size   = Vector2.Lerp(_openSize, folded, amount);
        var right  = _openPos.X + _openSize.X;

        // Des pixels entiers : ImGui tronque positions et tailles, et des
        // fractions feraient dériver la fenêtre d'un pixel à chaque repli.
        size = new Vector2(MathF.Round(size.X), MathF.Round(size.Y));
        ImGui.SetNextWindowSize(size);
        ImGui.SetNextWindowPos(new Vector2(MathF.Round(right - size.X), MathF.Round(_openPos.Y)));

        // Rouverte en entier : la fenêtre retrouve son redimensionnement et
        // l'enregistrement de sa géométrie.
        if (amount == 0f) Flags = _openFlags;
    }

    /// <summary>Position et taille à la dernière image, relevées pendant le rendu.</summary>
    private Vector2 _lastPos;
    private Vector2 _lastSize;

    /// <summary>
    /// Réduit la fenêtre à sa barre de titre, ou la restaure. Le coin haut droit
    /// reste fixe dans les deux sens : les boutons de la barre ne bougent pas
    /// sous la souris, on peut donc réduire puis restaurer sans viser à nouveau.
    /// </summary>
    private void ToggleFold()
    {
        var position = ImGui.GetWindowPos();
        var size     = ImGui.GetWindowSize();

        if (!_folded)
        {
            // Réduite à nouveau avant d'avoir fini de se rouvrir : la taille
            // mesurée n'est qu'une étape de l'animation, la vraie est déjà connue.
            if (_foldAmount == 0f)
            {
                _openSize  = size;
                _openFlags = Flags;
            }

            _openPos = new Vector2(position.X + size.X - _openSize.X, position.Y);

            // Sans redimensionnement, et sans enregistrement : imgui.ini doit
            // garder la taille ouverte, sinon la fenêtre se rouvrirait réduite
            // après un redémarrage du jeu.
            Flags   = _openFlags | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings;
            _folded = true;
            return;
        }

        // La barre a pu être déplacée une fois réduite : la fenêtre se rouvre
        // sous son coin haut droit actuel, sans déborder de l'écran.
        _openPos = OpenPositionUnder(position, size);
        _folded  = false;
    }

    /// <summary>
    /// Position de la fenêtre ouverte sous le coin haut droit d'une barre,
    /// ramenée dans l'espace utile de l'écran : une barre tirée contre le bord
    /// gauche ferait sinon rouvrir la fenêtre à moitié hors champ.
    /// </summary>
    private Vector2 OpenPositionUnder(Vector2 barPos, Vector2 barSize)
    {
        var viewport = ImGui.GetMainViewport();
        var wanted   = new Vector2(barPos.X + barSize.X - _openSize.X, barPos.Y);
        var max      = viewport.WorkPos + viewport.WorkSize - _openSize;

        return new Vector2(Math.Clamp(wanted.X, viewport.WorkPos.X, MathF.Max(viewport.WorkPos.X, max.X)),
                           Math.Clamp(wanted.Y, viewport.WorkPos.Y, MathF.Max(viewport.WorkPos.Y, max.Y)));
    }

    /// <summary>
    /// Réajuste la barre réduite quand son contenu change de largeur : un
    /// compteur qui passe de 9 à 12, une langue changée. Le bord droit reste en
    /// place, comme pour le repli.
    /// </summary>
    private void FitFoldedBar()
    {
        var want = TitleBar.FoldedWidth();
        if (_lastSize == Vector2.Zero || MathF.Abs(_lastSize.X - want) < 0.5f) return;

        ImGui.SetNextWindowSize(new Vector2(want, _lastSize.Y));
        ImGui.SetNextWindowPos(new Vector2(_lastPos.X + _lastSize.X - want, _lastPos.Y));
    }

    /// <summary>
    /// Fermée réduite, la fenêtre se rouvre en grand : la barre seule ne se
    /// comprend que si l'on vient de la réduire soi-même. Elle se rouvre sous
    /// le coin de la barre, comme un dépli, et non là où on l'avait réduite.
    /// </summary>
    public override void OnClose()
    {
        if (!_folded) return;

        _openPos       = OpenPositionUnder(_lastPos, _lastSize);
        _folded        = false;
        _restoreOnOpen = true;
        _foldAmount    = 0f;
        Flags          = _openFlags;
        Motion.Set(FoldKey, 0f);
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

        Layout.Spacer(Theme.GapXs);

        // ── Scènes ouvertes ───────────────────────────────────────────────────
        // Les actions de la liste se rangent dans son en-tête : posées seules
        // au-dessus, elles flottaient sans dire à quoi elles s'appliquaient.
        var gap     = Theme.S(Theme.GapS);
        var actionW = Btn.Measure(l.Refresh, Icons.Refresh) + gap + Btn.Measure(l.ViewOnline, Icons.External);
        var count   = _sessionsList.Count(s => s.EndedAt == null);

        Layout.SectionHeader(l.RpSectionScenes, Icons.RpLive, count > 0 ? count : null,
                             actions: () =>
                             {
                                 if (Btn.Draw(l.Refresh, BtnTone.Secondary, BtnSize.Medium, Icons.Refresh, id: "rp_refresh"))
                                     FetchSessions();
                                 ImGui.SameLine(0f, gap);
                                 if (Btn.Draw(l.ViewOnline, BtnTone.Secondary, BtnSize.Medium, Icons.External, id: "rp_online"))
                                     OpenUrl(_config.BaseUrl + "/rp-live");
                             },
                             actionsWidth: actionW);

        DrawSessionsList(l);

        // ── Mon RP ────────────────────────────────────────────────────────────
        // Une vraie coupure : ce qui suit ne liste plus les scènes des autres,
        // il règle ce que l'on montre soi-même.
        Layout.Spacer(Theme.GapXl);
        Layout.SectionHeader(l.RpSectionMine, Icons.Character);

        DrawAvailabilityCard(l);
        DrawMySessionCard(l);
    }

    /// <summary>
    /// Scènes ouvertes, les proches d'abord : ma zone, puis mon monde, puis
    /// le reste.
    ///
    /// Sans zone de défilement à hauteur figée : elle réservait dix lignes au bas
    /// de la page quoi qu'il arrive, laissant un vide sous une liste courte et un
    /// second ascenseur sous une longue.
    /// </summary>
    private void DrawSessionsList(Loc l)
    {
        if (_sessionsLoading)
        {
            Text.Body(l.Loading, Theme.TextMuted);
            return;
        }

        var active = _sessionsList.Where(s => s.EndedAt == null).ToList();

        // Une panne n'est pas une soirée sans scène : jamais reçue, la liste est
        // vide parce que le site ne répond pas, et « Lance la première ! »
        // mentirait. Même traitement que l'agenda et les lieux.
        if (active.Count == 0
            && (_sessionsFailed || (_sessionsLastFetch == DateTime.MinValue && Plugin.Api.IsServerDown)))
        {
            Feedback.EmptyState(Icons.Warning, l.LoadFailed, null, l.Refresh, FetchSessions,
                                centered: false);
            return;
        }

        if (active.Count == 0)
        {
            // « Lance la première ! » ne s'adresse qu'à qui peut ouvrir une scène.
            // Non centré : les cartes suivent, et le centrage les aurait
            // repoussées en bas d'une grande fenêtre.
            Feedback.EmptyState(Icons.RpLive, l.RpNoSession,
                                Plugin.Api.HasToken ? l.RpBeFirst : null,
                                centered: false);
            return;
        }

        var currentWorld = Plugin.ObjectTable.LocalPlayer?.CurrentWorld.Value.Name.ToString();
        var currentZone  = GetCurrentZoneName();

        // Trois groupes et non deux : une scène de mon monde mais d'une autre
        // zone tombait dans « Autres serveurs », ce qui était faux.
        List<RpSessionDto> nearby  = [];
        List<RpSessionDto> myWorld = [];
        List<RpSessionDto> others  = [];
        foreach (var s in active)
        {
            if (currentWorld != null && s.Server == currentWorld)
            {
                if (currentZone != null && s.Location == currentZone) nearby.Add(s);
                else                                                   myWorld.Add(s);
            }
            else others.Add(s);
        }

        var first = true;
        void Group(string label, FontAwesomeIcon icon, List<RpSessionDto> sessions)
        {
            if (sessions.Count == 0) return;
            if (!first) Layout.Spacer(Theme.GapS);
            first = false;

            Layout.SubHeader(label, icon);
            foreach (var s in sessions) DrawSessionEntry(s);
        }

        Group(string.Format(l.RpInYourZone, currentZone), Icons.Around, nearby);
        Group(string.Format(l.RpOnWorld, currentWorld), Icons.World, myWorld);
        Group(l.RpOtherServers, Icons.World, others);
    }

    /// <summary>
    /// Disponibilité pour du RP spontané. L'interrupteur se pose dans l'en-tête,
    /// en face du titre : seul sur sa ligne au milieu de la carte, il ne se
    /// lisait pas comme le réglage principal de celle-ci.
    /// </summary>
    private void DrawAvailabilityCard(Loc l)
    {
        using var card = Card.Begin("rp_availability", interactive: false);

        var player     = Plugin.ObjectTable.LocalPlayer;
        var charLinked = player != null
            && Plugin.Config.FindCharacterToken(player.Name.TextValue, (int)player.HomeWorld.RowId) != null;

        DrawAvailabilityHeading(l, charLinked);

        if (Plugin.LoginPromptPending)
        {
            Feedback.Alert(Theme.Online, Icons.Around, l.RpAvailableActiveStatus, l.RpLoginPrompt, () =>
            {
                if (Btn.Draw(l.RpLoginStay, BtnTone.Success, BtnSize.Small, Icons.Check, id: "stay"))
                {
                    Plugin.DismissLoginPrompt();
                    Plugin.SetRpAvailability(true);
                }
                ImGui.SameLine(0f, Theme.S(Theme.GapS));
                if (Btn.Draw(l.RpLoginDisable, BtnTone.Secondary, BtnSize.Small, id: "disable"))
                {
                    Plugin.DismissLoginPrompt();
                    Plugin.SetRpAvailability(false);
                }
            });
            Layout.Spacer(Theme.GapS);
        }

        Text.Hint(l.RpAvailableDesc);
        Layout.Spacer(Theme.GapS);

        if (!charLinked)
        {
            Text.Hint(l.RpAvailableNoToken, Theme.Gold);
            Layout.Spacer(Theme.GapXs);
            if (Btn.Draw(l.LinkThisCharacter, BtnTone.Primary, BtnSize.Medium, id: "rp_link"))
                Plugin.OpenSetup(migration: Plugin.Config.CharacterTokens.Count == 0
                    && !string.IsNullOrWhiteSpace(Plugin.Config.ApiToken));
            return;
        }

        if (Btn.Draw(l.RpProfileSetup, BtnTone.Secondary, BtnSize.Medium, Icons.Profile, id: "rp_wizard"))
            Plugin.OpenRpProfileWizard();
    }

    /// <summary>
    /// Titre de la carte à gauche, l'interrupteur et « Je suis disponible »
    /// à droite, tous centrés sur la hauteur de l'interrupteur. Le libellé
    /// passe au vert quand la disponibilité est allumée.
    /// </summary>
    private static void DrawAvailabilityHeading(Loc l, bool charLinked)
    {
        var top   = ImGui.GetCursorPosY();
        var frame = ImGui.GetFrameHeight();

        using (Fonts.PushH2())
        {
            ImGui.SetCursorPosY(top + (frame - ImGui.GetTextLineHeight()) * 0.5f);
            Text.WithIcon(Icons.Around, l.RpAvailableTitle, Theme.Accent, Theme.Text);
        }

        if (charLinked)
        {
            var available = Plugin.CurrentCharacterAvailabilityWanted;
            var toggleW   = frame * 0.82f * 1.85f;
            var gap       = Theme.S(Theme.GapS);
            var labelW    = ImGui.CalcTextSize(l.RpAvailableEnable).X;

            // Interrupteur puis libellé, dans l'ordre de toutes les lignes de
            // réglage du plugin.
            ImGui.SameLine();
            Layout.RightAlign(toggleW + gap + labelW);
            ImGui.SetCursorPosY(top);
            var changed = Inputs.Toggle("##rp_available", ref available);

            ImGui.SameLine(0f, gap);
            ImGui.SetCursorPosY(top + (frame - ImGui.GetTextLineHeight()) * 0.5f);
            ImGui.TextColored(available ? Theme.Online : Theme.TextMuted, l.RpAvailableEnable);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsItemClicked()) { available = !available; changed = true; }
            }

            if (changed) Plugin.SetRpAvailability(available);
        }

        ImGui.SetCursorPosY(top + frame);
        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapXs)));
    }

    private void DrawMySessionCard(Loc l)
    {
        // Rien à proposer sans personnage lié : ouvrir une scène lui est
        // impossible, et la carte resterait vide.
        if (!Plugin.Api.HasToken) return;

        using var card = Card.Begin("rp_mysession", interactive: false);

        Layout.CardHeading(l.MySessionTitle, Icons.RpLive);

        if (Plugin.HasActiveSession)
        {
            Text.Wrapped(l.RpYourSessionActive, Theme.Online);
            Layout.Spacer(Theme.GapS);
            if (Btn.Draw(l.RpManageSession, BtnTone.Primary, BtnSize.Medium, id: "rp_manage"))
                Plugin.OpenMySession();
            return;
        }

        if (Btn.Draw(l.RpNewSession, BtnTone.Primary, BtnSize.Medium, id: "rp_new"))
            Plugin.OpenMySession();
    }

    /// <summary>
    /// Carte d'une scène ouverte, au format des cartes d'événement : titre,
    /// une ligne par information avec son icône, description, puis les actions
    /// en rangée au pied de la carte.
    ///
    /// Les boutons étaient posés au bout des lignes, l'un calé sur la largeur
    /// de la fenêtre et non sur celle de la carte : il la faisait déborder à
    /// droite, et l'autre, collé au nom, sortait décalé en hauteur.
    /// </summary>
    private void DrawSessionEntry(RpSessionDto s)
    {
        var l = Plugin.L;

        using var card = Card.Begin($"session_{s.Id}", interactive: false);

        Text.CardTitle(s.Title, Theme.TitleGold);
        Layout.Spacer(Theme.GapXs);

        // ── Où ────────────────────────────────────────────────────────────────
        // Zone, monde et instance d'abord ; puis l'adresse précise : la parcelle
        // dans un quartier résidentiel, les coordonnées sur la carte ailleurs,
        // comme sur la page RP live du site. Sans elles, « Kugane » ne disait
        // pas où chercher.
        var where = s.InstanceId is > 0
            ? $"{s.Location}  ·  {s.Server}  ·  {string.Format(l.AroundInstance, s.InstanceId)}"
            : $"{s.Location}  ·  {s.Server}";
        Text.WithIcon(Icons.Location, where, Theme.TextFaint, Theme.Text, wrap: true);

        if (s.Ward.HasValue)
        {
            var ward = string.Format(l.HousingWard, s.Ward) + (s.Wing == true ? $" ({l.HousingAnnex})" : "");
            var housingInfo = s.Room.HasValue ? $"{ward}  ·  {l.FieldRoom} {s.Room}"
                            : s.Plot.HasValue ? $"{ward}  ·  {l.FieldPlot} {s.Plot}"
                            : ward;
            Text.WithIcon(Icons.Housing, housingInfo, Theme.TextFaint, Theme.Text);
        }
        else if (s.PosX is { } x && s.PosZ is { } z)
        {
            Text.WithIcon(Icons.Map, $"X {x:0.0}  ·  Y {z:0.0}", Theme.TextFaint, Theme.Text);
        }

        // Point de rendez-vous : hors logement, c'est le seul repère pour
        // retrouver l'auteur, il ne doit pas se perdre dans la description.
        if (!string.IsNullOrEmpty(s.MeetingPoint))
            Text.WithIcon(Icons.Venues, s.MeetingPoint!, Theme.TextFaint, Theme.TextMuted, wrap: true);

        // ── Qui ───────────────────────────────────────────────────────────────
        // Le personnage, et combien de joueurs sont déjà sur place : c'est ce
        // qui décide d'y aller ou non. Une session annoncée sous un nom libre
        // depuis le site n'a pas de personnage lié : rien ne garantirait que
        // l'auteur est bien lui.
        var who = s.CharacterName is { Length: > 0 } author ? author : null;
        if (s.NearbyCount > 0)
            who = who == null ? string.Format(l.OnSiteCount, s.NearbyCount)
                              : $"{who}  ·  {string.Format(l.OnSiteCount, s.NearbyCount)}";
        if (who != null)
            Text.WithIcon(Icons.Character, who, Theme.TextFaint, Theme.TextMuted, wrap: true);

        // Emplacement déclaré : le dire, sinon l'absence de bouton carte et le
        // comptage à zéro passeraient pour une panne du plugin.
        if (string.Equals(s.Source, "WEB", StringComparison.OrdinalIgnoreCase))
        {
            Text.WithIcon(Icons.Info, l.DeclaredPosition, Theme.TextFaint, Theme.TextFaint);
            Feedback.TooltipOnHover(l.DeclaredPositionHint);
        }

        if (!string.IsNullOrEmpty(s.Description))
        {
            Layout.Spacer(Theme.GapXs);
            Text.Wrapped(s.Description!, Theme.TextMuted);
        }

        // ── Actions ───────────────────────────────────────────────────────────
        var canMap     = s.TerritoryId.HasValue && s.MapId.HasValue && s.PosX.HasValue && s.PosZ.HasValue;
        var canProfile = !string.IsNullOrEmpty(s.CharacterId) && s.CharacterHasWebPage;
        var canResume  = !Plugin.HasActiveSession && Plugin.MySessionIds.Contains(s.Id);
        if (!canMap && !canProfile && !canResume) return;

        Layout.Spacer(Theme.GapS);
        var flow = FlowRow.Fill(Theme.S(Theme.GapS));

        // Session orpheline : la reprendre est l'action attendue, elle passe en tête.
        if (canResume)
        {
            flow.Next(Btn.Measure(l.RpResume));
            if (Btn.Draw(l.RpResume, BtnTone.Primary, BtnSize.Medium, id: $"claim_{s.Id}"))
                Plugin.ClaimSession(s);
        }
        if (canMap)
        {
            flow.Next(Btn.Measure(l.Map, Icons.Map));
            if (Btn.Draw(l.Map, BtnTone.Secondary, BtnSize.Medium, Icons.Map, id: $"map_{s.Id}"))
                Plugin.Framework.RunOnFrameworkThread(() => OpenOnMap(s));
        }
        if (canProfile)
        {
            flow.Next(Btn.Measure(l.MenuViewRpProfile, Icons.Profile));
            if (Btn.Draw(l.MenuViewRpProfile, BtnTone.Secondary, BtnSize.Medium, Icons.Profile, id: $"prof_{s.Id}"))
                Plugin.OpenRpProfileViewer(s.CharacterId!, s.CharacterName!, s.Server);
        }
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
            // Une panne n'est pas un agenda vide : le dire, et proposer de
            // réessayer plutôt que d'envoyer sur le site.
            if (_eventsFailed)
            {
                Feedback.EmptyState(Icons.Warning, l.LoadFailed, null,
                                    l.Refresh, FetchEvents);
                return;
            }

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

                // Joueurs détectés sur place, comme sur la page d'accueil du
                // site : c'est ce qui distingue un événement qui bat son plein
                // d'une salle vide.
                if (ev.NearbyCount > 0)
                {
                    ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                    Chip.Draw(string.Format(l.OnSiteCount, ev.NearbyCount), ChipTone.Neutral,
                              Icons.Around);
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

            // Titre de l'événement : même style que le nom d'un lieu ou le
            // titre d'une actualité, d'une carte à l'autre.
            var displayColor = ev.Cancelled ? UiStyle.TextSubtle : titleColor;
            if (ev.Cancelled)
            {
                using (Fonts.PushH2()) Text.Strikethrough(ev.Title, displayColor);
            }
            else
            {
                Text.CardTitle(ev.Title, displayColor);
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
                // « + d'infos » sur une carte d'événement parle de
                // l'événement : la fiche du lieu reste à un clic, depuis la
                // fenêtre qui s'ouvre.
                if (Btn.Draw(l.MoreInfo, BtnTone.Primary, BtnSize.Medium,
                             Icons.Info, id: $"info_{ev.Id}"))
                    Plugin.OpenEventDetail(ev);

                TravelButton.Draw(venue, $"ev_{ev.Id}", sameLine: true);
            }

        });
    }

    /// <summary>
    /// Carte d'événement. L'affiche propre à l'événement sert de bannière quand
    /// elle existe ; à défaut la carte reste sobre, la bannière du lieu étant
    /// déjà visible dans l'onglet des établissements.
    ///
    /// 140 px et non plus 84 : recadrée en bandeau, une affiche ne montrait
    /// guère plus qu'une rayure, et l'on ne reconnaissait pas l'événement.
    /// </summary>
    private void DrawEventCard(EventDto ev, System.Action content)
    {
        using var card = Card.Begin($"event_{ev.Id}", CardTone.Interactive,
                                    banner: Textures.Get(ev.Image),
                                    bannerHeight: 140f);
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
            try
            {
                _eventsList      = await Plugin.Api.GetUpcomingEventsAsync();
                _eventsLastFetch = DateTime.UtcNow;
                _eventsFailed    = false;
            }
            catch
            {
                // Daté même en échec : sinon l'onglet relançait la requête à
                // chaque image tant que le site était en maintenance. Le retour
                // du site la relance de lui-même (OnServerBack).
                _eventsFailed    = true;
                _eventsLastFetch = DateTime.UtcNow;
            }
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
            if (_estabFailed)
            {
                Feedback.EmptyState(Icons.Warning, l.LoadFailed, null,
                                    l.Refresh, () => FetchEstablishments(_estabSearchInput));
                return;
            }

            Feedback.EmptyState(Icons.Venues,
                _config.HiddenEstablishmentIds.Count > 0 ? l.EstabNoResults : l.EstabSearchHint);
            DrawHiddenEstablishmentsSection();
            return;
        }

        Layout.SectionHeader(l.TabEstabs, Icons.Venues, visibleEstabs.Count);

        if (!ImGui.BeginChild("##estabscroll", new Vector2(-1, -1), false)) return;

        // Deux colonnes dès que la largeur le permet. Une carte de lieu se lit
        // très bien sur une demi-largeur, et l'empilement en colonne unique
        // faisait défiler longuement une liste pourtant courte. Sous le seuil,
        // on retombe sur une colonne : à deux, les catégories et les boutons
        // d'une carte étroite passeraient à la ligne les uns après les autres.
        var columns = ImGui.GetContentRegionAvail().X >= Theme.S(680f) ? 2 : 1;

        string? toHide = null;
        if (!ImGui.BeginTable("##estabgrid", columns, ImGuiTableFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var e in visibleEstabs)
        {
            ImGui.TableNextColumn();
            bool hideThis = false;
            UiPrimitives.DrawCardWithBanner(Textures.Get(e.Banner), () =>
            {
                // ── Titre, teinté de la couleur choisie par le gérant ──────────
                var accent = Theme.TryParseHex(e.AccentColor) is { } custom
                    ? Theme.EnsureReadable(custom)
                    : Theme.Accent;

                Text.CardTitle(e.Name, accent);

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

        ImGui.EndTable();
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
                _estabList   = list;
                _estabFailed = false;
            }
            catch { _estabFailed = true; }
            finally { _estabLoading = false; }
        });
    }

    public void UpdateSessionsList(List<RpSessionDto> sessions)
    {
        _sessionsList      = sessions;
        _sessionsLastFetch = DateTime.UtcNow;
        _sessionsLoading   = false;
        _sessionsFailed    = false;
    }

    private void FetchSessions()
    {
        _sessionsLoading = true;
        Task.Run(async () =>
        {
            try
            {
                _sessionsList      = await Plugin.Api.GetActiveSessionsAsync();
                _sessionsLastFetch = DateTime.UtcNow;
                _sessionsFailed    = false;
            }
            catch { _sessionsFailed = true; }
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

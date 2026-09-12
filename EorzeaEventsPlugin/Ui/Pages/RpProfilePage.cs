using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui.Components;
using System.Linq;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Fiche RP du personnage connecté.
///
/// Le partage des rôles avec le site est délibéré : ce qui se règle vite et se
/// change souvent en jouant est éditable ici (disponibilité, accroches,
/// préférences), tandis que les textes longs se rédigent sur le site. Saisir
/// une biographie dans une fenêtre de jeu n'a pas de sens.
/// </summary>
internal sealed class RpProfilePage(Configuration config)
{
    private RpProfileDto? _profile;

    /// <summary>
    /// La fiche affichée vient du serveur, et non du cache local.
    ///
    /// Le cache ne porte ni la page web, ni l'indexation, ni l'audience par
    /// section : reconstituée depuis lui, la fiche affiche les défauts du DTO,
    /// c'est-à-dire une page web active et des sections ouvertes. Tant que le
    /// réseau n'a pas répondu, ces réglages ne sont donc pas connus, et les
    /// renvoyer les écraserait. Le cas n'a rien de théorique : il suffit que le
    /// site soit injoignable au changement de personnage.
    /// </summary>
    private bool _profileFromNetwork;

    private string        _loadedFor = string.Empty;
    private bool          _loading;
    private bool          _saving;
    private DateTime      _savedUntil = DateTime.MinValue;

    /// <summary>
    /// La fenêtre vient de s'ouvrir, un rafraîchissement est à tenter au premier
    /// rendu de cette page.
    ///
    /// Le signal est consommé dans <see cref="Draw"/> et non traité à l'ouverture :
    /// Draw n'est appelé que si l'onglet est effectivement affiché, ce qui évite
    /// un appel réseau pour une page que le joueur ne regarde pas. Effet de bord
    /// utile : ouvrir la fenêtre sur un autre onglet puis venir ici rafraîchit
    /// quand même.
    /// </summary>
    private bool _refreshPending;

    /// <summary>Horodatage du dernier chargement réseau réussi, pour l'anti-rebond.</summary>
    private DateTime _lastFetchedAt = DateTime.MinValue;

    /// <summary>
    /// Délai minimal entre deux rafraîchissements automatiques. Ouvrir et refermer
    /// la fenêtre trois fois de suite ne doit pas déclencher trois requêtes ; le
    /// bouton explicite, lui, passe outre, puisqu'il exprime une demande.
    /// </summary>
    private static readonly TimeSpan AutoRefreshCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Le dernier enregistrement a échoué.
    ///
    /// Il ne l'était nulle part : le bouton reprenait son état normal, sans
    /// « enregistré » ni erreur, et rien ne distinguait un refus de validation
    /// d'une réussite.
    /// </summary>
    private bool _saveFailed;

    // Copie de travail des champs éditables en jeu.
    private readonly string[] _hooks = ["", "", "", "", ""];

    // Coup d'œil : cinq emplacements fixes, comme les accroches. Ce sont des
    // champs courts, une icône et deux lignes, dont on veut pouvoir changer en
    // pleine soirée ; les saisir en jeu est donc voulu, contrairement aux textes
    // longs de la biographie.
    private readonly int[]    _glanceIcons  = new int[MaxGlances];
    private readonly string[] _glanceTitles = ["", "", "", "", ""];
    private readonly string[] _glanceBodies = ["", "", "", "", ""];
    private readonly bool[]   _glanceActive = new bool[MaxGlances];

    /// <summary>
    /// Nombre d'emplacements ouverts à l'écran, et non plafond fixe.
    ///
    /// Les cinq emplacements affichés en permanence donnaient un mur de quinze
    /// champs pour une fiche qui en remplit un ou deux : on n'ouvre plus que ce
    /// qui existe, le reste s'ajoute au bouton.
    /// </summary>
    private int _glanceCount;

    /// <summary>Emplacement dont le retrait est armé, -1 si aucun.</summary>
    private int _glanceArmed = -1;

    // Codes de sync : emplacements fixes, comme les accroches. Pas de boutons
    // ajouter/supprimer, une ligne sans identifiant n'est simplement pas envoyée.
    private readonly int[]    _syncTypes = new int[MaxSyncshells];
    private readonly string[] _syncNames = ["", "", "", "", ""];
    private readonly string[] _syncIds   = ["", "", "", "", ""];
    private string _currentQuest = string.Empty;

    // Instant présent. Son enregistrement est distinct de celui de la fiche : la
    // route dédiée n'écrit que ces deux champs, si bien que changer d'état
    // pendant qu'une biographie est en cours de saisie ne publie pas la saisie.
    private string   _currently        = string.Empty;
    private int      _icStateIndex;
    private bool     _statusDirty;
    private bool     _statusSaving;
    private bool     _statusFailed;
    private DateTime _statusSavedUntil = DateTime.MinValue;

    /// <summary>Aligné sur RP_MAX_CURRENTLY (src/lib/rp-vocabulary.ts).</summary>
    private const int MaxCurrently = 140;

    private int    _levelIndex;
    private int    _approachIndex;
    private bool   _langFr = true;
    private bool   _langEn;
    private bool   _dirty;

    /// <summary>
    /// Au moins une modification qui n'est pas un simple interrupteur.
    ///
    /// Verrou de l'enregistrement automatique : une case cochée est un choix
    /// arrêté, une phrase à demi tapée n'en est pas un. Tant que ce drapeau est
    /// levé, la fiche attend le bouton, et une biographie en cours de frappe ne
    /// part donc jamais toute seule sous les yeux des lecteurs.
    /// </summary>
    private bool _textDirty;

    /// <summary>
    /// Instant où l'enregistrement automatique doit partir, `null` si aucun n'est
    /// en attente. Le délai absorbe les rafales : régler quatre audiences d'affilée
    /// ne fait qu'un envoi.
    /// </summary>
    private DateTime? _autoSaveAt;

    private static readonly TimeSpan AutoSaveDelay = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Le dernier envoi est parti tout seul. Départage les deux retours : la
    /// ligne d'état ne revendique un enregistrement automatique que s'il l'était,
    /// et ne s'attribue pas celui d'un clic sur le bouton.
    /// </summary>
    private bool _lastSaveWasAuto;

    /// <summary>
    /// Contrôle d'où est parti le dernier enregistrement automatique, vide sinon.
    ///
    /// Le retour se lit sous le réglage qu'on vient de toucher, jamais ailleurs.
    /// En tête de page il disparaissait dès la fiche déroulée ; épinglé dans un
    /// coin, puis au pied de la carte, il restait hors du regard, qui n'a pas
    /// quitté l'interrupteur. Il tient donc à l'interrupteur lui-même, et tous
    /// les autres se taisent.
    /// </summary>
    private string _autoSaveControl = string.Empty;

    /// <summary>
    /// Onglet ouvert dans la disposition en onglets. Conservé le temps de la
    /// session seulement : revenir sur sa fiche, c'est presque toujours revenir
    /// à ce qu'on y règle le plus souvent.
    /// </summary>
    private string _tab = "overview";

    private string _height      = string.Empty;
    private string _build       = string.Empty;
    private string _marks       = string.Empty;
    private string _voice       = string.Empty;
    private string _freeCompany = string.Empty;
    private string _allegiance  = string.Empty;
    private string _quote       = string.Empty;

    /// <summary>
    /// Nom RP, seul champ d'identité modifiable en jeu.
    ///
    /// Il nomme la fiche dans le sélecteur : une fiche créée depuis le jeu
    /// s'appellerait « Fiche 2 » jusqu'à ce que son auteur ouvre le site, ce qui
    /// vide de son sens un cycle de vie conçu pour se passer du navigateur. Le
    /// reste de l'identité s'écrit toujours sur le site.
    /// </summary>
    private string _rpName      = string.Empty;

    // Identité, désormais éditable en jeu comme sur le site. Elle était en
    // lecture seule au motif qu'elle se rédige une fois pour toutes, ce qui
    // laissait sans recours un joueur qui n'ouvre jamais le navigateur.
    private int    _raceIndex;
    private int    _nameplateIndex;
    private string _nickname   = string.Empty;
    private string _age        = string.Empty;
    private string _pronouns   = string.Empty;
    private string _origin     = string.Empty;
    private string _occupation = string.Empty;

    // Thèmes recherchés et évités, six au plus de chaque côté.
    private readonly List<string> _themes      = [];
    private readonly List<string> _avoidThemes = [];

    // Relations, huit au plus. Trois tableaux parallèles plutôt qu'une liste
    // d'objets, comme les coups d'œil : ImGui édite des champs par référence,
    // et un tableau de chaînes s'y prête là où une liste de records ne s'y prête
    // pas.
    private readonly string[] _relationNames = new string[MaxRelations];
    private readonly int[]    _relationKinds = new int[MaxRelations];
    private readonly string[] _relationNotes = new string[MaxRelations];
    private int _relationCount;
    private int _relationArmed = -1;

    // Liens de la fiche, hors syncshells.
    private string _themeSongUrl = string.Empty;
    private string _externalUrl  = string.Empty;

    // Habillage réservé aux membres. Le serveur dit si le compte y a droit ; le
    // plugin ne le devine pas, et l'écriture est revérifiée de toute façon.
    private Vector3 _accent1;
    private Vector3 _accent2;
    private bool    _accent2On;
    private int     _frameIndex;
    private int     _titleAnimIndex;
    private string  _rpTitle = string.Empty;

    // Textes longs, rédigés dans une fenêtre à part faute de place ici.
    private string _appearance  = string.Empty;
    private string _personality = string.Empty;
    private string _background  = string.Empty;
    private string _limits      = string.Empty;
    private int    _deityIndex;

    // Visibilité : deux consentements, plus une audience par section. Le premier
    // couvre à la fois la consultation en jeu et l'adresse partageable de la
    // fiche, les deux ne se règlent plus séparément.
    private bool _visInGame = true;
    private bool _visIndexable;

    /// <summary>Consentement d'affichage du statut d'équipe, pour qui en a un.</summary>
    private bool _visStaffBadge;
    private readonly int[] _sectionAudience = new int[SectionKeys.Length];

    private static readonly string[] LevelKeys    = ["beginner", "casual", "confirmed"];

    /// <summary>
    /// États de jeu, alignés sur <c>RP_IC_STATES</c> (src/lib/rp-vocabulary.ts) et
    /// vérifiés par <c>scripts/check-rp-vocabulary.ts</c>. L'ordre est celui du
    /// serveur : c'est lui qui détermine ce que le sélecteur enregistre.
    /// </summary>
    private static readonly string[] IcStateKeys = ["ic", "ooc"];
    private static readonly string[] ApproachKeys = ["come_to_me", "i_approach", "either"];

    /// <summary>
    /// Habillage réservé aux membres. Vocabulaires finis, à garder alignés sur
    /// <c>RP_FRAME_STYLES</c> et <c>RP_TITLE_ANIMATIONS</c> (src/lib/rp-vocabulary.ts) :
    /// une valeur absente d'un côté serait acceptée puis rendue nulle part.
    /// L'absence de valeur signifie « aucun effet », elle ne figure donc pas ici.
    /// </summary>
    private static readonly string[] FrameKeys =
        ["glow", "shimmer", "orbit", "gilded", "corners", "ripple", "duo"];

    private static readonly string[] TitleAnimKeys =
        ["sweep", "pulse", "rainbow", "sheen", "halo", "duotone", "wave", "neon"];

    /// <summary>
    /// Formes du nom sur la plaque, dans l'ordre de RP_NAMEPLATE_NAMES côté
    /// site. Pas d'entrée vide en tête, contrairement aux races : « rp_name »
    /// EST le défaut, et une fiche dont la colonne est restée nulle doit
    /// trouver une option en regard plutôt qu'un sélecteur sans réponse.
    /// </summary>
    private static readonly string[] NameplateKeys =
        ["rp_name", "nickname", "rp_name_nickname"];

    // « glance » vient juste après « hooks », comme côté serveur : les défauts
    // ci-dessous sont lus par position, tout décalage réglerait une section sur
    // l'audience d'une autre.
    private static readonly string[] SectionKeys =
        ["identity", "hooks", "glance", "gallery", "traits", "belonging", "description", "relations", "limits", "links", "sync"];

    /// <summary>
    /// Audiences, de la plus large à la plus étroite, dans le même ordre que
    /// <c>RP_AUDIENCES</c> côté serveur. L'ordre porte du sens : « ami » est un
    /// échelon intermédiaire, pas une troisième option quelconque.
    /// </summary>
    private static readonly string[] AudienceKeys = ["public", "friend", "owner"];

    /// <summary>
    /// Services proposés à la saisie, alignés sur SYNCSHELL_TYPES
    /// (src/lib/syncshells.ts). Les services retirés de cette liste restent
    /// affichés par <c>RpProfileView.SyncshellLabel</c> sur les fiches anciennes.
    /// </summary>
    private static readonly string[] SyncTypeKeys = ["snowcloak", "lightless", "umbra", "autre"];

    /// <summary>Aligné sur MAX_SYNCSHELLS (src/lib/syncshells.ts) et sur le Zod serveur.</summary>
    private const int MaxSyncshells = 5;

    /// <summary>
    /// Vocabulaire d'icônes du coup d'œil, dans l'ordre exact de
    /// <c>RP_GLANCE_ICONS</c> (src/lib/rp-vocabulary.ts), que
    /// <c>scripts/check-rp-vocabulary.ts</c> compare à ce tableau. L'ordre est
    /// aussi celui du sélecteur : le rang choisi désigne la clé enregistrée.
    /// </summary>
    private static readonly string[] GlanceIconKeys =
    [
        "sword", "shield", "book", "scroll", "flask", "music", "heart", "star",
        "coin", "hammer", "leaf", "flame", "moon", "sun", "eye", "mask",
        "crown", "anchor", "feather", "key", "skull", "cup", "map", "paw",
    ];

    /// <summary>
    /// Icône d'un emplacement qu'on vient d'ouvrir. L'étoile est neutre, là où
    /// la première clé du tableau (l'épée) laissait croire à un choix déjà fait.
    /// Le site ouvre ses nouveaux emplacements sur la même icône.
    /// </summary>
    private static readonly int GlanceDefaultIcon = Array.IndexOf(GlanceIconKeys, "star");

    /// <summary>Emplacements et longueurs, alignés sur le Zod de la route.</summary>
    private const int MaxGlances     = 5;
    private const int MaxGlanceTitle = 60;
    private const int MaxGlanceBody  = 200;

    /// <summary>
    /// Défauts par section, dans l'ordre de <see cref="SectionKeys"/>, exprimés
    /// par clé et non par rang.
    ///
    /// Ils étaient écrits en indices dans <see cref="AudienceKeys"/>, ce qui
    /// rendait toute insertion au milieu du vocabulaire silencieusement fausse :
    /// ajouter « ami » décalait « moi seul » et affichait, puis enregistrait, une
    /// audience plus large que celle voulue. Ils doivent rester alignés sur
    /// <c>RP_SECTIONS</c> (src/lib/rp-vocabulary.ts).
    /// </summary>
    private static readonly string[] SectionDefaultKeys =
        ["public", "public", "public", "public", "public", "owner", "public", "owner", "public", "public", "owner"];

    /// <summary>
    /// Les Douze, dans l'ordre du serveur, précédés d'une entrée vide : l'index
    /// 0 signifie « non précisé », pas « Halone ».
    /// </summary>
    private static readonly string[] DeityKeys =
    [
        "", "halone", "menphina", "thaliak", "nymeia", "llymlaen", "oschon",
        "byregot", "rhalgr", "azeyma", "naldthal", "nophica", "althyk", "other",
    ];

    /// <summary>
    /// Races jouables, précédées d'une entrée vide : l'index 0 signifie « non
    /// précisé », pas « Hyur ». Même ordre que RP_RACES côté site.
    /// </summary>
    private static readonly string[] RaceKeys =
    [
        "", "hyur", "elezen", "lalafell", "miqote", "roegadyn", "aura",
        "hrothgar", "viera", "other",
    ];

    /// <summary>Thèmes de jeu, dans l'ordre de RP_THEMES côté site.</summary>
    private static readonly string[] ThemeKeys =
    [
        "tavern", "adventure", "drama", "romance", "lore", "dark",
        "mystery", "intrigue", "combat", "craft", "slice_of_life", "politics",
    ];

    /// <summary>Plafond de RP_MAX_THEMES, appliqué aussi par le serveur.</summary>
    private const int MaxThemes = 6;

    /// <summary>Types de relation, dans l'ordre de RP_RELATION_KINDS côté site.</summary>
    private static readonly string[] RelationKindKeys =
    [
        "ally", "friend", "family", "lover", "mentor", "student", "rival", "enemy", "other",
    ];

    /// <summary>Plafonds de RP_MAX_RELATIONS et RP_MAX_RELATION_NOTE.</summary>
    private const int MaxRelations    = 8;
    private const int MaxRelationName = 80;
    private const int MaxRelationNote = 300;

    /// <summary>
    /// Couleur d'accent de la fiche en cours, déjà rendue lisible sur le thème
    /// sombre. Recalculée à chaque accès plutôt que mise en cache : _profile est
    /// remplacé au chargement, un champ figé mentirait après un changement de
    /// personnage.
    /// </summary>
    private Vector4 Tone => RpProfileView.Accent(_profile);

    public void Draw()
    {
        var l = Plugin.L;

        if (Plugin.CurrentCharacter is not { } character)
        {
            Feedback.EmptyState(Icons.Character, l.RpProfileNoCharacter);
            return;
        }

        var key = Configuration.CharacterKey(character.Name, character.WorldId);
        // Recharger aussi quand la fiche choisie change : la clé du personnage
        // ne suffit plus à décrire ce qui est à l'écran.
        if (_loadedFor != key || _loadedProfile != _selectedProfileId) Load(key);
        else if (_refreshPending) AutoRefresh(key);

        // Avant le rendu : ce qui part maintenant s'affiche « enregistré » dès
        // cette image, et non à la suivante.
        TickAutoSave();

        // Place réservée en bas pour la barre d'enregistrement, quand il y a
        // quelque chose à enregistrer. Le bouton vivait au fond de la section en
        // cours de modification : sur une fiche qui tient en plusieurs écrans,
        // il fallait le chercher au défilement à chaque changement.
        var pinned  = _dirty || DateTime.UtcNow < _savedUntil;
        var reserve = pinned ? Theme.S(52f) : 0f;

        using (var scroll = ImRaii.Child("##rpprofilescroll", new Vector2(-1f, -reserve - 1f)))
        {
            if (!scroll) return;
            DrawScrollingBody(character, l);
        }

        if (pinned) DrawPinnedSaveBar(l);
    }

    /// <summary>
    /// Barre d'enregistrement toujours visible, en pied de page.
    ///
    /// Elle porte l'état de la dernière tentative, pour que le résultat se lise
    /// à l'endroit où l'on vient de cliquer plutôt qu'à celui où l'on a modifié
    /// quelque chose.
    /// </summary>
    private void DrawPinnedSaveBar(Loc l)
    {
        Layout.Divider(Theme.GapXs);

        if (_dirty)
        {
            if (Btn.Draw(_saving ? l.Processing : l.Save, BtnTone.Primary, BtnSize.Medium,
                         Icons.Check, disabled: _saving, id: "rpprofile_save_pinned"))
            {
                _lastSaveWasAuto = false;
                Save();
            }

            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            ImGui.AlignTextToFramePadding();

            if (_saveFailed) Text.Small(SaveFailureText(l), Theme.Danger);
            else             Text.Small(l.RpProfileUnsaved, Theme.Gold);

            return;
        }

        Text.WithIcon(Icons.Check, l.RpProfileSaved, Theme.Online, Theme.Online);
    }

    private void DrawScrollingBody((string Name, int WorldId) character, Loc l)
    {
        DrawHeader(character, l);
        DrawProfileSwitcher(l);

        if (_loading && _profile == null)
        {
            Feedback.SkeletonCards(2);
            return;
        }

        DrawActionRow(l);
        DrawWebNotice(l);

        if (config.RpProfileTabs) DrawTabbed(l);
        else                      DrawSinglePage(l);

        // Respiration en fin de page. Sans elle, la dernière carte est collée au
        // bord bas de la zone défilante, et la liste déroulante de sa dernière
        // ligne n'a pas la place de s'ouvrir vers le bas.
        Layout.Spacer(Theme.GapXl);
    }

    /// <summary>
    /// Fiche d'un seul tenant, tout déroulé. Disposition d'origine, conservée
    /// pour qui préfère chercher au défilement plutôt qu'au clic.
    ///
    /// La confidentialité vient avant tout bloc de contenu : c'est elle qui
    /// décide qui voit quoi, et on la cherche en haut de page. Reléguée en fin
    /// de fiche, elle se trouvait au bout d'un long défilement, là où personne ne
    /// pense à aller la vérifier avant d'écrire sa biographie.
    /// </summary>
    private void DrawSinglePage(Loc l)
    {
        DrawVisibility(l);

        DrawStatus(l);
        DrawAvailability(l);
        DrawHooks(l);
        DrawGlances(l);
        DrawTraits(l);
        DrawBelonging(l);
        DrawSyncshells(l);
        DrawPreferences(l);
        DrawThemes(l);
        DrawIdentity(l);
        DrawStyling(l);
        DrawProfileLinks(l);
        DrawRelations(l);
        DrawDescription(l);
        DrawLimits(l);
        if (_profile is { } linked) RpProfileView.DrawLinks(linked, l, Tone);
    }

    /// <summary>
    /// Même fiche, répartie en cinq onglets.
    ///
    /// Quatorze blocs bout à bout font une page qu'on parcourt au jugé : ce qui
    /// se règle une fois par an y côtoie ce qu'on change tous les soirs. Le
    /// découpage suit l'usage et non la structure des données : ce qu'on vient
    /// modifier avant une soirée d'abord, ce qui décrit le personnage ensuite,
    /// la confidentialité à part parce qu'on y va exprès.
    ///
    /// Les blocs sont les mêmes méthodes que la page d'un seul tenant, appelées
    /// depuis un autre endroit : il n'y a pas deux fiches à maintenir, seulement
    /// deux façons de les ranger.
    /// </summary>
    private void DrawTabbed(Loc l)
    {
        Tabs.Tab[] tabs =
        [
            new("overview",  l.RpProfileTabOverview,  Icons.RpLive),
            new("character", l.RpProfileTabCharacter, Icons.Profile),
            new("play",      l.RpProfileTabPlay,      Icons.Sparkle),
            new("links",     l.RpProfileTabLinks,     Icons.Copy),
            new("privacy",   l.RpProfileTabPrivacy,   Icons.Hide),
        ];

        _tab = Tabs.Draw("rpprofiletabs", tabs, _tab, Tone);

        // Une saisie en attente n'est plus visible dès qu'on change d'onglet :
        // le bouton d'enregistrement est resté sur l'onglet d'où elle vient. Le
        // rappel suit donc l'utilisateur, sur tous les onglets, tant que rien
        // n'est parti. Rien n'est perdu pour autant : les valeurs saisies restent
        // en mémoire jusqu'au prochain chargement de la fiche.
        if (_textDirty)
        {
            Layout.Spacer(Theme.GapXs);
            Text.WithIcon(Icons.Warning, l.RpProfileTabUnsaved, Theme.Idle, Theme.Idle);
        }

        Layout.Spacer(Theme.GapS);

        switch (_tab)
        {
            case "character":
                DrawIdentity(l);
                DrawTraits(l);
                DrawDescription(l);
                DrawBelonging(l);
                DrawStyling(l);
                break;

            case "play":
                DrawHooks(l);
                DrawPreferences(l);
                DrawThemes(l);
                DrawLimits(l);
                DrawRelations(l);
                break;

            case "links":
                DrawProfileLinks(l);
                DrawSyncshells(l);
                if (_profile is { } linked) RpProfileView.DrawLinks(linked, l, Tone);
                break;

            case "privacy":
                DrawVisibility(l);
                break;

            // « Aperçu » sert aussi de repli : un onglet inconnu, laissé par une
            // version antérieure, ouvre la fiche là où elle est la plus utile.
            default:
                DrawStatus(l);
                DrawAvailability(l);
                DrawGlances(l);
                break;
        }
    }

    // ─── Chargement ───────────────────────────────────────────────────────────

    /// <summary>
    /// Signale que la fenêtre principale vient de s'ouvrir.
    ///
    /// Une fiche modifiée sur le site ne se voyait qu'après un redémarrage du
    /// plugin : la page ne se chargeait qu'au changement de personnage, et gardait
    /// donc indéfiniment ce qu'elle avait lu la première fois.
    /// </summary>
    public void NotifyWindowOpened() => _refreshPending = true;

    /// <summary>
    /// Rafraîchissement d'ouverture, sous conditions.
    ///
    /// Le signal est consommé quoi qu'il arrive : c'est une ouverture précise
    /// qu'il représente, pas une intention à conserver jusqu'à ce qu'elle devienne
    /// réalisable.
    ///
    /// Il ne recharge jamais pendant une saisie en cours : <see cref="Load"/>
    /// repasse par <see cref="Reset"/>, et écraser le travail de l'utilisateur
    /// serait bien pire que la fiche périmée qu'on corrige ici.
    /// </summary>
    private void AutoRefresh(string key)
    {
        _refreshPending = false;

        if (_dirty || _statusDirty || _loading || _saving) return;
        if (DateTime.UtcNow - _lastFetchedAt < AutoRefreshCooldown) return;

        Load(key);
    }

    private void Load(string key)
    {
        // Au changement de personnage seulement : le cache évite un écran vide, le
        // réseau le rafraîchit ensuite. Sur un rechargement à clé constante, y
        // repasser ferait clignoter la fiche déjà à l'écran, et la ferait régresser
        // vers une version partielle (le cache ne porte ni les relations, ni le
        // rôle d'équipe) le temps de la requête.
        var slot = _selectedProfileId;

        if (_loadedFor != key || _loadedProfile != slot)
        {
            // L'éditeur ouvert porte le texte de la fiche qu'on quitte : le
            // laisser ouvert inviterait à valider dans le vide.
            Plugin.CancelMarkdownEditor();

            _loadedFor     = key;
            _loadedProfile = slot;

            // Le cache ne vaut que pour la fiche publiée : il est indexé par
            // personnage et n'en connaît qu'une. Ouvrir une fiche en réserve
            // part donc d'un écran vide, le temps de la réponse.
            _profile = slot.Length == 0 && config.RpProfiles.TryGetValue(key, out var cached)
                ? ToDto(cached)
                : null;
            _profileFromNetwork = false;
            Reset();
        }

        // L'aperçu interroge la route publique par un appel distinct : sans ce
        // rappel, la fenêtre ouverte à côté continuerait d'afficher l'état d'avant.
        Plugin.RefreshRpProfilePreview();

        // La liste des fiches suit le même chargement : une fiche créée depuis
        // le site doit apparaître dans le sélecteur sans redémarrer le jeu.
        LoadSlots(key);

        _loading = true;
        _ = Task.Run(async () =>
        {
            var fetched = await Plugin.Api.GetRpProfileAsync(slot.Length > 0 ? slot : null);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _loading = false;
                if (fetched == null) return;
                if (_loadedFor != key || _loadedProfile != slot) return;

                _profile = fetched;
                _profileFromNetwork = true;
                _lastFetchedAt = DateTime.UtcNow;

                // Seule la fiche publiée alimente le cache : y écrire une fiche
                // en réserve ferait afficher celle-ci au prochain démarrage, à
                // la place de ce que les autres voient.
                if (slot.Length == 0)
                {
                    config.RpProfiles[key] = FromDto(fetched);
                    config.Save();
                }

                // L'utilisateur a pu commencer à saisir pendant la requête : ses
                // champs priment alors sur la réponse, que la fiche lue sert de
                // base à l'enregistrement suivant. Le statut compte au même
                // titre, tout court qu'il soit : il s'écrit à la main lui aussi.
                if (!_dirty && !_statusDirty) Reset();
            });
        });
    }

    // Les fiches du personnage, pour le sélecteur. Vide tant que le serveur n'a
    // pas répondu, ou s'il est antérieur aux fiches multiples : dans les deux
    // cas la rangée ne s'affiche pas, et le plugin travaille la fiche publiée
    // comme il l'a toujours fait.
    private List<Api.RpProfileSummaryDto> _slots = [];
    private int    _slotMax;
    /// <summary>La liste a été demandée et n'est pas revenue.</summary>
    private bool   _slotsFailed;
    private bool   _switching;
    private string _slotError    = string.Empty;
    private string _confirmDelete = string.Empty;

    /// <summary>Identifiant de la fiche publiée, ou vide si le serveur ne l'a pas dit.</summary>
    private string ActiveSlotId => _slots.FirstOrDefault(s => s.IsActive)?.Id ?? string.Empty;

    /// <summary>
    /// Fiche ouverte pour édition. Vide veut dire « celle que le personnage
    /// publie », ce qui est l'état par défaut et le seul que connaissait le
    /// plugin avant de savoir en ouvrir une autre.
    ///
    /// Choisir n'est pas publier : on travaille une fiche gardée en réserve
    /// sans que personne la voie, et un bouton distinct la met en ligne.
    /// </summary>
    private string _selectedProfileId = string.Empty;

    /// <summary>La fiche effectivement chargée, pour savoir quand recharger.</summary>
    private string _loadedProfile = string.Empty;

    /// <summary>Identifiant de la fiche travaillée, publiée ou non.</summary>
    private string SelectedSlotId =>
        _selectedProfileId.Length > 0 ? _selectedProfileId : ActiveSlotId;

    /// <summary>
    /// Les fiches du personnage, et tout ce qu'on en fait.
    ///
    /// Affichée dès la première fiche, et non à partir de deux : c'est le seul
    /// endroit où un joueur qui n'ouvre jamais le site apprend qu'il peut en
    /// tenir plusieurs. La masquer tant qu'il n'y en a qu'une revenait à cacher
    /// la fonction à ceux à qui elle s'adresse.
    ///
    /// Le plugin n'édite jamais que la fiche publiée : basculer publie la fiche
    /// choisie, puis recharge tout.
    /// </summary>
    private void DrawProfileSwitcher(Loc l)
    {
        // Le plafond ne vaut zéro que si le serveur n'a pas répondu, ou s'il est
        // antérieur aux fiches multiples. La carte reste sinon affichée même
        // sans aucune fiche : c'est là qu'un personnage tout juste lié apprend
        // qu'il peut en tenir plusieurs, et qu'il nomme la première.
        //
        // Un échec le dit, plutôt que de faire disparaître la carte en silence :
        // sans cette ligne, une fiche servie depuis le cache local donne un
        // écran presque normal dont il manque une section, sans rien qui
        // explique pourquoi.
        if (_slotsFailed && _slotMax == 0)
        {
            using var failed = Card.Begin("rp_slots_failed", interactive: false);
            Layout.SectionHeader(l.RpProfileSlots, Icons.Profile, tone: Tone);
            Text.Small(l.RpProfileSlotsUnavailable, Theme.TextMuted);
            return;
        }

        if (_slotMax == 0) return;

        using var card = Card.Begin("rp_slots", interactive: false);

        Layout.SectionHeader(l.RpProfileSlots, Icons.Profile, count: _slots.Count, tone: Tone);
        Text.Small(l.RpProfileSlotsHint, Theme.TextMuted);
        Layout.Spacer(Theme.GapXs);

        // Le nom de la fiche publiée, modifiable ici : c'est lui qui nomme le
        // bouton juste en dessous, et une fiche sans nom ne se distingue pas des
        // autres au moment d'en choisir une.
        if (Inputs.Field("##rpname", l.RpProfileRpName, ref _rpName, 80,
                         help: l.RpProfileRpNameHint))
            MarkDirty();

        Layout.Spacer(Theme.GapXs);

        using (ImRaii.Disabled(_switching))
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (i > 0) ImGui.SameLine(0f, Theme.S(Theme.GapXs));

                var entry = _slots[i];
                var label = string.IsNullOrWhiteSpace(entry.RpName)
                    ? string.Format(l.RpProfileSlotUnnamed, i + 1)
                    : entry.RpName!;

                // L'étoile marque la fiche publiée ; la teinte pleine marque
                // celle qu'on est en train de travailler. Les deux coïncident la
                // plupart du temps, et se séparent quand on prépare une fiche.
                if (entry.IsActive) label = $"{Icons.Sparkle.S()}  {label}";

                if (Btn.Draw(label,
                             entry.Id == SelectedSlotId ? BtnTone.Primary : BtnTone.Ghost,
                             BtnSize.Small, id: $"rpslot_{entry.Id}"))
                    Select(entry.Id);
            }

            if (_slots.Count < _slotMax)
            {
                ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                if (Btn.Draw(l.RpProfileSlotNew, BtnTone.Ghost, BtnSize.Small, Icons.Plus,
                             id: "rpslot_new"))
                    CreateSlot();
            }

            Layout.Spacer(Theme.GapXs);

            // Publier la fiche travaillée, quand ce n'est pas déjà celle que le
            // personnage montre. C'est le seul geste qui change quelque chose
            // pour les autres joueurs, d'où son bouton à part.
            if (SelectedSlotId.Length > 0 && SelectedSlotId != ActiveSlotId
                && Btn.Draw(l.RpProfileSlotActivate, BtnTone.Primary, BtnSize.Small, Icons.Sparkle,
                            id: "rpslot_pub"))
                RunSlotAction(() => Plugin.Api.ActivateRpProfileAsync(SelectedSlotId));

            // Dupliquer copie la fiche travaillée.
            if (_slots.Count > 0 && _slots.Count < _slotMax
                && Btn.Draw(l.RpProfileSlotDuplicate, BtnTone.Ghost, BtnSize.Small, Icons.Copy,
                            id: "rpslot_dup"))
                RunSlotAction(() => Plugin.Api.DuplicateRpProfileAsync(SelectedSlotId));

            // Deux clics, comme partout ailleurs dans le plugin : le premier
            // arme le bouton, le second supprime. Une fiche emporte ses images.
            if (_slots.Count > 1)
            {
                ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                var armed = _confirmDelete == SelectedSlotId && SelectedSlotId.Length > 0;
                if (Btn.Draw(armed ? l.RpProfileSlotDeleteConfirm : l.RpProfileSlotDelete,
                             armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Small, Icons.Trash,
                             id: "rpslot_del"))
                {
                    if (armed)
                    {
                        var doomed = SelectedSlotId;
                        // Revenir à la fiche publiée : celle qu'on supprime ne
                        // sera plus là pour être rechargée.
                        _selectedProfileId = string.Empty;
                        RunSlotAction(() => Plugin.Api.DeleteRpProfileAsync(doomed));
                    }
                    else _confirmDelete = SelectedSlotId;
                }
                if (!ImGui.IsItemHovered() && armed) _confirmDelete = string.Empty;
            }
        }

        if (_slotError.Length > 0)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small(_slotError, Theme.Danger);
        }
    }

    /// <summary>
    /// Enchaîne un geste sur les fiches et le rechargement complet.
    ///
    /// Créer, dupliquer et supprimer changent la liste, et supprimer peut même
    /// changer la fiche publiée : tout relire est plus sûr que de deviner l'état
    /// résultant depuis le plugin.
    /// </summary>
    private void RunSlotAction(Func<Task<Api.RpSlotResult>> action)
    {
        _switching     = true;
        _slotError     = string.Empty;
        _confirmDelete = string.Empty;

        var key = _loadedFor;

        _ = Task.Run(async () =>
        {
            var result = await action();
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _switching = false;
                if (_loadedFor != key) return;

                var l = Plugin.L;
                switch (result)
                {
                    case Api.RpSlotResult.Quota:       _slotError = l.RpProfileSlotErrQuota; return;
                    case Api.RpSlotResult.LastProfile: _slotError = l.RpProfileSlotErrLast;  return;
                    case Api.RpSlotResult.Failed:      _slotError = l.RpProfileSlotErr;      return;
                }

                ForceReload(key);
            });
        });
    }

    /// <summary>
    /// Oublie tout ce qui décrit la fiche courante, cache local compris.
    ///
    /// Le chargement lit le cache en premier pour éviter un écran vide : l'y
    /// laisser ferait repartir l'enregistrement suivant du contenu d'avant, qui
    /// écraserait la fiche fraîchement publiée. Mieux vaut un squelette le temps
    /// de la réponse.
    /// </summary>
    private void ForceReload(string key)
    {
        // Même raison qu'au changement de personnage : la fiche derrière la clé
        // n'est plus la même, le texte ouvert ne la décrit plus.
        Plugin.CancelMarkdownEditor();

        config.RpProfiles.Remove(key);
        config.Save();

        _loadedFor     = string.Empty;
        _loadedProfile = string.Empty;
        _profile       = null;
        _dirty         = false;
    }

    /// <summary>
    /// Publie une autre fiche, puis force le rechargement.
    ///
    /// Le cache local est réécrit par <see cref="Load"/> : sans ce rechargement,
    /// la fenêtre continuerait d'afficher l'ancienne fiche, et le prochain
    /// enregistrement l'écrirait par-dessus la nouvelle.
    /// </summary>
    /// <summary>
    /// Ce que dit un échec d'enregistrement.
    ///
    /// « L'enregistrement a échoué » seul n'aide personne : les trois causes
    /// possibles se règlent de trois façons différentes, et rien à l'écran ne
    /// permettait de les distinguer. L'état du jeton est déjà tenu par le client
    /// d'API, il suffisait de le lire.
    /// </summary>
    private static string SaveFailureText(Loc l) =>
        !Plugin.Api.HasToken       ? l.SaveFailedNoToken
        : !Plugin.Api.IsTokenValid ? l.SaveFailedToken
        : l.SaveFailed;

    /// <summary>
    /// Ouvre une fiche pour la travailler, sans rien publier.
    ///
    /// Une saisie en cours serait perdue au rechargement : elle appartient à la
    /// fiche qu'on quitte, et la porter sur la suivante l'écrirait au mauvais
    /// endroit. Le changement est donc refusé tant que rien n'est enregistré.
    /// </summary>
    private void Select(string profileId)
    {
        if (_dirty || _statusDirty)
        {
            _slotError = Plugin.L.RpProfileSlotDirty;
            return;
        }

        _slotError = string.Empty;
        _confirmDelete = string.Empty;
        _selectedProfileId = profileId == ActiveSlotId ? string.Empty : profileId;
    }

    /// <summary>
    /// Crée une fiche et l'ouvre aussitôt.
    ///
    /// La créer pour devoir ensuite la retrouver dans la liste serait un pas de
    /// trop, et une fiche vierge n'a d'intérêt qu'une fois remplie.
    /// </summary>
    private void CreateSlot()
    {
        _slotError     = string.Empty;
        _confirmDelete = string.Empty;

        var key = _loadedFor;

        // L'assistant fait le travail : il pose les questions qui rendent une
        // fiche utile, la crée, puis nous rend son identifiant pour l'ouvrir.
        // Une fiche vierge de plus dans la liste ne disait pas quoi en faire.
        Plugin.OpenRpProfileWizard(profileId =>
        {
            if (_loadedFor != key) return;
            _selectedProfileId = profileId;
            ForceReload(key);
        });
    }

    /// <summary>
    /// Relit la liste des fiches du personnage, en tâche de fond.
    ///
    /// La réponse est rattachée au personnage qui l'a demandée : sans cela, en
    /// changeant de personnage pendant la requête, le sélecteur montrerait les
    /// fiches du précédent, et un clic basculerait la mauvaise.
    /// </summary>
    private void LoadSlots(string key)
    {
        _ = Task.Run(async () =>
        {
            var list = await Plugin.Api.GetRpProfileListAsync();
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                if (_loadedFor != key) return;

                _slotsFailed = list == null;
                if (list == null) return;

                _slots   = list.Profiles;
                _slotMax = list.Max;
            });
        });
    }

    /// <summary>Recharge la copie de travail depuis la fiche connue.</summary>
    private void Reset()
    {
        var p = _profile;

        for (var i = 0; i < _hooks.Length; i++)
            _hooks[i] = p != null && i < p.Hooks.Length ? p.Hooks[i] : string.Empty;

        // Seuls les emplacements réellement enregistrés sont ouverts : la fiche
        // vide n'affiche donc aucun champ, juste son bouton d'ajout.
        _glanceCount = Math.Min(p?.Glances.Length ?? 0, MaxGlances);
        _glanceArmed = -1;

        for (var i = 0; i < MaxGlances; i++)
        {
            var glance = i < _glanceCount ? p!.Glances[i] : null;
            var icon   = glance != null ? Array.IndexOf(GlanceIconKeys, glance.Icon) : -1;

            // Une icône inconnue, réglée par une version plus récente du site,
            // retombe sur l'icône neutre : la fiche reste éditable, et seule
            // l'icône change, pas le texte que le joueur a écrit.
            _glanceIcons[i]  = icon >= 0 ? icon : GlanceDefaultIcon;
            _glanceTitles[i] = glance?.Title ?? string.Empty;
            _glanceBodies[i] = glance?.Body  ?? string.Empty;

            // Un emplacement vierge naît allumé : le remplir suffit à le montrer,
            // sans second geste à comprendre.
            _glanceActive[i] = glance?.Active ?? true;
        }

        var syncshells = ParseSyncshells(p?.Syncshells);
        for (var i = 0; i < MaxSyncshells; i++)
        {
            var entry = i < syncshells.Length ? syncshells[i] : null;
            var type  = entry != null ? Array.IndexOf(SyncTypeKeys, entry.Type) : -1;

            // Un service disparu de la liste retombe sur « autre », en conservant
            // son nom : mieux vaut une entrée réétiquetée qu'une entrée perdue.
            _syncTypes[i] = type >= 0 ? type : (entry != null ? SyncTypeKeys.Length - 1 : 0);
            _syncNames[i] = entry?.Name ?? (type >= 0 ? string.Empty : entry?.Type ?? string.Empty);
            _syncIds[i]   = entry?.Id ?? string.Empty;
        }

        _currentQuest  = p?.CurrentQuest ?? string.Empty;
        _currently     = p?.Currently    ?? string.Empty;

        // Un serveur ou un cache antérieurs ne portent pas l'état : on affiche
        // alors le défaut du serveur (« ooc »), qui est ce que la fiche vaut
        // réellement en base tant qu'aucun tag n'a été vu. L'état n'est plus
        // réglable ici, il suit le tag « Jeu de rôle » du jeu.
        _icStateIndex  = Math.Max(0, Array.IndexOf(IcStateKeys, p?.IcState ?? "ooc"));
        _statusDirty   = false;
        _levelIndex    = Math.Max(0, Array.IndexOf(LevelKeys,    p?.RpLevel      ?? "casual"));
        _approachIndex = Math.Max(0, Array.IndexOf(ApproachKeys, p?.ApproachMode ?? "come_to_me"));
        _langFr        = p?.Languages.Contains("fr") ?? true;
        _langEn        = p?.Languages.Contains("en") ?? false;

        // Une fiche sans aucune langue existe en base mais ne peut pas être
        // réenregistrée : le serveur en exige une. La correction vivait dans le
        // rendu des préférences, donc seulement quand cet onglet était affiché ;
        // ici elle s'applique quoi qu'on regarde.
        if (!_langFr && !_langEn) _langFr = true;

        _height      = p?.Height      ?? string.Empty;
        _build       = p?.Build       ?? string.Empty;
        _marks       = p?.Marks       ?? string.Empty;
        _voice       = p?.Voice       ?? string.Empty;
        _freeCompany = p?.FreeCompany ?? string.Empty;
        _allegiance  = p?.Allegiance  ?? string.Empty;
        _quote       = p?.Quote       ?? string.Empty;
        _rpName      = p?.RpName      ?? string.Empty;
        _nickname     = p?.Nickname     ?? string.Empty;
        _nameplateIndex = Math.Max(0, Array.IndexOf(NameplateKeys, p?.NameplateName ?? string.Empty));
        _age          = p?.Age          ?? string.Empty;
        _themeSongUrl = p?.ThemeSongUrl ?? string.Empty;
        _externalUrl  = p?.ExternalUrl  ?? string.Empty;

        // Habillage : l'index 0 des deux listes vaut « aucun effet », d'où le
        // décalage de un. Une valeur inconnue retombe donc sur « aucun ».
        _rpTitle        = p?.RpTitle ?? string.Empty;
        _frameIndex     = Math.Max(0, Array.IndexOf(FrameKeys, p?.FrameStyle ?? "") + 1);
        _titleAnimIndex = Math.Max(0, Array.IndexOf(TitleAnimKeys, p?.TitleAnimation ?? "") + 1);

        var accent = RpProfileView.Accent(p);
        _accent1   = HexToRgb(p?.AccentColor, new Vector3(accent.X, accent.Y, accent.Z));
        _accent2On = p?.AccentColor2 is { Length: > 0 };
        _accent2   = HexToRgb(p?.AccentColor2, _accent1);

        _pronouns    = p?.Pronouns    ?? string.Empty;
        _origin      = p?.Origin      ?? string.Empty;
        _occupation  = p?.Occupation  ?? string.Empty;

        // Une valeur écrite par une version antérieure du vocabulaire retombe
        // sur « non précisé » plutôt que d'afficher la première race au hasard.
        _raceIndex = Math.Max(0, Array.IndexOf(RaceKeys, p?.Race ?? string.Empty));

        _relationCount = Math.Min(p?.Relations.Length ?? 0, MaxRelations);
        _relationArmed = -1;
        for (var i = 0; i < MaxRelations; i++)
        {
            var relation = i < _relationCount ? p!.Relations[i] : null;
            _relationNames[i] = relation?.TargetName ?? string.Empty;
            _relationNotes[i] = relation?.Note       ?? string.Empty;
            // Un type retiré du vocabulaire depuis l'enregistrement retombe sur
            // « autre » plutôt que sur le premier de la liste, qui dirait « allié »
            // à la place de l'auteur et serait réenregistré tel quel.
            var kind = Array.IndexOf(RelationKindKeys, relation?.Kind ?? "");
            _relationKinds[i] = kind >= 0 ? kind : Array.IndexOf(RelationKindKeys, "other");
        }

        _appearance  = p?.Appearance  ?? string.Empty;
        _personality = p?.Personality ?? string.Empty;
        _background  = p?.Background  ?? string.Empty;
        _limits      = p?.Limits      ?? string.Empty;

        _themes.Clear();
        _themes.AddRange((p?.Themes ?? []).Where(t => ThemeKeys.Contains(t)));
        _avoidThemes.Clear();
        _avoidThemes.AddRange((p?.AvoidThemes ?? []).Where(t => ThemeKeys.Contains(t)));
        _deityIndex  = Math.Max(0, Array.IndexOf(DeityKeys, p?.Deity ?? string.Empty));

        _visInGame    = p?.IsPublic        ?? true;
        _visIndexable = p?.SearchIndexable ?? false;
        _visStaffBadge = p?.StaffBadgeVisible ?? false;

        var audience = ParseSectionVisibility(p?.SectionVisibility);
        for (var i = 0; i < SectionKeys.Length; i++)
        {
            var stored = audience.TryGetValue(SectionKeys[i], out var value) ? value : null;
            var index  = stored != null ? Array.IndexOf(AudienceKeys, stored) : -1;
            _sectionAudience[i] = index >= 0
                ? index
                : Array.IndexOf(AudienceKeys, SectionDefaultKeys[i]);
        }

        _dirty      = false;
        _textDirty  = false;
        _autoSaveAt = null;
    }

    /// <summary>
    /// Décode l'audience par section. Une valeur absente ou illisible donne un
    /// dictionnaire vide, chaque section retombant alors sur son défaut : la
    /// fiche doit rester réglable même si la colonne a été écrite par une version
    /// antérieure.
    /// </summary>
    private static Dictionary<string, string> ParseSectionVisibility(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch { return []; }
    }

    // ─── Sections ─────────────────────────────────────────────────────────────

    private void DrawHeader((string Name, int WorldId) character, Loc l)
    {
        // Même habillage que sur la fiche vue par les autres : sans cela, le
        // joueur ne verrait jamais en jeu ce qu'il a réglé sur le site.
        var accent    = RpProfileView.Accent(_profile);
        var accent2   = RpProfileView.Accent2(_profile);
        var hasAccent = Theme.TryParseHex(_profile?.AccentColor) != null;

        var banner = Textures.Get(_profile?.BannerUrl);

        // Un effet de cadre sans couleur choisie mérite quand même son liseré.
        var hasFrame = hasAccent || _profile?.FrameStyle is { Length: > 0 };

        // Relevée avant la carte, comme sur la fiche vue par les autres : c'est
        // d'elle que se déduit le bas de la bannière, sous lequel le bloc de nom
        // doit rester.
        var cardOrigin = ImGui.GetCursorScreenPos();

        using var card = Card.Begin("rp_header", interactive: false,
            background:   hasAccent ? RpProfileView.HeaderBackground(accent, accent2) : null,
            accent:       hasAccent ? accent : null,
            banner:       banner,
            bannerHeight: RpProfileView.HeaderBanner);

        var overlap = RpProfileView.HeaderOverlap(banner != null);
        if (overlap > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - Theme.S(overlap));

        RpProfileView.DrawPortrait(_profile?.PortraitUrl, character.Name,
            height:     RpProfileView.HeaderPortrait,
            status:     Plugin.CurrentCharacterAvailable ? Theme.Online : null,
            frame:      hasFrame ? accent : null,
            frameStyle: _profile?.FrameStyle,
            frame2:     accent2);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        // Le portrait mange une bonne part de la largeur, et ces textes ne se
        // replient pas d'eux-mêmes : sans borne, un nom RP un peu long sort de
        // la carte.
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X
                              - Card.RightInset);

        // Mêmes chaînes pour la mesure et pour le dessin : le bloc de nom est
        // centré sur la hauteur du portrait, un écart entre les deux se verrait.
        // Pas de ligne « personnage · serveur » ici, on est sur sa propre fiche.
        var displayName = _profile?.RpName is { Length: > 0 } rpName ? rpName : character.Name;
        var nickname    = _profile?.Nickname is { Length: > 0 } nick ? $"« {nick} »" : null;

        // Pas de badge d'équipe dans ce bloc : sur sa propre fiche il reste avec
        // les chips de synthèse, cet écran ayant sa propre mise en page.
        RpProfileView.HeaderNameFiller(null, displayName, _profile?.RpTitle, null, nickname,
                                       RpProfileView.HeaderNameMinTop(cardOrigin, banner != null));

        Text.Title(displayName);

        // Titre court réservé aux membres, à la même place que sur la fiche vue
        // par les autres.
        AnimatedText.Draw(_profile?.RpTitle, accent2, _profile?.TitleAnimation, accent);

        if (nickname != null) Text.Small(nickname);

        ImGui.PopTextWrapPos();
        ImGui.EndGroup();

        // Citation, disponibilité et chips sous le portrait, comme sur la fiche
        // vue par les autres.
        if (_profile?.Quote is { Length: > 0 } quote)
        {
            Layout.Spacer(Theme.GapS);
            Text.Small($"« {quote} »", accent);
        }

        // Les chips manquaient ici : on ne voyait sur sa propre fiche ni son
        // niveau, ni ses langues, ni son marquage sensible, alors que les autres
        // joueurs, eux, les voyaient. Les deux entêtes doivent concorder, au
        // statut de disponibilité près, qui n'a de sens que sur la sienne.
        if (_profile is { } p)
        {
            Layout.Spacer(Theme.GapS);

            // Sa propre fiche : le payload porte le rôle même sans consentement,
            // pour que la case qui l'active reste atteignable. C'est donc ici, et
            // seulement ici, que la case doit être relue avant d'afficher.
            if (RpProfileView.StaffBadge(p, l, requireConsent: true))
                ImGui.SameLine(0f, Theme.S(Theme.GapXs));

            Chip.Colored(RpProfileView.LevelLabel(p.RpLevel, l), accent);

            if (p.Languages.Length > 0)
            {
                ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                Chip.Draw(string.Join(" / ", p.Languages.Select(RpProfileView.LanguageLabel)),
                          ChipTone.Neutral);
            }

            if (p.Nsfw)
            {
                ImGui.SameLine(0f, Theme.S(Theme.GapXs));
                Chip.Draw(l.RpProfileNsfw, ChipTone.Danger, Icons.Warning);
            }
        }

        // Le lien vers l'éditeur en ligne vit maintenant dans le bandeau
        // explicatif juste en dessous : deux boutons identiques à l'écran, dont un
        // sans son explication, n'apportaient rien.
    }

    /// <summary>
    /// Rappelle noir sur blanc le partage des rôles avec le site, et porte le lien
    /// vers l'éditeur en ligne.
    ///
    /// L'information tenait dans l'infobulle d'un bouton du bandeau de titre :
    /// autant dire qu'elle n'était lue par personne, et rien à l'écran n'expliquait
    /// pourquoi l'identité, les relations ou l'histoire s'affichent sans pouvoir
    /// être modifiées.
    /// </summary>
    private void DrawWebNotice(Loc l)
    {
        Feedback.Alert(Theme.Accent, Icons.Info, l.RpProfileWebNoticeTitle,
                       l.RpProfileWebNoticeBody,
                       () =>
                       {
                           if (Btn.Draw(l.RpProfileEditOnline, BtnTone.Primary, BtnSize.Medium,
                                        Icons.External, id: "rp_editonline"))
                               OpenSite(EditUrl());
                       });
    }

    /// <summary>
    /// Instant présent : état de jeu et statut du moment, les deux seuls champs
    /// de la fiche qui se changent en cours de soirée.
    ///
    /// Éditables en jeu sans réserve, contrairement aux textes longs : ce sont
    /// une liste à trois entrées et une ligne de texte, et c'est justement en
    /// jouant qu'on veut les changer. Devoir sortir du jeu pour dire qu'on est
    /// hors RP est le meilleur moyen de ne jamais le dire.
    ///
    /// L'enregistrement passe par la route dédiée et non par le bouton commun de
    /// la page : celui-ci publierait toute la fiche, y compris une saisie en
    /// cours ailleurs.
    /// </summary>
    private void DrawStatus(Loc l)
    {
        using var card = Card.Begin("rp_status", interactive: false);

        Layout.SectionHeader(l.RpProfileStatus, Icons.RpLive, tone: Tone);
        Text.Small(l.RpProfileStatusHint);
        Layout.Spacer(Theme.GapS);

        // L'état de jeu se lit, il ne se règle plus : le jeu a déjà son tag
        // « Jeu de rôle », et un second interrupteur ici donnait deux réponses
        // contradictoires à la même question. Le tag fait foi, le plugin le
        // recopie, et n'écrit jamais /jdr à la place du joueur.
        var stateKey = CurrentStateKey();
        Text.Small(l.RpProfileIcState);
        Chip.Draw(RpProfileView.IcStateLabel(stateKey, l),
                  RpProfileView.IcStateTone(stateKey), Icons.RpLive);
        Text.Small(l.RpProfileIcStateHint);

        Layout.Spacer(Theme.GapS);

        // Le statut, lui, s'écrit lettre par lettre : l'envoyer à chaque frappe
        // ferait autant de requêtes que de caractères.
        if (Inputs.Field("##currently", l.RpProfileCurrently, ref _currently, MaxCurrently,
                         placeholder: l.RpProfileCurrentlyExample, showCounter: true))
            _statusDirty = true;

        DrawStatusSaveRow(l);
    }

    /// <summary>
    /// Retour d'enregistrement propre au statut. Il ne partage pas celui de la
    /// fiche : les deux s'enregistrent séparément, et un « Enregistré » commun
    /// laisserait croire qu'un clic ici a publié le reste de la page.
    /// </summary>
    private void DrawStatusSaveRow(Loc l)
    {
        var justSaved = DateTime.UtcNow < _statusSavedUntil;
        if (!_statusDirty && !justSaved && !_statusFailed) return;

        Layout.Spacer(Theme.GapS);

        if (justSaved && !_statusDirty)
        {
            Text.WithIcon(Icons.Check, l.RpProfileSaved, Theme.Online, Theme.Online);
            return;
        }

        // L'échec reste affiché tant que le statut n'est pas reparti : le texte
        // saisi est encore là, il n'a simplement pas atteint le serveur.
        if (_statusFailed)
        {
            Text.WithIcon(Icons.Warning, SaveFailureText(l), Theme.Danger, Theme.Danger);
            Layout.Spacer(Theme.GapXs);
        }

        if (!_statusDirty) return;

        if (Btn.Draw(_statusSaving ? l.Processing : l.Save, BtnTone.Primary, BtnSize.Medium,
                     Icons.Check, disabled: _statusSaving, id: "rpstatus_save"))
            SaveStatus();
    }

    /// <summary>
    /// Publie l'instant présent, et lui seul.
    ///
    /// Le statut part toujours, même vide : la chaîne vide est la façon
    /// d'effacer, alors qu'un null serait omis du corps et laisserait en place
    /// ce que le serveur a déjà. Vider le champ en jeu doit vider le statut.
    /// </summary>
    private void SaveStatus()
    {
        if (_statusSaving) return;
        _statusSaving = true;

        var currently = _currently.Trim();
        var icState   = CurrentStateKey();
        var key       = _loadedFor;

        _ = Task.Run(async () =>
        {
            var ok = await Plugin.Api.SetRpStatusAsync(currently, icState);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _statusSaving = false;
                _statusFailed = !ok;
                if (!ok) return;

                _statusDirty      = false;
                _statusSavedUntil = DateTime.UtcNow.AddSeconds(3);

                // La fiche en mémoire et son cache portent l'ancien statut : sans
                // cette recopie, l'entête et la fiche vue par les autres
                // continueraient d'afficher l'état précédent jusqu'au prochain
                // rafraîchissement réseau, c'est-à-dire précisément au moment où
                // l'on vient de dire le contraire.
                var stored = currently.Length > 0 ? currently : null;

                if (_profile is { } profile)
                {
                    profile.Currently = stored;
                    profile.IcState   = icState;
                }

                if (config.RpProfiles.TryGetValue(key, out var cached))
                {
                    cached.Currently = stored;
                    cached.IcState   = icState;
                    config.Save();
                }
            });
        });
    }

    /// <summary>
    /// État de jeu à afficher et à réenvoyer avec le statut du moment.
    ///
    /// Le cache du personnage connecté est tenu à jour par la surveillance du
    /// tag : le lire évite qu'un enregistrement du statut ne renvoie l'état
    /// chargé à l'ouverture de la page, devenu faux depuis un /jdr. Hors du jeu,
    /// il ne reste que ce que la fiche portait au chargement.
    /// </summary>
    private string CurrentStateKey()
    {
        if (Plugin.CurrentCharacter is not null) return Plugin.CurrentIcState();
        return IcStateKeys[Math.Clamp(_icStateIndex, 0, IcStateKeys.Length - 1)];
    }

    private void DrawAvailability(Loc l)
    {
        using var card = Card.Begin("rp_available", interactive: false,
                                    accent: Plugin.CurrentCharacterAvailabilityWanted ? Theme.Online : null);

        var available = Plugin.CurrentCharacterAvailabilityWanted;
        if (Inputs.ToggleRow(l.RpAvailableEnable, ref available, l.RpAvailableEnableHint, Icons.RpLive))
            Plugin.SetRpAvailability(available);
    }

    private void DrawHooks(Loc l)
    {
        using var card = Card.Begin("rp_hooks", interactive: false);

        Layout.SectionHeader(l.RpProfileHooks, Icons.Sparkle, tone: Tone);
        Text.Small(l.RpProfileHooksHint);
        Layout.Spacer(Theme.GapS);

        for (var i = 0; i < _hooks.Length; i++)
        {
            if (Inputs.Field($"##hook{i}", string.Empty, ref _hooks[i], 120,
                             placeholder: i == 0 ? l.RpProfileHooksExample : null))
                MarkDirty();
        }

        Layout.Spacer(Theme.GapS);
        if (Inputs.Field("##quest", l.RpProfileCurrentQuest, ref _currentQuest, 200))
            MarkDirty();

        DrawSaveRow(l);
    }

    /// <summary>
    /// Coup d'œil : jusqu'à cinq emplacements, chacun une icône, un titre et une
    /// ligne de description.
    ///
    /// Les emplacements s'ajoutent et se retirent un à un plutôt que d'occuper
    /// l'écran en permanence : quatre cases vides sous un seul détail rempli ne
    /// disaient rien de plus et noyaient le bloc.
    ///
    /// L'interrupteur éteint un emplacement sans le vider : ce qu'on ne montre
    /// pas ce soir se remontrera demain, et effacer pour masquer ferait
    /// retaper le texte à chaque fois.
    /// </summary>
    private void DrawGlances(Loc l)
    {
        using var card = Card.Begin("rp_glance", interactive: false);

        Layout.SectionHeader(l.RpProfileGlance, Icons.Show, tone: Tone);
        Text.Small(l.RpProfileGlanceHint);
        Layout.Spacer(Theme.GapS);

        // Le glyphe précède son nom dans chaque entrée du menu, et donc aussi
        // dans la ligne fermée : on choisissait jusque-là un dessin dans une
        // liste de mots, sans jamais voir ce qu'on choisissait.
        var iconLabels = GlanceIconKeys
            .Select(k => $"{Icons.Glance(k).S()}  {GlanceIconLabel(k, l)}")
            .ToArray();

        if (_glanceCount == 0) Text.Muted(l.RpProfileGlanceEmpty);

        for (var i = 0; i < _glanceCount; i++)
        {
            if (i > 0) Layout.Divider(Theme.GapS);

            DrawGlanceRemove(i, l);

            if (Inputs.Select($"##glanceicon{i}", string.Empty, ref _glanceIcons[i], iconLabels))
                MarkDirty();

            if (Inputs.Field($"##glancetitle{i}", string.Empty, ref _glanceTitles[i], MaxGlanceTitle,
                             placeholder: l.RpProfileGlanceExample))
                MarkDirty();

            // Description et interrupteur ne servent à rien tant que rien n'est
            // écrit : un emplacement sans titre n'est de toute façon pas envoyé,
            // et deux champs de moins allègent d'autant la saisie.
            if (_glanceTitles[i].Trim().Length == 0) continue;

            if (Inputs.Field($"##glancebody{i}", string.Empty, ref _glanceBodies[i], MaxGlanceBody,
                             placeholder: l.RpProfileGlanceBody))
                MarkDirty();

            if (Inputs.ToggleRow(l.RpProfileGlanceActive, ref _glanceActive[i]))
                MarkDirty();
        }

        Layout.Spacer(Theme.GapS);

        // Grisé plutôt que masqué une fois le plafond atteint : disparaître
        // laisserait croire que le bloc s'est cassé.
        if (Btn.Draw(l.RpProfileGlanceAdd, BtnTone.Secondary, BtnSize.Medium, Icons.Plus,
                     disabled: _glanceCount >= MaxGlances, id: "glance_add"))
            AddGlance();

        DrawSaveRow(l);
    }

    /// <summary>
    /// En-tête d'un emplacement : son rang à gauche, son retrait à droite.
    ///
    /// Retrait en deux temps, comme partout ailleurs dans le projet : le premier
    /// clic arme, le second efface, et quitter le bouton désarme.
    /// </summary>
    private void DrawGlanceRemove(int index, Loc l)
    {
        Text.Muted(string.Format(l.RpProfileGlanceSlot, index + 1));
        ImGui.SameLine();

        var armed   = _glanceArmed == index;
        var caption = armed ? l.RpProfileGlanceRemoveArm : l.RpProfileGlanceRemove;

        Layout.RightAlign(Btn.Measure(caption, Icons.Trash));

        if (Btn.Draw(caption, armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Medium,
                     Icons.Trash, id: $"glance_del_{index}"))
        {
            if (armed) RemoveGlance(index);
            else       _glanceArmed = index;
        }

        if (armed && !ImGui.IsItemHovered()) _glanceArmed = -1;
    }

    /// <summary>
    /// Ouvre un emplacement vierge en fin de liste. Rien n'est marqué modifié :
    /// un emplacement sans titre n'est pas enregistré, et allumer le bouton
    /// « Enregistrer » pour une case vide serait mentir sur ce qu'il reste à
    /// faire.
    /// </summary>
    private void AddGlance()
    {
        if (_glanceCount >= MaxGlances) return;

        _glanceIcons[_glanceCount]  = GlanceDefaultIcon;
        _glanceTitles[_glanceCount] = string.Empty;
        _glanceBodies[_glanceCount] = string.Empty;
        _glanceActive[_glanceCount] = true;
        _glanceCount++;
    }

    /// <summary>
    /// Retire un emplacement en décalant les suivants : les tableaux sont de
    /// taille fixe, c'est le compteur qui dit lesquels comptent, et laisser un
    /// trou au milieu renverrait un emplacement vide au serveur.
    /// </summary>
    private void RemoveGlance(int index)
    {
        for (var i = index; i < _glanceCount - 1; i++)
        {
            _glanceIcons[i]  = _glanceIcons[i + 1];
            _glanceTitles[i] = _glanceTitles[i + 1];
            _glanceBodies[i] = _glanceBodies[i + 1];
            _glanceActive[i] = _glanceActive[i + 1];
        }

        _glanceCount--;
        _glanceIcons[_glanceCount]  = GlanceDefaultIcon;
        _glanceTitles[_glanceCount] = string.Empty;
        _glanceBodies[_glanceCount] = string.Empty;
        _glanceActive[_glanceCount] = true;

        _glanceArmed = -1;
        MarkDirty();
    }

    /// <summary>
    /// Nom traduit d'une icône. Une clé absente du dictionnaire s'affiche telle
    /// quelle : mieux vaut un identifiant lisible qu'une entrée vide dans le
    /// sélecteur.
    /// </summary>
    private static string GlanceIconLabel(string key, Loc l) =>
        l.RpGlanceIconLabels.TryGetValue(key, out var label) ? label : key;

    private void DrawTraits(Loc l)
    {
        using var card = Card.Begin("rp_traits", interactive: false);

        Layout.SectionHeader(l.RpProfileTraits, Icons.Character, tone: Tone);
        Text.Small(l.RpProfileTraitsHint);
        Layout.Spacer(Theme.GapS);

        if (Inputs.Field("##height", l.RpProfileHeight, ref _height, 30)) MarkDirty();
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##build", l.RpProfileBuild, ref _build, 40)) MarkDirty();
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##voice", l.RpProfileVoice, ref _voice, 80)) MarkDirty();
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##marks", l.RpProfileMarks, ref _marks, 300)) MarkDirty();

        DrawSaveRow(l);
    }

    private void DrawBelonging(Loc l)
    {
        using var card = Card.Begin("rp_belonging", interactive: false);

        Layout.SectionHeader(l.RpProfileBelonging, Icons.World, tone: Tone);

        if (Inputs.Field("##fc", l.RpProfileFreeCompany, ref _freeCompany, 80)) MarkDirty();
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##allegiance", l.RpProfileAllegiance, ref _allegiance, 80)) MarkDirty();
        Layout.Spacer(Theme.GapXs);

        if (Inputs.Select("##deity", l.RpProfileDeity, ref _deityIndex,
                          [.. DeityKeys.Select(k => RpProfileView.DeityLabel(k, l))]))
            MarkToggleDirty("belong_deity");

        DrawAutoSaveAt("belong_deity", l);

        Layout.Spacer(Theme.GapS);
        if (Inputs.Field("##quote", l.RpProfileQuote, ref _quote, 300,
                         help: l.RpProfileQuoteHint))
            MarkDirty();

        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>
    /// Codes de sync, éditables en jeu contrairement aux relations : il n'y a
    /// rien à chercher ni à rapprocher, juste un identifiant à saisir, et c'est
    /// en jeu qu'on pense à l'ajouter.
    ///
    /// Emplacements fixes, sur le modèle des accroches : cinq lignes toujours
    /// présentes valent mieux que des boutons ajouter/supprimer à la souris dans
    /// une fenêtre ImGui.
    /// </summary>
    private void DrawSyncshells(Loc l)
    {
        using var card = Card.Begin("rp_sync", interactive: false);

        Layout.SectionHeader(l.RpProfileSyncshells, Icons.Copy, tone: Tone);
        Text.Small(l.RpProfileSyncshellsHint);
        Layout.Spacer(Theme.GapS);

        var typeLabels = SyncTypeKeys
            .Select(k => k == "autre" ? l.RpProfileSyncshellOther : RpProfileView.SyncshellLabel(
                new SyncshellEntryDto { Type = k }, l))
            .ToArray();

        for (var i = 0; i < MaxSyncshells; i++)
        {
            if (i > 0) Layout.Spacer(Theme.GapXs);

            if (Inputs.Select($"##synctype{i}", string.Empty, ref _syncTypes[i], typeLabels))
                MarkDirty();

            if (SyncTypeKeys[_syncTypes[i]] == "autre"
                && Inputs.Field($"##syncname{i}", string.Empty, ref _syncNames[i], 40,
                                placeholder: l.RpProfileSyncshellName))
                MarkDirty();

            if (Inputs.Field($"##syncid{i}", string.Empty, ref _syncIds[i], 100,
                             placeholder: l.RpProfileSyncshellId))
                MarkDirty();
        }

        DrawSaveRow(l);
    }

    /// <summary>
    /// Désérialise la chaîne stockée. Illisible, elle rend un tableau vide : la
    /// page doit rester utilisable même sur une fiche mal formée.
    /// </summary>
    private static SyncshellEntryDto[] ParseSyncshells(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<SyncshellEntryDto[]>(json) ?? [];
        }
        catch { return []; }
    }

    /// <summary>
    /// Relations, en consultation seule : les nouer se fait sur le site, où l'on
    /// dispose du clavier et de la recherche de personnages.
    /// </summary>
    /// <summary>
    /// Relations du personnage, éditables en jeu.
    ///
    /// Le nom de la cible est saisi librement : le serveur le rapproche ensuite
    /// d'un personnage à fiche publique s'il en trouve un, ce qui fait le lien.
    /// Un partenaire qui n'a pas de fiche reste donc citable, ce qui est le
    /// point : une relation raconte l'histoire de son auteur, pas celle de
    /// l'autre.
    /// </summary>
    private void DrawRelations(Loc l)
    {
        using var card = Card.Begin("rp_relations", interactive: false);
        Layout.SectionHeader(l.RpProfileRelations, Icons.Around, _relationCount, tone: Tone);

        var kindLabels = RelationKindKeys
            .Select(k => RpProfileView.RelationLabel(k, l))
            .ToArray();

        if (_relationCount == 0) Text.Muted(l.RpProfileRelationsEmpty);

        for (var i = 0; i < _relationCount; i++)
        {
            if (i > 0) Layout.Divider(Theme.GapS);

            DrawRelationRemove(i, l);

            if (Inputs.Field($"##relname{i}", string.Empty, ref _relationNames[i], MaxRelationName,
                             placeholder: l.RpProfileRelationName))
                MarkDirty();

            // Type et note n'ont rien à dire tant que la relation n'a pas de
            // cible : un emplacement sans nom n'est de toute façon pas envoyé.
            if (_relationNames[i].Trim().Length == 0) continue;

            if (Inputs.Select($"##relkind{i}", string.Empty, ref _relationKinds[i], kindLabels))
                MarkDirty();

            if (Inputs.Field($"##relnote{i}", string.Empty, ref _relationNotes[i], MaxRelationNote,
                             placeholder: l.RpProfileRelationNote))
                MarkDirty();
        }

        if (_relationCount < MaxRelations)
        {
            Layout.Spacer(Theme.GapS);
            if (Btn.Draw(l.RpProfileRelationAdd, BtnTone.Ghost, BtnSize.Small, Icons.Plus,
                         id: "rel_add"))
            {
                _relationNames[_relationCount] = string.Empty;
                _relationKinds[_relationCount] = 0;
                _relationNotes[_relationCount] = string.Empty;
                _relationCount++;
                MarkDirty();
            }
        }

        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>Ligne de titre d'une relation, avec sa suppression en deux clics.</summary>
    private void DrawRelationRemove(int index, Loc l)
    {
        Text.Muted(string.Format(l.RpProfileRelationSlot, index + 1));
        ImGui.SameLine();

        var armed   = _relationArmed == index;
        var caption = armed ? l.RpProfileGlanceRemoveArm : l.RpProfileGlanceRemove;

        Layout.RightAlign(Btn.Measure(caption, Icons.Trash));

        if (Btn.Draw(caption, armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Medium,
                     Icons.Trash, id: $"rel_del_{index}"))
        {
            if (armed) RemoveRelation(index);
            else       _relationArmed = index;
        }

        if (armed && !ImGui.IsItemHovered()) _relationArmed = -1;
    }

    /// <summary>Retire une relation en décalant les suivantes, sans laisser de trou.</summary>
    private void RemoveRelation(int index)
    {
        for (var i = index; i < _relationCount - 1; i++)
        {
            _relationNames[i] = _relationNames[i + 1];
            _relationKinds[i] = _relationKinds[i + 1];
            _relationNotes[i] = _relationNotes[i + 1];
        }

        _relationCount--;
        _relationNames[_relationCount] = string.Empty;
        _relationKinds[_relationCount] = 0;
        _relationNotes[_relationCount] = string.Empty;
        _relationArmed = -1;
        MarkDirty();
    }

    private void DrawPreferences(Loc l)
    {
        using var card = Card.Begin("rp_prefs", interactive: false);

        Layout.SectionHeader(l.RpProfilePreferences, Icons.Settings, tone: Tone);

        if (Inputs.Select("##rplevel", l.RpProfileLevel, ref _levelIndex,
                          [l.RpProfileLevelBeginner, l.RpProfileLevelCasual, l.RpProfileLevelConfirmed]))
            MarkToggleDirty("pref_level");
        DrawAutoSaveAt("pref_level", l);

        Layout.Spacer(Theme.GapS);

        if (Inputs.Select("##rpapproach", l.RpProfileApproach, ref _approachIndex,
                          [l.RpProfileApproachCome, l.RpProfileApproachIGo, l.RpProfileApproachEither]))
            MarkToggleDirty("pref_approach");
        DrawAutoSaveAt("pref_approach", l);

        Layout.Spacer(Theme.GapS);
        Text.Muted(l.RpProfileLanguages);
        Layout.Spacer(Theme.GapXs);

        if (Inputs.Toggle("##langfr", ref _langFr)) MarkToggleDirty("pref_lang");
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ImGui.AlignTextToFramePadding();
        Text.Body("Français");
        ImGui.SameLine(0f, Theme.S(Theme.GapL));
        if (Inputs.Toggle("##langen", ref _langEn)) MarkToggleDirty("pref_lang");
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ImGui.AlignTextToFramePadding();
        Text.Body("English");

        // Au moins une langue doit rester active, sinon la fiche n'apparaît
        // dans aucun filtre.
        if (!_langFr && !_langEn) _langFr = true;

        DrawAutoSaveAt("pref_lang", l);

        // Les thèmes ne s'affichent plus ici : ils ont leur propre carte, où ils
        // s'éditent. Les montrer aux deux endroits faisait se contredire une
        // liste éditée et une liste lue de la fiche, le temps que le serveur
        // réponde.

        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>
    /// Identité du personnage, éditable en jeu.
    ///
    /// Elle était en lecture seule au motif qu'elle se rédige une fois pour
    /// toutes : le raisonnement tenait tant que le site était le point d'entrée,
    /// il laissait sans recours un joueur qui n'ouvre jamais de navigateur.
    /// </summary>
    private void DrawIdentity(Loc l)
    {
        using var card = Card.Begin("rp_identity", interactive: false);
        Layout.SectionHeader(l.RpProfileIdentity, Icons.Profile, tone: Tone);

        if (Inputs.Select("##race", l.RpProfileRace, ref _raceIndex,
                          [.. RaceKeys.Select(k => k.Length == 0
                              ? l.RpProfileUnset
                              : RpProfileView.RaceLabel(k, l))]))
            MarkToggleDirty("identity_race");

        DrawAutoSaveAt("identity_race", l);

        Layout.Spacer(Theme.GapS);
        Layout.Spacer(Theme.GapS);
        if (Inputs.Field("##nickname", l.RpProfileNickname, ref _nickname, 60)) MarkDirty();

        // Placé juste sous le surnom, et non parmi les réglages : il ne se
        // comprend qu'en voyant les deux champs qu'il combine. Le nom RP, lui,
        // vit plus haut, là où il nomme la fiche.
        if (Inputs.Select("##nameplate", l.RpProfileNameplate, ref _nameplateIndex,
                          [l.RpProfileNameplateRpName,
                           l.RpProfileNameplateNickname,
                           l.RpProfileNameplateBoth]))
            MarkDirty();

        // Montrer le résultat plutôt que le décrire : c'est le seul moyen de
        // rendre lisibles les replis quand un des deux champs est vide.
        var nameplatePreview = Plugin.ComposeNameplateName(
            NameplateKeys[_nameplateIndex], _rpName, _nickname,
            Plugin.CurrentCharacter?.Name ?? string.Empty);

        Text.Small(nameplatePreview is { Length: > 0 } shown
                       ? string.Format(l.RpProfileNameplatePreview, shown)
                       : l.RpProfileNameplatePreviewNone,
                   Theme.TextMuted);
        Text.Small(l.RpProfileNameplateHint, Theme.TextMuted);
        Layout.Spacer(Theme.GapXs);

        if (Inputs.Field("##age", l.RpProfileAge, ref _age, 30))               MarkDirty();
        if (Inputs.Field("##pronouns", l.RpProfilePronouns, ref _pronouns, 30)) MarkDirty();
        if (Inputs.Field("##origin", l.RpProfileOrigin, ref _origin, 80))       MarkDirty();
        if (Inputs.Field("##occupation", l.RpProfileOccupation, ref _occupation, 80)) MarkDirty();

        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>
    /// Liens de la fiche : musique de thème et page personnelle.
    ///
    /// Ils voisinent avec les syncshells, qui sont eux aussi une adresse qu'on
    /// donne à ses partenaires plutôt qu'un trait du personnage.
    /// </summary>
    private void DrawProfileLinks(Loc l)
    {
        using var card = Card.Begin("rp_profilelinks", interactive: false);
        Layout.SectionHeader(l.RpProfileLinksSection, Icons.External, tone: Tone);

        if (Inputs.Field("##themesong", l.RpProfileThemeSong, ref _themeSongUrl, 300,
                         help: l.RpProfileThemeSongHint))
            MarkDirty();

        if (Inputs.Field("##externalurl", l.RpProfileExternalUrl, ref _externalUrl, 300,
                         help: l.RpProfileExternalUrlHint))
            MarkDirty();

        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>
    /// Habillage de la fiche, réservé aux membres.
    ///
    /// Le droit vient du serveur et non d'un calcul local : une échéance
    /// d'adhésion ne se juge pas sur l'horloge d'un poste de joueur. Sans droit,
    /// la carte reste affichée mais verrouillée, plutôt que masquée : savoir ce
    /// qu'on n'a pas fait partie de ce qui donne envie d'adhérer.
    /// </summary>
    private void DrawStyling(Loc l)
    {
        using var card = Card.Begin("rp_styling", interactive: false);
        Layout.SectionHeader(l.RpProfileStyling, Icons.Sparkle, tone: Tone);

        if (_profile?.CosmeticsAllowed != true)
        {
            Text.Small(l.RpProfileStylingLocked, Theme.TextMuted);
            return;
        }

        if (ImGui.ColorEdit3("##accent1", ref _accent1, ImGuiColorEditFlags.NoInputs))
            MarkDirty();
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ImGui.AlignTextToFramePadding();
        Text.Small(l.RpProfileAccent, Theme.TextMuted);

        // La seconde couleur est facultative : sans elle, la fiche garde un
        // aplat plutôt qu'un dégradé. Un interrupteur dit mieux « aucune » qu'une
        // teinte qu'il faudrait deviner comme neutre.
        var second = _accent2On;
        if (Inputs.ToggleRow(l.RpProfileAccent2, ref second)) { _accent2On = second; MarkDirty(); }

        if (_accent2On)
        {
            if (ImGui.ColorEdit3("##accent2", ref _accent2, ImGuiColorEditFlags.NoInputs))
                MarkDirty();
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            ImGui.AlignTextToFramePadding();
            Text.Small(l.RpProfileAccent2, Theme.TextMuted);
        }

        Layout.Spacer(Theme.GapS);

        if (Inputs.Select("##frame", l.RpProfileFrame, ref _frameIndex, [.. FrameLabels(l)]))
            MarkToggleDirty("styling_frame");

        DrawAutoSaveAt("styling_frame", l);

        // Une sanction peut interdire le titre : dans ce cas le champ disparaît
        // plutôt que d'attendre un refus du serveur sans explication.
        if (_profile?.TitlesBlocked == true)
        {
            Layout.Spacer(Theme.GapS);
            Text.Small(l.RpProfileTitleBlocked, Theme.Danger);
            if (_textDirty) DrawSaveRow(l);
            return;
        }

        Layout.Spacer(Theme.GapS);
        if (Inputs.Field("##rptitle", l.RpProfileRpTitle, ref _rpTitle, 40,
                         help: l.RpProfileRpTitleHint))
            MarkDirty();

        if (_rpTitle.Trim().Length > 0
            && Inputs.Select("##titleanim", l.RpProfileTitleAnim, ref _titleAnimIndex,
                             [.. TitleAnimLabels(l)]))
            MarkToggleDirty("styling_anim");

        DrawAutoSaveAt("styling_anim", l);

        if (_textDirty) DrawSaveRow(l);
    }

    private static IEnumerable<string> FrameLabels(Loc l) =>
        new[] { l.RpProfileStylingNone }.Concat(
            FrameKeys.Select(k => l.RpFrameLabels.TryGetValue(k, out var v) ? v : k));

    private static IEnumerable<string> TitleAnimLabels(Loc l) =>
        new[] { l.RpProfileStylingNone }.Concat(
            TitleAnimKeys.Select(k => l.RpTitleAnimLabels.TryGetValue(k, out var v) ? v : k));

    /// <summary>
    /// Convertit une couleur « #RRGGBB » en composantes normalisées. Une valeur
    /// absente ou illisible retombe sur la teinte donnée, jamais sur du noir :
    /// un sélecteur qui s'ouvre en noir laisserait croire à une couleur choisie.
    /// </summary>
    private static Vector3 HexToRgb(string? hex, Vector3 fallback)
    {
        if (hex is { Length: 7 } value && value[0] == '#'
            && int.TryParse(value[1..], System.Globalization.NumberStyles.HexNumber,
                            null, out var rgb))
            return new Vector3(((rgb >> 16) & 0xFF) / 255f,
                               ((rgb >> 8) & 0xFF) / 255f,
                               (rgb & 0xFF) / 255f);

        return new Vector3(fallback.X, fallback.Y, fallback.Z);
    }

    /// <summary>Format attendu par le serveur.</summary>
    private static string RgbToHex(Vector3 rgb) =>
        $"#{(int)(Math.Clamp(rgb.X, 0f, 1f) * 255f):x2}"
        + $"{(int)(Math.Clamp(rgb.Y, 0f, 1f) * 255f):x2}"
        + $"{(int)(Math.Clamp(rgb.Z, 0f, 1f) * 255f):x2}";

    /// <summary>
    /// Thèmes recherchés et thèmes évités, six au plus de chaque côté.
    ///
    /// Deux listes distinctes et non une échelle : « j'aime » et « je refuse »
    /// ne sont pas les deux bouts d'une même graduation, et un thème absent des
    /// deux veut simplement dire qu'on n'en a rien dit.
    /// </summary>
    private void DrawThemes(Loc l)
    {
        using var card = Card.Begin("rp_themes", interactive: false);
        Layout.SectionHeader(l.RpProfileThemesSection, Icons.Sparkle, tone: Tone);

        DrawThemeRow(l.RpProfileThemes, _themes, _avoidThemes, "want", l);
        Layout.Spacer(Theme.GapS);
        DrawThemeRow(l.RpProfileAvoidThemes, _avoidThemes, _themes, "avoid", l);

        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>
    /// Une rangée de thèmes à bascule. Un thème choisi d'un côté est retiré de
    /// l'autre : se dire à la fois preneur et rétif sur le même sujet ne veut
    /// rien dire, et le serveur ne trancherait pas à notre place.
    /// </summary>
    private void DrawThemeRow(string title, List<string> list, List<string> other, string id, Loc l)
    {
        Text.Small($"{title} ({list.Count}/{MaxThemes})", Theme.TextMuted);
        Layout.Spacer(Theme.GapXs);

        var limit = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        var gap   = Theme.S(Theme.GapXs);
        var first = true;

        foreach (var key in ThemeKeys)
        {
            var label  = RpProfileView.ThemeLabel(key, l);
            var active = list.Contains(key);
            var width  = Btn.Measure(label);

            if (!first && ImGui.GetCursorPosX() + gap + width <= limit)
                ImGui.SameLine(0f, gap);
            first = false;

            if (!Btn.Draw(label, active ? BtnTone.Primary : BtnTone.Ghost, BtnSize.Small,
                          id: $"theme_{id}_{key}"))
                continue;

            if (active) list.Remove(key);
            else if (list.Count < MaxThemes)
            {
                list.Add(key);
                other.Remove(key);
            }

            MarkDirty();
        }
    }

    /// <summary>
    /// Ce que le personnage est : son allure, son caractère, son passé.
    ///
    /// Séparé des limites, avec lesquelles ces blocs voisinaient. Le regroupement
    /// venait de leur forme, trois pavés de texte rédigés sur le site, non de
    /// leur sens : décrire un personnage et dire ce qu'on refuse de jouer sont
    /// deux sujets, et la confidentialité les distingue déjà (« description » et
    /// « limits » y sont deux sections d'audience). Rangés ensemble, l'apparence
    /// se retrouvait à l'écart des traits physiques, qui disent la même chose.
    /// </summary>
    private void DrawDescription(Loc l)
    {
        // Mêmes icônes et même teinte que sur la fiche vue par les autres : les
        // deux écrans affichent la même fiche et doivent rendre à l'identique.
        DrawLongText("rp_appearance",  l.RpProfileAppearance,  _appearance,
                     MaxAppearance, Icons.Diamond, Tone, l);
        DrawLongText("rp_personality", l.RpProfilePersonality, _personality,
                     MaxPersonality, Icons.RpLive, Tone, l);
        DrawLongText("rp_background",  l.RpProfileBackground,  _background,
                     MaxBackground, Icons.Clock, Tone, l);
    }

    // Longueurs maximales, reprises une à une du schéma du serveur. Un plafond
    // trop haut fait refuser tout l'enregistrement, pas seulement le champ ; un
    // plafond trop bas tronque en jeu un texte écrit sur le site.
    private const int MaxAppearance  = 4000;
    private const int MaxPersonality = 4000;
    private const int MaxBackground  = 8000;
    private const int MaxLimits      = 2000;

    /// <summary>
    /// Un texte long : son rendu, et un bouton qui ouvre l'éditeur.
    ///
    /// La carte reste affichée même vide, contrairement au rendu de la fiche
    /// d'autrui : c'est ici qu'on écrit, et une section qui disparaît faute de
    /// contenu n'offre nulle part où cliquer pour en ajouter.
    /// </summary>
    private void DrawLongText(string id, string title, string value, int maxLength,
                              FontAwesomeIcon icon, Vector4 tone, Loc l)
    {
        using var card = Card.Begin(id, interactive: false);
        Layout.SectionHeader(title, icon, tone: tone);

        if (string.IsNullOrWhiteSpace(value)) Text.Small(l.RpProfileEmptyText, Theme.TextMuted);
        else                                  MarkdownView.Draw(value, Theme.TextMuted);

        Layout.Spacer(Theme.GapXs);

        // La clé du personnage est capturée à l'ouverture : l'éditeur rend sa
        // réponse plusieurs frames plus tard, et le joueur a pu changer de
        // personnage ou de fiche entretemps. Sans elle, un texte validé
        // atterrirait dans la fiche d'un autre.
        var key = _loadedFor;
        if (Btn.Draw(l.RpProfileWrite, BtnTone.Ghost, BtnSize.Small, Icons.Edit, id: $"{id}_edit"))
            Plugin.OpenMarkdownEditor(title, value, maxLength, text => ApplyLongText(key, id, text));
    }

    /// <summary>
    /// Range le texte validé dans le bon champ, puis marque la fiche à
    /// enregistrer. Le passage par l'identifiant évite de faire voyager une
    /// référence de champ jusque dans une fenêtre à la durée de vie propre.
    ///
    /// Une validation qui arrive après un changement de personnage ou de fiche
    /// est jetée : elle décrit une fiche qui n'est plus à l'écran.
    /// </summary>
    private void ApplyLongText(string key, string id, string text)
    {
        if (_loadedFor != key) return;

        switch (id)
        {
            case "rp_appearance":  _appearance  = text; break;
            case "rp_personality": _personality = text; break;
            case "rp_background":  _background  = text; break;
            case "rp_limits":      _limits      = text; break;
            default: return;
        }

        MarkDirty();
    }

    /// <summary>
    /// Ce que le personnage ne joue pas. Va avec les préférences de jeu, pas
    /// avec sa description : c'est un cadre posé au partenaire, pas un trait.
    /// </summary>
    private void DrawLimits(Loc l)
    {
        DrawLongText("rp_limits", l.RpProfileLimits, _limits,
                     MaxLimits, Icons.Warning, Theme.Danger, l);
    }

    /// <summary>
    /// Confidentialité de la fiche, réglable en jeu.
    ///
    /// C'est en jeu qu'on réalise que sa biographie est lisible par n'importe
    /// qui : devoir sortir du jeu pour la masquer est précisément le moment où on
    /// ne le fait pas. Les mêmes réglages existent sur le site.
    /// </summary>
    private void DrawVisibility(Loc l)
    {
        if (_profile == null) return;

        using var card = Card.Begin("rp_visibility", interactive: false);
        Layout.SectionHeader(l.RpProfileVisibility, Icons.Hide, tone: Tone);

        Text.Muted(l.RpProfileVisWhere);
        Layout.Spacer(Theme.GapXs);

        if (Inputs.ToggleRow(l.RpProfileVisInGame, ref _visInGame, l.RpProfileVisInGameHint))
            MarkToggleDirty("vis_ingame");
        DrawAutoSaveAt("vis_ingame", l);

        if (Inputs.ToggleRow(l.RpProfileVisIndexable, ref _visIndexable, l.RpProfileVisIndexableHint))
            MarkToggleDirty("vis_index");
        DrawAutoSaveAt("vis_index", l);

        // Réservé à qui a effectivement un rôle. L'exposition est inoffensive :
        // le serveur relit le rôle en base au moment de sérialiser la fiche, si
        // bien qu'un binaire modifié qui afficherait la case malgré tout ne
        // gagnerait aucun badge.
        if (_profile?.StaffRole is { Length: > 0 })
        {
            if (Inputs.ToggleRow(l.RpProfileStaffBadge, ref _visStaffBadge,
                                 l.RpProfileStaffBadgeHint, Icons.Shield))
                MarkToggleDirty("vis_staff");
            DrawAutoSaveAt("vis_staff", l);
        }

        Layout.Divider(Theme.GapS);

        Text.Muted(l.RpProfileVisWho);
        Layout.Spacer(Theme.GapXs);

        // « Moi seul » ou « Moi seule » : le libellé parle du joueur à la première
        // personne, il s'accorde donc avec son personnage. Le plugin le sait, le
        // site non (le modèle Character ne stocke pas le genre) et emploie là-bas
        // la forme « Moi seul·e ».
        var ownerLabel = Plugin.CurrentCharacterIsFemale()
            ? l.RpProfileAudienceOwnerFem
            : l.RpProfileAudienceOwner;

        // Ordre imposé par AudienceKeys : de la plus large à la plus étroite.
        var options = new[] { l.RpProfileAudiencePublic, l.RpProfileAudienceFriend, ownerLabel };
        for (var i = 0; i < SectionKeys.Length; i++)
        {
            if (Inputs.Select($"##vis_{SectionKeys[i]}", SectionLabel(SectionKeys[i], l),
                              ref _sectionAudience[i], options))
                MarkToggleDirty($"vis_{SectionKeys[i]}");
            DrawAutoSaveAt($"vis_{SectionKeys[i]}", l);
        }

        Layout.Spacer(Theme.GapS);

        // Le cas d'usage principal de l'audience « ami » est d'ouvrir d'un geste
        // ce qu'on gardait pour soi. Sept listes déroulantes à dérouler une à une
        // suffiraient à décourager.
        var ownerIndex  = Array.IndexOf(AudienceKeys, "owner");
        var friendIndex = Array.IndexOf(AudienceKeys, "friend");
        if (_sectionAudience.Any(a => a == ownerIndex)
            && Btn.Draw(l.RpProfilePresetFriends, BtnTone.Secondary, BtnSize.Medium, Icons.Friend,
                        tooltip: l.RpProfilePresetFriendsHint, id: "rp_preset_friends"))
        {
            for (var i = 0; i < _sectionAudience.Length; i++)
                if (_sectionAudience[i] == ownerIndex) _sectionAudience[i] = friendIndex;
            MarkToggleDirty("vis_preset");
        }

        DrawAutoSaveAt("vis_preset", l);

        Layout.Spacer(Theme.GapS);
        Text.Small(string.Format(l.RpProfileVisOwnerNote, ownerLabel));
        Layout.Spacer(Theme.GapXs);
        Text.Small(l.RpProfileVisFriendNote);
        Layout.Spacer(Theme.GapXs);
        Text.Small(l.RpProfileVisAlwaysPublic);

        // Le bouton n'a plus à paraître pour un interrupteur, qui part de
        // lui-même : il apparaîtrait le temps du délai puis disparaîtrait, ce qui
        // se lit comme une occasion manquée d'enregistrer. Il reste pour ce qui
        // l'attend vraiment, une saisie en cours ailleurs dans la fiche.
        if (_textDirty) DrawSaveRow(l);
    }

    /// <summary>
    /// Aperçu et rafraîchissement, en tête de page.
    ///
    /// L'aperçu vivait en bas, après la ligne d'enregistrement, pour ne pas
    /// laisser croire qu'il montrerait les réglages en cours : il interroge le
    /// serveur et rend donc l'état enregistré. Ce n'est pas la position qui
    /// protège de ce malentendu mais le verrou `disabled: _dirty`, conservé ici,
    /// qui grise le bouton tant que des modifications ne sont pas enregistrées et
    /// dit pourquoi en infobulle. En haut, il se trouve là où on le cherche.
    ///
    /// Le rafraîchissement porte le même verrou, et pour la même raison en plus
    /// littérale : recharger écrase la copie de travail.
    /// </summary>
    private void DrawActionRow(Loc l)
    {
        if (_profile?.CharacterId is { Length: > 0 } characterId
            && Plugin.CurrentCharacter is { } character)
        {
            if (Btn.Draw(l.RpProfilePreview, BtnTone.Secondary, BtnSize.Medium, Icons.Show,
                         disabled: _dirty, tooltip: _dirty ? l.RpProfileVisSaveFirst : null))
                Plugin.OpenRpProfilePreview(characterId, character.Name, Plugin.CurrentWorldName());

            ImGui.SameLine(0f, Theme.S(Theme.GapS));
        }

        // Demande explicite : elle ignore l'anti-rebond de l'ouverture. Seule la
        // saisie en cours la bloque, l'utilisateur perdrait sinon son texte d'un
        // clic sur un bouton qui ne promet rien de tel.
        if (Btn.Draw(l.Refresh, BtnTone.Ghost, BtnSize.Medium, Icons.Refresh,
                     disabled: _dirty || _statusDirty || _loading || _saving,
                     tooltip: _dirty || _statusDirty
                         ? l.RpProfileRefreshSaveFirst
                         : l.RpProfileRefreshHint,
                     id: "rp_refresh"))
            Load(_loadedFor);

        Layout.Spacer(Theme.GapS);
    }

    /// <summary>Libellé d'une section, réutilisant les intitulés déjà traduits.</summary>
    private static string SectionLabel(string section, Loc l) => section switch
    {
        "identity"    => l.RpProfileIdentity,
        "hooks"       => l.RpProfileHooks,
        "glance"      => l.RpProfileGlance,
        "gallery"     => l.RpProfileGallery,
        "traits"      => l.RpProfileTraits,
        "belonging"   => l.RpProfileBelonging,
        "description" => l.RpProfileDescription,
        "relations"   => l.RpProfileRelations,
        "limits"      => l.RpProfileLimits,
        "links"       => l.RpProfileLinks,
        "sync"        => l.RpProfileSyncshells,
        _             => section,
    };

    // ─── Enregistrement ───────────────────────────────────────────────────────

    /// <summary>
    /// Modification qui attend le bouton : tout ce qui se tape, se choisit dans
    /// une liste de vocabulaire ou se construit champ par champ.
    ///
    /// Annule aussi l'enregistrement automatique en attente : on vient d'ouvrir
    /// une saisie, ce n'est plus le moment de figer la fiche.
    /// </summary>
    private void MarkDirty()
    {
        _dirty      = true;
        _textDirty  = true;
        _autoSaveAt = null;
    }

    /// <summary>
    /// Modification qui s'enregistre d'elle-même : les interrupteurs et les
    /// listes de la confidentialité, des préférences et de la divinité, qui
    /// n'ont pas d'état intermédiaire.
    ///
    /// Le reste du plugin enregistre déjà ses réglages au clic (voir
    /// SettingsPage) : ici, le bouton se trouvait sous trois paragraphes
    /// d'explication, si bien qu'on quittait la page en croyant avoir réglé sa
    /// visibilité alors que rien n'était parti. Le pire endroit du plugin pour
    /// un malentendu de ce genre.
    ///
    /// Si une saisie est en cours, aucun envoi n'est programmé : le bouton
    /// reprend la main plutôt que d'emporter le texte avec le réglage.
    /// </summary>
    /// <param name="control">
    /// Réglage touché, sous lequel s'affichera le retour. Voir
    /// <see cref="_autoSaveControl"/>.
    /// </param>
    private void MarkToggleDirty(string control)
    {
        _dirty = true;

        if (_textDirty)
        {
            _autoSaveAt = null;
            return;
        }

        _autoSaveAt      = DateTime.UtcNow + AutoSaveDelay;
        _autoSaveControl = control;
    }

    /// <summary>
    /// Déclenche l'enregistrement automatique dû, s'il l'est.
    ///
    /// Appelé à chaque rendu : il n'y a pas d'autre horloge dans cette page, et
    /// une page qu'on ne regarde pas n'a rien d'urgent à enregistrer. Le réglage
    /// reste à l'écran en attendant, il n'est pas perdu.
    ///
    /// `_profileFromNetwork` conditionne l'envoi comme il conditionne déjà celui
    /// des consentements : une fiche reconstituée du cache ne les connaît pas et
    /// les enverrait faux (voir BuildSaveRequest).
    /// </summary>
    private void TickAutoSave()
    {
        if (_autoSaveAt is not { } due) return;

        if (_textDirty || !_profileFromNetwork)
        {
            _autoSaveAt = null;
            return;
        }

        if (_loading || _saving || DateTime.UtcNow < due) return;

        _autoSaveAt      = null;
        _lastSaveWasAuto = true;
        Save();
    }


    /// <summary>
    /// Sort de l'enregistrement automatique, sous le réglage qui l'a déclenché.
    ///
    /// Un enregistrement qu'on ne voit pas ne vaut guère mieux qu'un bouton
    /// qu'on ne trouve pas : le doute change simplement de camp. Il se dit donc
    /// à l'endroit exact où le regard se trouve, c'est-à-dire sur l'interrupteur
    /// qu'on vient de basculer.
    ///
    /// Aligné à droite, sous l'interrupteur lui-même plutôt que sous son
    /// libellé : c'est de ce côté que le doigt a cliqué. En petite police, sur
    /// une ligne : le retour informe, il ne réclame pas l'attention.
    /// </summary>
    private void DrawAutoSaveAt(string control, Loc l)
    {
        if (_autoSaveControl != control) return;

        string  label;
        Vector4 color;

        if (_saveFailed)
        {
            label = l.SaveFailed;
            color = Theme.Danger;
        }
        // « En attente » et « en cours » se disent pareil : la nuance ne regarde
        // que le code, et deux libellés qui se succèdent en une seconde et demie
        // feraient clignoter la ligne pour rien.
        else if (_saving || _autoSaveAt != null)
        {
            label = l.RpProfileAutoSaving;
            color = Theme.TextMuted;
        }
        else if (_lastSaveWasAuto && DateTime.UtcNow < _savedUntil)
        {
            label = l.RpProfileAutoSaved;
            color = Theme.Online;
        }
        else return;

        using var font = Fonts.PushSmall();

        Layout.RightAlign(ImGui.CalcTextSize(label).X);
        ImGui.TextColored(color, label);
    }

    private void DrawSaveRow(Loc l)
    {
        var justSaved = DateTime.UtcNow < _savedUntil;
        if (!_dirty && !justSaved) return;

        Layout.Spacer(Theme.GapS);

        if (justSaved && !_dirty)
        {
            Text.WithIcon(Icons.Check, l.RpProfileSaved, Theme.Online, Theme.Online);
            return;
        }

        // L'échec reste affiché tant que la fiche n'a pas été réenregistrée :
        // les modifications sont encore là, elles ne sont simplement pas parties.
        if (_saveFailed)
        {
            Text.WithIcon(Icons.Warning, SaveFailureText(l), Theme.Danger, Theme.Danger);
            Layout.Spacer(Theme.GapXs);
        }

        if (Btn.Draw(_saving ? l.Processing : l.Save, BtnTone.Primary, BtnSize.Medium,
                     Icons.Check, disabled: _saving, id: "rpprofile_save"))
        {
            _lastSaveWasAuto = false;
            Save();
        }
    }

    private void Save()
    {
        if (_saving) return;
        _saving = true;

        // La fiche lue sert de base : les champs rédigés sur le site ne sont
        // pas édités ici et seraient effacés si on ne les renvoyait pas.
        var request = _profile != null
            ? SaveRpProfileRequest.From(_profile)
            : new SaveRpProfileRequest();

        var languages = new List<string>();
        if (_langFr) languages.Add("fr");
        if (_langEn) languages.Add("en");

        request.RpLevel      = LevelKeys[_levelIndex];
        request.ApproachMode = ApproachKeys[_approachIndex];
        request.Languages    = [.. languages];
        request.Hooks        = [.. _hooks.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim())];

        // Un emplacement sans titre n'existe pas : il n'est pas envoyé, et
        // l'ordre des autres est celui de l'écran, qui est celui que le lecteur
        // verra. Le tableau part toujours, y compris vide : c'est ainsi qu'on
        // efface un emplacement depuis le jeu.
        request.Glances = [.. Enumerable.Range(0, _glanceCount)
            .Where(i => !string.IsNullOrWhiteSpace(_glanceTitles[i]))
            .Select(i => new RpGlanceDto
            {
                Icon   = GlanceIconKeys[_glanceIcons[i]],
                Title  = _glanceTitles[i].Trim(),
                Body   = _glanceBodies[i].Trim(),
                Active = _glanceActive[i],
            })];

        // Les lignes sans identifiant ne sont pas envoyées. Le tableau est
        // toujours transmis, y compris vide : c'est ainsi qu'on efface un code
        // depuis le jeu.
        request.Syncshells = [.. Enumerable.Range(0, MaxSyncshells)
            .Where(i => !string.IsNullOrWhiteSpace(_syncIds[i]))
            .Select(i => new SyncshellEntryDto
            {
                Type = SyncTypeKeys[_syncTypes[i]],
                Id   = _syncIds[i].Trim(),
                Name = SyncTypeKeys[_syncTypes[i]] == "autre" && _syncNames[i].Trim() is { Length: > 0 } n
                    ? n
                    : null,
            })];
        request.CurrentQuest = Edited(_currentQuest);

        request.Height      = Edited(_height);
        request.Build       = Edited(_build);
        request.Marks       = Edited(_marks);
        request.Voice       = Edited(_voice);
        request.FreeCompany = Edited(_freeCompany);
        request.Allegiance  = Edited(_allegiance);
        request.Quote       = Edited(_quote);
        request.RpName      = Edited(_rpName);

        // Identité et thèmes, désormais rédigés en jeu comme sur le site.
        request.Race        = RaceKeys[_raceIndex];
        request.Age         = Edited(_age);
        request.Pronouns    = Edited(_pronouns);
        request.Origin      = Edited(_origin);
        request.Occupation  = Edited(_occupation);
        // La fiche visée, quand ce n'est pas celle que le personnage publie.
        request.ProfileId    = _selectedProfileId.Length > 0 ? _selectedProfileId : null;
        request.Nickname     = Edited(_nickname);
        request.NameplateName = NameplateKeys[_nameplateIndex];
        request.ThemeSongUrl = Edited(_themeSongUrl);
        request.ExternalUrl  = Edited(_externalUrl);
        request.Themes       = [.. _themes];
        request.AvoidThemes  = [.. _avoidThemes];

        // Habillage : envoyé seulement si le serveur a dit que le compte y a
        // droit. Sans ce garde-fou, un client qui n'a pas reçu l'information
        // renverrait les valeurs du cache et pourrait rétablir un habillage
        // qu'une adhésion expirée avait éteint.
        if (_profile?.CosmeticsAllowed == true)
        {
            request.AccentColor    = RgbToHex(_accent1);
            request.AccentColor2   = _accent2On ? RgbToHex(_accent2) : string.Empty;
            request.FrameStyle     = _frameIndex     > 0 ? FrameKeys[_frameIndex - 1]         : string.Empty;
            request.TitleAnimation = _titleAnimIndex > 0 ? TitleAnimKeys[_titleAnimIndex - 1] : string.Empty;
            if (_profile?.TitlesBlocked != true) request.RpTitle = Edited(_rpTitle);
        }
        request.Appearance  = Edited(_appearance);
        request.Personality = Edited(_personality);
        request.Background  = Edited(_background);
        request.Limits      = Edited(_limits);

        // Relations : envoyées seulement si elles ont été lues du serveur. Le
        // cache local ne les porte pas, et le serveur remplace la liste entière
        // à chaque envoi : partir du cache effacerait des relations nouées
        // depuis le site. Laissée nulle, la clé est omise et le serveur garde
        // les siennes, comme pour la confidentialité plus bas.
        //
        // Un emplacement sans nom n'existe pas : il n'est pas envoyé, et ne
        // consomme donc pas l'un des huit que le serveur accepte.
        if (_profileFromNetwork) request.Relations =
        [
            .. Enumerable.Range(0, _relationCount)
                .Where(i => _relationNames[i].Trim().Length > 0)
                .Select(i => new RpRelationDto
                {
                    TargetName = _relationNames[i].Trim(),
                    Kind       = RelationKindKeys[_relationKinds[i]],
                    Note       = Edited(_relationNotes[i]),
                }),
        ];

        // L'index 0 vaut « non précisé », que le serveur attend en null : une
        // chaîne vide serait refusée par l'énumération.
        // Chaîne vide et non null : « non précisé » est un choix, et un null
        // serait omis, donc l'ancienne divinité resterait en base.
        request.Deity = _deityIndex > 0 ? DeityKeys[_deityIndex] : string.Empty;

        // Statut d'équipe : envoyé seulement si le rôle et le consentement ont
        // été lus du serveur. From() l'a recopié de la fiche connue, mais une
        // fiche reconstituée du cache le donnerait à false, le cache ne le
        // stockant pas. Remis à null, il est omis du corps et le serveur garde
        // le sien.
        request.StaffBadgeVisible =
            _profileFromNetwork && _profile?.StaffRole is { Length: > 0 }
                ? _visStaffBadge
                : null;

        // Confidentialité : envoyée seulement si elle a été lue du serveur.
        // Laissés nuls, ces champs sont omis du corps et le serveur conserve les
        // siens, plutôt que de se voir imposer les défauts du cache local.
        if (_profileFromNetwork)
        {
            request.IsPublic        = _visInGame;
            request.SearchIndexable = _visIndexable;
            request.SectionVisibility = SectionKeys
                .Select((section, i) => (section, audience: AudienceKeys[_sectionAudience[i]]))
                .ToDictionary(entry => entry.section, entry => entry.audience);
        }

        var key = _loadedFor;
        _ = Task.Run(async () =>
        {
            var saved = await Plugin.Api.SaveRpProfileAsync(request);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _saving = false;

                if (saved == null)
                {
                    _saveFailed = true;
                    return;
                }

                _saveFailed = false;
                _profile = saved;
                config.RpProfiles[key] = FromDto(saved);
                config.Save();
                _dirty      = false;
                _textDirty  = false;
                _savedUntil = DateTime.UtcNow.AddSeconds(3);
            });
        });
    }

    // ─── Rendu utilitaire ─────────────────────────────────────────────────────

    /// <summary>Une saisie vide vaut absence de valeur, jamais chaîne vide.</summary>
    /// <summary>
    /// Valeur d'un champ édité dans cette page, prête à être envoyée.
    ///
    /// Un champ vidé part en chaîne vide, et non en null : le serveur conserve ce
    /// qu'il a pour tout champ absent du corps, et le null est omis à la
    /// sérialisation. Vider une valeur en jeu ne faisait donc rien, alors que
    /// l'écran affichait « Enregistré ».
    ///
    /// La distinction porte tout le contrat : absent veut dire « ce client ne
    /// touche pas à ce champ », vide veut dire « efface-le ». C'est ce qui permet
    /// d'effacer sans rouvrir la porte aux écrasements en masse.
    /// </summary>
    private static string Edited(string value) => value.Trim();

    /// <summary>
    /// Adresse d'édition de la fiche, personnage compris quand on le connaît.
    ///
    /// Sans lui, le site ouvre la fiche du premier personnage lié : un joueur
    /// qui en a plusieurs devait retrouver le bon à la main, alors même qu'il
    /// venait de cliquer depuis celle qu'il voulait modifier. L'identifiant est
    /// déjà public, il figure dans l'adresse de la fiche web.
    /// </summary>
    private string EditUrl()
    {
        var id = _profile?.CharacterId;
        return string.IsNullOrEmpty(id)
            ? "/dashboard/profil-rp"
            : $"/dashboard/profil-rp?personnage={Uri.EscapeDataString(id)}";
    }

    private static void OpenSite(string path) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl + path)
            { UseShellExecute = true });

    // ─── Conversion cache et transport ────────────────────────────────────────

    private static RpProfileCache FromDto(RpProfileDto p) => new()
    {
        RpLevel      = p.RpLevel,       ApproachMode  = p.ApproachMode,
        ContactMode  = p.ContactMode,   SessionLength = p.SessionLength,
        Languages    = Join(p.Languages),
        Themes       = Join(p.Themes),
        AvoidThemes  = Join(p.AvoidThemes),
        Hooks        = Join(p.Hooks),
        Glances      = System.Text.Json.JsonSerializer.Serialize(p.Glances),
        RpName       = p.RpName,        Nickname     = p.Nickname,
        NameplateName = p.NameplateName,
        Pronouns     = p.Pronouns,      Race         = p.Race,
        Age          = p.Age,           Origin       = p.Origin,
        Occupation   = p.Occupation,    Appearance   = p.Appearance,
        Personality  = p.Personality,   Background   = p.Background,
        CurrentQuest = p.CurrentQuest,  Limits       = p.Limits,
        Availability = p.Availability,  ExternalUrl  = p.ExternalUrl,
        Currently    = p.Currently,     IcState      = p.IcState,
        Syncshells   = p.Syncshells,
        Nsfw         = p.Nsfw,          IsPublic     = p.IsPublic,
        PortraitUrl  = p.PortraitUrl,
        Height       = p.Height,        Build        = p.Build,
        Marks        = p.Marks,         Voice        = p.Voice,
        FreeCompany  = p.FreeCompany,   Allegiance   = p.Allegiance,
        Deity        = p.Deity,         Quote        = p.Quote,
        ThemeSongUrl = p.ThemeSongUrl,  CharacterId  = p.CharacterId,

        // Confidentialité complète : sans elle, une fiche relue du cache
        // repartait sur les défauts du DTO et pouvait les imposer au serveur.
        SearchIndexable   = p.SearchIndexable,
        SectionVisibility = p.SectionVisibility,

        FetchedAt    = DateTime.UtcNow,
    };

    private static RpProfileDto ToDto(RpProfileCache c) => new()
    {
        RpLevel      = c.RpLevel,       ApproachMode  = c.ApproachMode,
        ContactMode  = c.ContactMode,   SessionLength = c.SessionLength,
        Languages    = Split(c.Languages),
        Themes       = Split(c.Themes),
        AvoidThemes  = Split(c.AvoidThemes),
        Hooks        = Split(c.Hooks),
        Glances      = SplitGlances(c.Glances),
        RpName       = c.RpName,        Nickname     = c.Nickname,
        NameplateName = c.NameplateName,
        Pronouns     = c.Pronouns,      Race         = c.Race,
        Age          = c.Age,           Origin       = c.Origin,
        Occupation   = c.Occupation,    Appearance   = c.Appearance,
        Personality  = c.Personality,   Background   = c.Background,
        CurrentQuest = c.CurrentQuest,  Limits       = c.Limits,
        Availability = c.Availability,  ExternalUrl  = c.ExternalUrl,

        // Un cache antérieur rend les deux nuls : la fiche n'affiche alors ni
        // état ni statut, ce qui vaut mieux qu'un « hors RP » inventé le temps
        // que le réseau réponde.
        Currently    = c.Currently,     IcState      = c.IcState,
        Syncshells   = c.Syncshells,
        Nsfw         = c.Nsfw,          IsPublic     = c.IsPublic,
        PortraitUrl  = c.PortraitUrl,
        Height       = c.Height,        Build        = c.Build,
        Marks        = c.Marks,         Voice        = c.Voice,
        FreeCompany  = c.FreeCompany,   Allegiance   = c.Allegiance,
        Deity        = c.Deity,         Quote        = c.Quote,
        ThemeSongUrl = c.ThemeSongUrl,  CharacterId  = c.CharacterId ?? string.Empty,

        // Un cache antérieur à ces champs les rend nuls : on retombe alors sur
        // les mêmes défauts qu'avant, mais l'écran reste bloqué en écriture tant
        // que le réseau n'a pas répondu (voir _profileFromNetwork).
        SearchIndexable   = c.SearchIndexable ?? false,
        SectionVisibility = c.SectionVisibility,

        // Les relations ne sont pas mises en cache : elles ne servent qu'à
        // l'affichage et arrivent avec le premier rafraîchissement réseau.
    };

    private static string Join(string[] values) =>
        System.Text.Json.JsonSerializer.Serialize(values);

    /// <summary>
    /// Coup d'œil relu du cache. Un cache antérieur à ce champ, ou corrompu,
    /// donne une liste vide : la page reste utilisable, et le premier
    /// rafraîchissement réseau rétablit les emplacements réels.
    /// </summary>
    private static RpGlanceDto[] SplitGlances(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<RpGlanceDto[]>(json) ?? []; }
        catch { return []; }
    }

    /// <summary>Une valeur absente ou corrompue donne une liste vide, jamais une exception.</summary>
    private static string[] Split(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }
}

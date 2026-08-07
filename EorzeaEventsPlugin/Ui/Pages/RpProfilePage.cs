using Dalamud.Bindings.ImGui;
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

    // Codes de sync : emplacements fixes, comme les accroches. Pas de boutons
    // ajouter/supprimer, une ligne sans identifiant n'est simplement pas envoyée.
    private readonly int[]    _syncTypes = new int[MaxSyncshells];
    private readonly string[] _syncNames = ["", "", "", "", ""];
    private readonly string[] _syncIds   = ["", "", "", "", ""];
    private string _currentQuest = string.Empty;
    private int    _levelIndex;
    private int    _approachIndex;
    private bool   _langFr = true;
    private bool   _langEn;
    private bool   _dirty;

    private string _height      = string.Empty;
    private string _build       = string.Empty;
    private string _marks       = string.Empty;
    private string _voice       = string.Empty;
    private string _freeCompany = string.Empty;
    private string _allegiance  = string.Empty;
    private string _quote       = string.Empty;
    private int    _deityIndex;

    // Visibilité : trois consentements, plus une audience par section.
    private bool _visInGame = true;
    private bool _visWebPage = true;
    private bool _visIndexable;

    /// <summary>Consentement d'affichage du statut d'équipe, pour qui en a un.</summary>
    private bool _visStaffBadge;
    private readonly int[] _sectionAudience = new int[SectionKeys.Length];

    private static readonly string[] LevelKeys    = ["beginner", "casual", "confirmed"];
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

    private static readonly string[] SectionKeys =
        ["identity", "hooks", "traits", "belonging", "description", "relations", "limits", "links", "sync"];

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
        ["public", "public", "owner", "owner", "owner", "owner", "owner", "public", "owner"];

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
        if (_loadedFor != key) Load(key);
        else if (_refreshPending) AutoRefresh(key);

        using var scroll = ImRaii.Child("##rpprofilescroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        DrawHeader(character, l);

        if (_loading && _profile == null)
        {
            Feedback.SkeletonCards(2);
            return;
        }

        DrawActionRow(l);
        DrawWebNotice(l);
        DrawAvailability(l);
        DrawHooks(l);
        DrawTraits(l);
        DrawBelonging(l);
        DrawSyncshells(l);
        DrawPreferences(l);
        DrawIdentity(l);
        DrawRelations(l);
        DrawStory(l);
        if (_profile is { } linked) RpProfileView.DrawLinks(linked, l, Tone);
        DrawVisibility(l);

        // Respiration en fin de page. Sans elle, la dernière carte est collée au
        // bord bas de la zone défilante, et la liste déroulante de sa dernière
        // ligne n'a pas la place de s'ouvrir vers le bas.
        Layout.Spacer(Theme.GapXl);
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

        if (_dirty || _loading || _saving) return;
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
        if (_loadedFor != key)
        {
            _loadedFor = key;
            _profile = config.RpProfiles.TryGetValue(key, out var cached) ? ToDto(cached) : null;
            _profileFromNetwork = false;
            Reset();
        }

        // L'aperçu interroge la route publique par un appel distinct : sans ce
        // rappel, la fenêtre ouverte à côté continuerait d'afficher l'état d'avant.
        Plugin.RefreshRpProfilePreview();

        _loading = true;
        _ = Task.Run(async () =>
        {
            var fetched = await Plugin.Api.GetRpProfileAsync();
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _loading = false;
                if (fetched == null) return;

                _profile = fetched;
                _profileFromNetwork = true;
                _lastFetchedAt = DateTime.UtcNow;
                config.RpProfiles[key] = FromDto(fetched);
                config.Save();

                // L'utilisateur a pu commencer à saisir pendant la requête : ses
                // champs priment alors sur la réponse, que la fiche lue sert de
                // base à l'enregistrement suivant.
                if (!_dirty) Reset();
            });
        });
    }

    /// <summary>Recharge la copie de travail depuis la fiche connue.</summary>
    private void Reset()
    {
        var p = _profile;

        for (var i = 0; i < _hooks.Length; i++)
            _hooks[i] = p != null && i < p.Hooks.Length ? p.Hooks[i] : string.Empty;

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
        _levelIndex    = Math.Max(0, Array.IndexOf(LevelKeys,    p?.RpLevel      ?? "casual"));
        _approachIndex = Math.Max(0, Array.IndexOf(ApproachKeys, p?.ApproachMode ?? "come_to_me"));
        _langFr        = p?.Languages.Contains("fr") ?? true;
        _langEn        = p?.Languages.Contains("en") ?? false;

        _height      = p?.Height      ?? string.Empty;
        _build       = p?.Build       ?? string.Empty;
        _marks       = p?.Marks       ?? string.Empty;
        _voice       = p?.Voice       ?? string.Empty;
        _freeCompany = p?.FreeCompany ?? string.Empty;
        _allegiance  = p?.Allegiance  ?? string.Empty;
        _quote       = p?.Quote       ?? string.Empty;
        _deityIndex  = Math.Max(0, Array.IndexOf(DeityKeys, p?.Deity ?? string.Empty));

        _visInGame    = p?.IsPublic        ?? true;
        _visWebPage   = p?.WebPageEnabled  ?? true;
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

        _dirty = false;
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
                               OpenSite("/dashboard/profil-rp");
                       });
    }

    private void DrawAvailability(Loc l)
    {
        using var card = Card.Begin("rp_available", interactive: false,
                                    accent: Plugin.CurrentCharacterAvailable ? Theme.Online : null);

        var available = Plugin.CurrentCharacterAvailable;
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
                _dirty = true;
        }

        Layout.Spacer(Theme.GapS);
        if (Inputs.Field("##quest", l.RpProfileCurrentQuest, ref _currentQuest, 200))
            _dirty = true;

        DrawSaveRow(l);
    }

    private void DrawTraits(Loc l)
    {
        using var card = Card.Begin("rp_traits", interactive: false);

        Layout.SectionHeader(l.RpProfileTraits, Icons.Character, tone: Tone);
        Text.Small(l.RpProfileTraitsHint);
        Layout.Spacer(Theme.GapS);

        if (Inputs.Field("##height", l.RpProfileHeight, ref _height, 30)) _dirty = true;
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##build", l.RpProfileBuild, ref _build, 40)) _dirty = true;
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##voice", l.RpProfileVoice, ref _voice, 80)) _dirty = true;
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##marks", l.RpProfileMarks, ref _marks, 300)) _dirty = true;

        DrawSaveRow(l);
    }

    private void DrawBelonging(Loc l)
    {
        using var card = Card.Begin("rp_belonging", interactive: false);

        Layout.SectionHeader(l.RpProfileBelonging, Icons.World, tone: Tone);

        if (Inputs.Field("##fc", l.RpProfileFreeCompany, ref _freeCompany, 80)) _dirty = true;
        Layout.Spacer(Theme.GapXs);
        if (Inputs.Field("##allegiance", l.RpProfileAllegiance, ref _allegiance, 80)) _dirty = true;
        Layout.Spacer(Theme.GapXs);

        if (Inputs.Select("##deity", l.RpProfileDeity, ref _deityIndex,
                          [.. DeityKeys.Select(k => RpProfileView.DeityLabel(k, l))]))
            _dirty = true;

        Layout.Spacer(Theme.GapS);
        if (Inputs.Field("##quote", l.RpProfileQuote, ref _quote, 300,
                         help: l.RpProfileQuoteHint))
            _dirty = true;

        DrawSaveRow(l);
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
                _dirty = true;

            if (SyncTypeKeys[_syncTypes[i]] == "autre"
                && Inputs.Field($"##syncname{i}", string.Empty, ref _syncNames[i], 40,
                                placeholder: l.RpProfileSyncshellName))
                _dirty = true;

            if (Inputs.Field($"##syncid{i}", string.Empty, ref _syncIds[i], 100,
                             placeholder: l.RpProfileSyncshellId))
                _dirty = true;
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
    private void DrawRelations(Loc l)
    {
        if (_profile is not { Relations.Length: > 0 } p) return;

        using var card = Card.Begin("rp_relations", interactive: false);
        Layout.SectionHeader(l.RpProfileRelations, Icons.Around, p.Relations.Length, tone: Tone);

        foreach (var relation in p.Relations)
        {
            Chip.Draw(RpProfileView.RelationLabel(relation.Kind, l), ChipTone.Accent);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            ImGui.AlignTextToFramePadding();
            Text.Body(relation.TargetName);

            if (relation.Note is { Length: > 0 } note)
                Text.Small(note);

            Layout.Spacer(Theme.GapXs);
        }
    }

    private void DrawPreferences(Loc l)
    {
        using var card = Card.Begin("rp_prefs", interactive: false);

        Layout.SectionHeader(l.RpProfilePreferences, Icons.Settings, tone: Tone);

        if (Inputs.Select("##rplevel", l.RpProfileLevel, ref _levelIndex,
                          [l.RpProfileLevelBeginner, l.RpProfileLevelCasual, l.RpProfileLevelConfirmed]))
            _dirty = true;

        Layout.Spacer(Theme.GapS);

        if (Inputs.Select("##rpapproach", l.RpProfileApproach, ref _approachIndex,
                          [l.RpProfileApproachCome, l.RpProfileApproachIGo, l.RpProfileApproachEither]))
            _dirty = true;

        Layout.Spacer(Theme.GapS);
        Text.Muted(l.RpProfileLanguages);
        Layout.Spacer(Theme.GapXs);

        if (Inputs.Toggle("##langfr", ref _langFr)) _dirty = true;
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ImGui.AlignTextToFramePadding();
        Text.Body("Français");
        ImGui.SameLine(0f, Theme.S(Theme.GapL));
        if (Inputs.Toggle("##langen", ref _langEn)) _dirty = true;
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ImGui.AlignTextToFramePadding();
        Text.Body("English");

        // Au moins une langue doit rester active, sinon la fiche n'apparaît
        // dans aucun filtre.
        if (!_langFr && !_langEn) _langFr = true;

        if (_profile is { Themes.Length: > 0 })
        {
            Layout.Spacer(Theme.GapS);
            Text.Muted(l.RpProfileThemes);
            Layout.Spacer(Theme.GapXs);
            RpProfileView.DrawThemeChips(_profile.Themes, ChipTone.Accent, Tone);
        }

        if (_profile is { AvoidThemes.Length: > 0 })
        {
            Layout.Spacer(Theme.GapS);
            Text.Muted(l.RpProfileAvoidThemes);
            Layout.Spacer(Theme.GapXs);
            RpProfileView.DrawThemeChips(_profile.AvoidThemes, ChipTone.Danger);
        }

        DrawSaveRow(l);
    }

    private void DrawIdentity(Loc l)
    {
        var p = _profile;
        if (p == null) return;

        var hasIdentity = p.Race is { Length: > 0 } || p.Age is { Length: > 0 }
                       || p.Origin is { Length: > 0 } || p.Occupation is { Length: > 0 }
                       || p.Pronouns is { Length: > 0 };
        if (!hasIdentity) return;

        using var card = Card.Begin("rp_identity", interactive: false);
        Layout.SectionHeader(l.RpProfileIdentity, Icons.Profile, tone: Tone);
        RpProfileView.BeginRows();

        if (p.Race is { Length: > 0 } race)            RpProfileView.Row(l.RpProfileRace, RpProfileView.RaceLabel(race, l));
        if (p.Age is { Length: > 0 } age)              RpProfileView.Row(l.RpProfileAge, age);
        if (p.Pronouns is { Length: > 0 } pronouns)    RpProfileView.Row(l.RpProfilePronouns, pronouns);
        if (p.Origin is { Length: > 0 } origin)        RpProfileView.Row(l.RpProfileOrigin, origin);
        if (p.Occupation is { Length: > 0 } occupation) RpProfileView.Row(l.RpProfileOccupation, occupation);
    }

    private void DrawStory(Loc l)
    {
        var p = _profile;
        if (p == null) return;

        // Mêmes icônes et même teinte que sur la fiche vue par les autres : les
        // deux écrans affichent la même fiche et doivent rendre à l'identique.
        RpProfileView.DrawTextBlock("rp_appearance",  l.RpProfileAppearance,  p.Appearance,
                                    Icons.Diamond, Tone);
        RpProfileView.DrawTextBlock("rp_personality", l.RpProfilePersonality, p.Personality,
                                    Icons.RpLive, Tone);
        RpProfileView.DrawTextBlock("rp_background",  l.RpProfileBackground,  p.Background,
                                    Icons.Clock, Tone);
        RpProfileView.DrawTextBlock("rp_limits",      l.RpProfileLimits,      p.Limits,
                                    Icons.Warning, Theme.Danger);
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
            _dirty = true;
        if (Inputs.ToggleRow(l.RpProfileVisWebPage, ref _visWebPage, l.RpProfileVisWebPageHint))
            _dirty = true;
        if (Inputs.ToggleRow(l.RpProfileVisIndexable, ref _visIndexable, l.RpProfileVisIndexableHint))
            _dirty = true;

        // Réservé à qui a effectivement un rôle. L'exposition est inoffensive :
        // le serveur relit le rôle en base au moment de sérialiser la fiche, si
        // bien qu'un binaire modifié qui afficherait la case malgré tout ne
        // gagnerait aucun badge.
        if (_profile?.StaffRole is { Length: > 0 })
        {
            if (Inputs.ToggleRow(l.RpProfileStaffBadge, ref _visStaffBadge,
                                 l.RpProfileStaffBadgeHint, Icons.Shield))
                _dirty = true;
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
                _dirty = true;
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
            _dirty = true;
        }

        Layout.Spacer(Theme.GapS);
        Text.Small(string.Format(l.RpProfileVisOwnerNote, ownerLabel));
        Layout.Spacer(Theme.GapXs);
        Text.Small(l.RpProfileVisFriendNote);
        Layout.Spacer(Theme.GapXs);
        Text.Small(l.RpProfileVisAlwaysPublic);

        DrawSaveRow(l);
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
                     disabled: _dirty || _loading || _saving,
                     tooltip: _dirty ? l.RpProfileRefreshSaveFirst : l.RpProfileRefreshHint,
                     id: "rp_refresh"))
            Load(_loadedFor);

        Layout.Spacer(Theme.GapS);
    }

    /// <summary>Libellé d'une section, réutilisant les intitulés déjà traduits.</summary>
    private static string SectionLabel(string section, Loc l) => section switch
    {
        "identity"    => l.RpProfileIdentity,
        "hooks"       => l.RpProfileHooks,
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
            Text.WithIcon(Icons.Warning, l.SaveFailed, Theme.Danger, Theme.Danger);
            Layout.Spacer(Theme.GapXs);
        }

        if (Btn.Draw(_saving ? l.Processing : l.Save, BtnTone.Primary, BtnSize.Medium,
                     Icons.Check, disabled: _saving, id: "rpprofile_save"))
            Save();
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
            request.WebPageEnabled  = _visWebPage;
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
        RpName       = p.RpName,        Nickname     = p.Nickname,
        Pronouns     = p.Pronouns,      Race         = p.Race,
        Age          = p.Age,           Origin       = p.Origin,
        Occupation   = p.Occupation,    Appearance   = p.Appearance,
        Personality  = p.Personality,   Background   = p.Background,
        CurrentQuest = p.CurrentQuest,  Limits       = p.Limits,
        Availability = p.Availability,  ExternalUrl  = p.ExternalUrl,
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
        WebPageEnabled    = p.WebPageEnabled,
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
        RpName       = c.RpName,        Nickname     = c.Nickname,
        Pronouns     = c.Pronouns,      Race         = c.Race,
        Age          = c.Age,           Origin       = c.Origin,
        Occupation   = c.Occupation,    Appearance   = c.Appearance,
        Personality  = c.Personality,   Background   = c.Background,
        CurrentQuest = c.CurrentQuest,  Limits       = c.Limits,
        Availability = c.Availability,  ExternalUrl  = c.ExternalUrl,
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
        WebPageEnabled    = c.WebPageEnabled  ?? true,
        SearchIndexable   = c.SearchIndexable ?? false,
        SectionVisibility = c.SectionVisibility,

        // Les relations ne sont pas mises en cache : elles ne servent qu'à
        // l'affichage et arrivent avec le premier rafraîchissement réseau.
    };

    private static string Join(string[] values) =>
        System.Text.Json.JsonSerializer.Serialize(values);

    /// <summary>Une valeur absente ou corrompue donne une liste vide, jamais une exception.</summary>
    private static string[] Split(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }
}

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

    // Copie de travail des champs éditables en jeu.
    private readonly string[] _hooks = ["", "", "", "", ""];
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
    private readonly int[] _sectionAudience = new int[SectionKeys.Length];

    private static readonly string[] LevelKeys    = ["beginner", "casual", "confirmed"];
    private static readonly string[] ApproachKeys = ["come_to_me", "i_approach", "either"];

    private static readonly string[] SectionKeys =
        ["identity", "hooks", "traits", "belonging", "description", "relations", "limits", "links"];

    /// <summary>
    /// Audiences, de la plus large à la plus étroite, dans le même ordre que
    /// <c>RP_AUDIENCES</c> côté serveur. L'ordre porte du sens : « ami » est un
    /// échelon intermédiaire, pas une troisième option quelconque.
    /// </summary>
    private static readonly string[] AudienceKeys = ["public", "friend", "owner"];

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
        ["public", "public", "owner", "owner", "owner", "owner", "owner", "public"];

    /// <summary>
    /// Les Douze, dans l'ordre du serveur, précédés d'une entrée vide : l'index
    /// 0 signifie « non précisé », pas « Halone ».
    /// </summary>
    private static readonly string[] DeityKeys =
    [
        "", "halone", "menphina", "thaliak", "nymeia", "llymlaen", "oschon",
        "byregot", "rhalgr", "azeyma", "naldthal", "nophica", "althyk", "other",
    ];

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

        using var scroll = ImRaii.Child("##rpprofilescroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        DrawHeader(character, l);

        if (_loading && _profile == null)
        {
            Feedback.SkeletonCards(2);
            return;
        }

        DrawWebNotice(l);
        DrawAvailability(l);
        DrawHooks(l);
        DrawTraits(l);
        DrawBelonging(l);
        DrawPreferences(l);
        DrawIdentity(l);
        DrawRelations(l);
        DrawStory(l);
        if (_profile is { } linked) RpProfileView.DrawLinks(linked, l);
        DrawVisibility(l);

        // Respiration en fin de page. Sans elle, la dernière carte est collée au
        // bord bas de la zone défilante, et la liste déroulante de sa dernière
        // ligne n'a pas la place de s'ouvrir vers le bas.
        Layout.Spacer(Theme.GapXl);
    }

    // ─── Chargement ───────────────────────────────────────────────────────────

    private void Load(string key)
    {
        _loadedFor = key;

        // Le cache évite un écran vide au changement de personnage ; le réseau
        // le rafraîchit ensuite.
        _profile = config.RpProfiles.TryGetValue(key, out var cached) ? ToDto(cached) : null;
        _profileFromNetwork = false;
        Reset();

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
                config.RpProfiles[key] = FromDto(fetched);
                config.Save();
                Reset();
            });
        });
    }

    /// <summary>Recharge la copie de travail depuis la fiche connue.</summary>
    private void Reset()
    {
        var p = _profile;

        for (var i = 0; i < _hooks.Length; i++)
            _hooks[i] = p != null && i < p.Hooks.Length ? p.Hooks[i] : string.Empty;

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
        using var card = Card.Begin("rp_header", interactive: false);

        RpProfileView.DrawPortrait(_profile?.PortraitUrl, character.Name,
            status: Plugin.CurrentCharacterAvailable ? Theme.Online : null);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        // Le portrait mange une bonne part de la largeur, et ces textes ne se
        // replient pas d'eux-mêmes : sans borne, un nom RP ou une citation un peu
        // longue sort de la carte.
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X
                              - Card.RightInset);

        Text.Title(_profile?.RpName is { Length: > 0 } rpName ? rpName : character.Name);
        if (_profile?.Nickname is { Length: > 0 } nickname) Text.Small($"« {nickname} »");
        if (_profile?.Quote is { Length: > 0 } quote)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small($"« {quote} »", Theme.Accent);
        }

        ImGui.PopTextWrapPos();
        ImGui.EndGroup();

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

        Layout.SectionHeader(l.RpProfileHooks, Icons.Sparkle);
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

        Layout.SectionHeader(l.RpProfileTraits, Icons.Character);
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

        Layout.SectionHeader(l.RpProfileBelonging, Icons.World);

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
    /// Relations, en consultation seule : les nouer se fait sur le site, où l'on
    /// dispose du clavier et de la recherche de personnages.
    /// </summary>
    private void DrawRelations(Loc l)
    {
        if (_profile is not { Relations.Length: > 0 } p) return;

        using var card = Card.Begin("rp_relations", interactive: false);
        Layout.SectionHeader(l.RpProfileRelations, Icons.Around, p.Relations.Length);

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

        Layout.SectionHeader(l.RpProfilePreferences, Icons.Settings);

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
            RpProfileView.DrawThemeChips(_profile.Themes, ChipTone.Accent);
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
        Layout.SectionHeader(l.RpProfileIdentity, Icons.Profile);

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

        RpProfileView.DrawTextBlock("rp_appearance",  l.RpProfileAppearance,  p.Appearance);
        RpProfileView.DrawTextBlock("rp_personality", l.RpProfilePersonality, p.Personality);
        RpProfileView.DrawTextBlock("rp_background",  l.RpProfileBackground,  p.Background);
        RpProfileView.DrawTextBlock("rp_limits",      l.RpProfileLimits,      p.Limits, Theme.Danger);
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
        Layout.SectionHeader(l.RpProfileVisibility, Icons.Hide);

        Text.Muted(l.RpProfileVisWhere);
        Layout.Spacer(Theme.GapXs);

        if (Inputs.ToggleRow(l.RpProfileVisInGame, ref _visInGame, l.RpProfileVisInGameHint))
            _dirty = true;
        if (Inputs.ToggleRow(l.RpProfileVisWebPage, ref _visWebPage, l.RpProfileVisWebPageHint))
            _dirty = true;
        if (Inputs.ToggleRow(l.RpProfileVisIndexable, ref _visIndexable, l.RpProfileVisIndexableHint))
            _dirty = true;

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

        // L'aperçu vient après la ligne d'enregistrement : il interroge le
        // serveur, donc il montre l'état enregistré et non les réglages en cours
        // de modification. Le placer avant laisserait croire l'inverse.
        if (_profile?.CharacterId is { Length: > 0 } characterId
            && Plugin.CurrentCharacter is { } character)
        {
            Layout.Spacer(Theme.GapS);
            if (Btn.Draw(l.RpProfilePreview, BtnTone.Secondary, BtnSize.Medium, Icons.Show,
                         disabled: _dirty, tooltip: _dirty ? l.RpProfileVisSaveFirst : null))
                Plugin.OpenRpProfilePreview(characterId, character.Name, Plugin.CurrentWorldName());
        }
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
        request.CurrentQuest = Trimmed(_currentQuest);

        request.Height      = Trimmed(_height);
        request.Build       = Trimmed(_build);
        request.Marks       = Trimmed(_marks);
        request.Voice       = Trimmed(_voice);
        request.FreeCompany = Trimmed(_freeCompany);
        request.Allegiance  = Trimmed(_allegiance);
        request.Quote       = Trimmed(_quote);

        // L'index 0 vaut « non précisé », que le serveur attend en null : une
        // chaîne vide serait refusée par l'énumération.
        request.Deity = _deityIndex > 0 ? DeityKeys[_deityIndex] : null;

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
                if (saved == null) return;

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
    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

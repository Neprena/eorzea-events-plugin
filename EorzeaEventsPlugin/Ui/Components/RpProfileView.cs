using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Api;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Rendu en lecture seule d'une fiche RP.
///
/// Le même rendu sert à deux écrans : la fiche de son propre personnage
/// (<c>Ui.Pages.RpProfilePage</c>, qui délègue ici ses sections non éditables) et
/// la fiche d'un autre joueur (<c>Windows.RpProfileWindow</c> en mode
/// consultation). Avant cette mise en commun, la seconde se contentait de quatre
/// lignes en ImGui brut alors que les données étaient déjà disponibles.
///
/// Les libellés de vocabulaire restent ici plutôt que dans <see cref="Loc"/>
/// quand ils sont identiques dans les deux langues du plugin : thèmes, races et
/// divinités portent des noms propres.
/// </summary>
internal static class RpProfileView
{
    /// <summary>
    /// Fiche complète, en-tête compris. Sert à consulter le personnage d'un
    /// autre joueur.
    /// </summary>
    public static void Draw(RpProfileDto? profile, string characterName, string? server, Loc l)
    {
        DrawHeader(profile, characterName, server, l);

        if (profile == null)
        {
            Feedback.EmptyState(Icons.Profile, l.RpProfileNoProfile);
            return;
        }

        DrawHooks(profile, l);
        DrawPreferences(profile, l);
        DrawIdentity(profile, l);
        DrawTraits(profile, l);
        DrawBelonging(profile, l);
        DrawRelations(profile, l);
        DrawStory(profile, l);
        DrawLinks(profile, l);
    }

    // ─── En-tête ──────────────────────────────────────────────────────────────

    private static void DrawHeader(RpProfileDto? profile, string characterName, string? server, Loc l)
    {
        using var card = Card.Begin("rpview_header", interactive: false);

        DrawPortrait(profile?.PortraitUrl, characterName);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        // Le portrait mange une bonne part de la largeur : sans repli, un nom RP
        // ou une citation un peu longue sort de la carte, ces textes ne bouclant
        // pas d'eux-mêmes.
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X
                              - Card.RightInset);

        Text.Title(profile?.RpName is { Length: > 0 } rpName ? rpName : characterName);

        if (!string.IsNullOrWhiteSpace(server))
            Text.Small($"{characterName} · {server}");

        if (profile?.Nickname is { Length: > 0 } nickname) Text.Small($"« {nickname} »");

        if (profile?.Quote is { Length: > 0 } quote)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small($"« {quote} »", Theme.Accent);
        }

        if (profile?.Availability is { Length: > 0 } availability)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small(availability);
        }

        if (profile?.Nsfw == true)
        {
            Layout.Spacer(Theme.GapXs);
            Chip.Draw(l.RpProfileNsfw, ChipTone.Danger, Icons.Warning);
        }

        ImGui.PopTextWrapPos();
        ImGui.EndGroup();
    }

    /// <summary>
    /// Portrait téléversé depuis le site, cadré en 3:4 et recadré en mode
    /// « couvrir » : la source fait 480×640, tout autre ratio serait déformé.
    ///
    /// Tant que l'image n'est pas arrivée, ou s'il n'y en a pas, un cadre aux
    /// initiales tient la place. Il réserve exactement les mêmes dimensions, sans
    /// quoi la carte grandit quand la texture arrive : le cercle de
    /// <see cref="Layout.Avatar"/> ne réservait qu'un carré, soit un quart de
    /// hauteur en moins.
    ///
    /// Un clic ouvre le portrait en grand, seul moyen de le voir à sa résolution
    /// réelle sans passer par le site.
    /// </summary>
    public static void DrawPortrait(string? portraitUrl, string characterName,
                                    float height = 200f, Vector4? status = null,
                                    string? id = null, bool zoomable = true)
    {
        var width  = height * 3f / 4f;
        var size   = new Vector2(Theme.S(width), Theme.S(height));
        var origin = ImGui.GetCursorScreenPos();
        var dl     = ImGui.GetWindowDrawList();
        var radius = Theme.S(Theme.RadiusCard);

        var texture = Textures.Get(portraitUrl);

        if (texture != null)
        {
            var (uv0, uv1) = Surface.CoverUv(texture.Width, texture.Height, size.X, size.Y);
            dl.AddImageRounded(texture.Handle, origin, origin + size, uv0, uv1,
                               ImGui.GetColorU32(Vector4.One), radius);
        }
        else
        {
            DrawInitialsFrame(dl, origin, size, characterName, radius);
        }

        // Un bouton invisible plutôt qu'un Dummy : mêmes dimensions réservées,
        // mais le survol et le clic deviennent détectables. Le retour vaut au
        // relâchement, ce qui évite d'ouvrir l'agrandissement en déplaçant la
        // fenêtre depuis le portrait.
        var key     = id is { Length: > 0 } ? id : characterName;
        var clicked = ImGui.InvisibleButton($"##portrait_{key}", size);

        var interactive = zoomable && texture != null;
        if (interactive && ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            dl.AddRect(origin, origin + size, ImGui.GetColorU32(Theme.BorderLight),
                       radius, ImDrawFlags.None, Theme.S(1.5f));
            Feedback.Tooltip(Plugin.L.RpProfileZoom);
        }

        if (status is { } dot) DrawStatusDot(dl, origin, size, dot);

        if (interactive && clicked)
            Plugin.OpenPortraitZoom(portraitUrl!, characterName);
    }

    /// <summary>Cadre 3:4 aux initiales, en attente du portrait ou à défaut.</summary>
    private static void DrawInitialsFrame(ImDrawListPtr dl, Vector2 origin, Vector2 size,
                                          string characterName, float radius)
    {
        var background = Theme.FromName(characterName);
        dl.AddRectFilled(origin, origin + size, ImGui.GetColorU32(background), radius);

        var initials = Layout.Initials(characterName);
        // Sur un grand cadre, deux lettres en petite police se perdent au milieu.
        using var font = size.X >= Theme.S(96f) ? Fonts.PushTitle() : Fonts.PushSmall();
        var textSize = ImGui.CalcTextSize(initials);
        dl.AddText(origin + (size - textSize) * 0.5f,
                   ImGui.GetColorU32(Theme.TextOn(background)), initials);
    }

    /// <summary>
    /// Pastille de présence dans l'angle du portrait. Dessinée dans les deux cas :
    /// tant qu'elle ne l'était que sur le cadre d'initiales, le point « en ligne »
    /// disparaissait dès que l'image finissait de charger.
    /// </summary>
    private static void DrawStatusDot(ImDrawListPtr dl, Vector2 origin, Vector2 size,
                                      Vector4 color)
    {
        var radius = MathF.Max(Theme.S(4f), size.X * 0.09f);
        var inset  = radius + Theme.S(4f);
        var center = new Vector2(origin.X + size.X - inset, origin.Y + size.Y - inset);

        dl.AddCircleFilled(center, radius + Theme.S(1.5f), ImGui.GetColorU32(Theme.BgSurface));
        dl.AddCircleFilled(center, radius, ImGui.GetColorU32(color));
    }

    // ─── Sections ─────────────────────────────────────────────────────────────

    public static void DrawHooks(RpProfileDto p, Loc l)
    {
        var hooks = p.Hooks.Where(h => !string.IsNullOrWhiteSpace(h)).ToArray();
        if (hooks.Length == 0 && string.IsNullOrWhiteSpace(p.CurrentQuest)) return;

        using var card = Card.Begin("rpview_hooks", interactive: false);
        Layout.SectionHeader(l.RpProfileHooks, Icons.Sparkle);

        if (p.CurrentQuest is { Length: > 0 } quest)
        {
            Row(l.RpProfileCurrentQuest, quest);
            if (hooks.Length > 0) Layout.Spacer(Theme.GapXs);
        }

        foreach (var hook in hooks)
            Text.WithIcon(Icons.Chevron, hook, Theme.Accent, wrap: true);
    }

    public static void DrawPreferences(RpProfileDto p, Loc l)
    {
        using var card = Card.Begin("rpview_prefs", interactive: false);
        Layout.SectionHeader(l.RpProfilePreferences, Icons.Settings);

        Row(l.RpProfileLevel,    LevelLabel(p.RpLevel, l));
        Row(l.RpProfileApproach, ApproachLabel(p.ApproachMode, l));

        // Prise de contact et durée des scènes étaient servies par le serveur et
        // renseignées sur le site, mais n'étaient affichées nulle part en jeu.
        if (p.ContactMode is { Length: > 0 } contact)
            Row(l.RpProfileContact, ContactLabel(contact, l));
        if (p.SessionLength is { Length: > 0 } length)
            Row(l.RpProfileLengths, SessionLengthLabel(length, l));

        if (p.Languages.Length > 0)
            Row(l.RpProfileLanguages, string.Join(" / ", p.Languages.Select(LanguageLabel)));

        if (p.Themes.Length > 0)
        {
            Layout.Spacer(Theme.GapS);
            Text.Muted(l.RpProfileThemes);
            Layout.Spacer(Theme.GapXs);
            DrawThemeChips(p.Themes, ChipTone.Accent);
        }

        if (p.AvoidThemes.Length > 0)
        {
            Layout.Spacer(Theme.GapS);
            Text.Muted(l.RpProfileAvoidThemes);
            Layout.Spacer(Theme.GapXs);
            DrawThemeChips(p.AvoidThemes, ChipTone.Danger);
        }
    }

    public static void DrawIdentity(RpProfileDto p, Loc l)
    {
        var hasIdentity = p.Race is { Length: > 0 } || p.Age is { Length: > 0 }
                       || p.Origin is { Length: > 0 } || p.Occupation is { Length: > 0 }
                       || p.Pronouns is { Length: > 0 };
        if (!hasIdentity) return;

        using var card = Card.Begin("rpview_identity", interactive: false);
        Layout.SectionHeader(l.RpProfileIdentity, Icons.Profile);

        if (p.Race is { Length: > 0 } race)             Row(l.RpProfileRace, RaceLabel(race, l));
        if (p.Age is { Length: > 0 } age)               Row(l.RpProfileAge, age);
        if (p.Pronouns is { Length: > 0 } pronouns)     Row(l.RpProfilePronouns, pronouns);
        if (p.Origin is { Length: > 0 } origin)         Row(l.RpProfileOrigin, origin);
        if (p.Occupation is { Length: > 0 } occupation) Row(l.RpProfileOccupation, occupation);
    }

    public static void DrawTraits(RpProfileDto p, Loc l)
    {
        var hasTraits = p.Height is { Length: > 0 } || p.Build is { Length: > 0 }
                     || p.Voice is { Length: > 0 } || p.Marks is { Length: > 0 };
        if (!hasTraits) return;

        using var card = Card.Begin("rpview_traits", interactive: false);
        Layout.SectionHeader(l.RpProfileTraits, Icons.Character);

        if (p.Height is { Length: > 0 } height) Row(l.RpProfileHeight, height);
        if (p.Build is { Length: > 0 } build)   Row(l.RpProfileBuild, build);
        if (p.Voice is { Length: > 0 } voice)   Row(l.RpProfileVoice, voice);
        if (p.Marks is { Length: > 0 } marks)   Row(l.RpProfileMarks, marks);
    }

    public static void DrawBelonging(RpProfileDto p, Loc l)
    {
        var hasBelonging = p.FreeCompany is { Length: > 0 } || p.Allegiance is { Length: > 0 }
                        || p.Deity is { Length: > 0 };
        if (!hasBelonging) return;

        using var card = Card.Begin("rpview_belonging", interactive: false);
        Layout.SectionHeader(l.RpProfileBelonging, Icons.World);

        if (p.FreeCompany is { Length: > 0 } fc)        Row(l.RpProfileFreeCompany, fc);
        if (p.Allegiance is { Length: > 0 } allegiance) Row(l.RpProfileAllegiance, allegiance);
        if (p.Deity is { Length: > 0 } deity)           Row(l.RpProfileDeity, DeityLabel(deity, l));
    }

    /// <summary>
    /// Relations. Toujours en consultation : les nouer se fait sur le site, où
    /// l'on dispose du clavier et de la recherche de personnages.
    /// </summary>
    public static void DrawRelations(RpProfileDto p, Loc l)
    {
        if (p.Relations.Length == 0) return;

        using var card = Card.Begin("rpview_relations", interactive: false);
        Layout.SectionHeader(l.RpProfileRelations, Icons.Around, p.Relations.Length);

        foreach (var relation in p.Relations)
        {
            Chip.Draw(RelationLabel(relation.Kind, l), ChipTone.Accent);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            ImGui.AlignTextToFramePadding();
            Text.Body(relation.TargetName);

            if (relation.Note is { Length: > 0 } note)
                Text.Small(note);

            Layout.Spacer(Theme.GapXs);
        }
    }

    public static void DrawStory(RpProfileDto p, Loc l)
    {
        DrawTextBlock("rpview_appearance",  l.RpProfileAppearance,  p.Appearance);
        DrawTextBlock("rpview_personality", l.RpProfilePersonality, p.Personality);
        DrawTextBlock("rpview_background",  l.RpProfileBackground,  p.Background);
        DrawTextBlock("rpview_limits",      l.RpProfileLimits,      p.Limits, Theme.Danger);
    }

    /// <summary>
    /// Thème musical et lien externe, jusqu'ici saisissables sur le site sans
    /// jamais apparaître en jeu.
    ///
    /// L'adresse est affichée en toutes lettres à côté du bouton : ce sont des
    /// liens écrits par un autre joueur, on doit voir où l'on va avant de sortir
    /// du jeu.
    /// </summary>
    public static void DrawLinks(RpProfileDto p, Loc l)
    {
        var hasLinks = p.ThemeSongUrl is { Length: > 0 } || p.ExternalUrl is { Length: > 0 };
        if (!hasLinks) return;

        using var card = Card.Begin("rpview_links", interactive: false);
        Layout.SectionHeader(l.RpProfileLinks, Icons.External);

        if (p.ThemeSongUrl is { Length: > 0 } song)
            DrawLink("rpview_song", l.RpProfileThemeSong, song, l);

        if (p.ExternalUrl is { Length: > 0 } external)
        {
            if (p.ThemeSongUrl is { Length: > 0 }) Layout.Spacer(Theme.GapS);
            DrawLink("rpview_ext", l.RpProfileExternalLink, external, l);
        }
    }

    private static void DrawLink(string id, string label, string url, Loc l)
    {
        Text.Muted(label);
        Layout.Spacer(Theme.GapXs);
        Text.Small(url);
        Layout.Spacer(Theme.GapXs);

        if (Btn.Draw(l.RpProfileOpenLink, BtnTone.Ghost, BtnSize.Small, Icons.External, id: id))
            OpenUrl(url);
    }

    /// <summary>
    /// Ouvre une adresse dans le navigateur. Seuls http et https sont suivis :
    /// l'adresse vient d'un autre joueur, et un schéma exotique ne doit pas
    /// pouvoir lancer autre chose qu'une page web.
    /// </summary>
    private static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;

        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    public static void DrawTextBlock(string id, string title, string? body, Vector4? tone = null)
    {
        if (string.IsNullOrWhiteSpace(body)) return;

        using var card = Card.Begin(id, interactive: false);
        Layout.SectionHeader(title, tone: tone);
        MarkdownView.Draw(body, Theme.TextMuted);
    }

    // ─── Rendu utilitaire ─────────────────────────────────────────────────────

    public static void Row(string label, string value)
    {
        Text.Small(label);
        ImGui.SameLine(Theme.S(140f));
        Text.Body(value);
    }

    public static void DrawThemeChips(string[] themes, ChipTone tone)
    {
        for (var i = 0; i < themes.Length; i++)
        {
            if (i > 0) ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(ThemeLabel(themes[i], Plugin.L), tone);
        }
    }

    // ─── Libellés de vocabulaire ──────────────────────────────────────────────

    public static string LevelLabel(string key, Loc l) => key switch
    {
        "beginner"  => l.RpProfileLevelBeginner,
        "casual"    => l.RpProfileLevelCasual,
        "confirmed" => l.RpProfileLevelConfirmed,
        _           => key,
    };

    public static string ApproachLabel(string key, Loc l) => key switch
    {
        "come_to_me" => l.RpProfileApproachCome,
        "i_approach" => l.RpProfileApproachIGo,
        "either"     => l.RpProfileApproachEither,
        _            => key,
    };

    public static string ContactLabel(string key, Loc l) => key switch
    {
        "direct"     => l.RpProfileContactDirect,
        "tell_first" => l.RpProfileContactTell,
        "either"     => l.RpProfileContactEither,
        _            => key,
    };

    public static string SessionLengthLabel(string key, Loc l) => key switch
    {
        "short"  => l.RpProfileLengthShort,
        "medium" => l.RpProfileLengthMedium,
        "long"   => l.RpProfileLengthLong,
        _        => key,
    };

    /// <summary>Les noms de langue s'écrivent dans leur propre langue.</summary>
    public static string LanguageLabel(string key) => key switch
    {
        "fr" => "Français",
        "en" => "English",
        _    => key,
    };

    /// <summary>
    /// Nom de race traduit. Les orthographes diffèrent d'une langue à l'autre
    /// (« Elézen » contre « Elezen »), et une valeur inconnue s'affiche telle
    /// quelle, pour qu'un ajout côté serveur se voie au lieu de se fondre.
    /// </summary>
    public static string RaceLabel(string key, Loc l) =>
        l.RpRaceLabels.TryGetValue(key, out var label) ? label : key;

    public static string DeityLabel(string key, Loc l) => key switch
    {
        ""         => l.RpProfileDeityNone,
        "halone"   => "Halone",
        "menphina" => "Menphina",
        "thaliak"  => "Thaliak",
        "nymeia"   => "Nymeia",
        "llymlaen" => "Llymlaen",
        "oschon"   => "Oschon",
        "byregot"  => "Byregot",
        "rhalgr"   => "Rhalgr",
        "azeyma"   => "Azeyma",
        "naldthal" => "Nald'thal",
        "nophica"  => "Nophica",
        "althyk"   => "Althyk",
        "other"    => l.RpProfileRaceOther,
        _          => key,
    };

    public static string RelationLabel(string key, Loc l) => key switch
    {
        "ally"    => l.RpProfileRelationAlly,
        "friend"  => l.RpProfileRelationFriend,
        "family"  => l.RpProfileRelationFamily,
        "lover"   => l.RpProfileRelationLover,
        "mentor"  => l.RpProfileRelationMentor,
        "student" => l.RpProfileRelationStudent,
        "rival"   => l.RpProfileRelationRival,
        "enemy"   => l.RpProfileRelationEnemy,
        "other"   => l.RpProfileRelationOther,
        // Une valeur inconnue s'affiche telle quelle plutôt que sous « Autre » :
        // un type de relation ajouté côté serveur doit se voir, pas se déguiser
        // en catégorie existante.
        _         => key,
    };

    /// <summary>
    /// Nom de thème traduit. Huit des douze diffèrent entre les deux langues,
    /// et une valeur inconnue s'affiche telle quelle.
    /// </summary>
    public static string ThemeLabel(string key, Loc l) =>
        l.RpThemeLabels.TryGetValue(key, out var label) ? label : key;
}

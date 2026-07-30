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
    }

    // ─── En-tête ──────────────────────────────────────────────────────────────

    private static void DrawHeader(RpProfileDto? profile, string characterName, string? server, Loc l)
    {
        using var card = Card.Begin("rpview_header", interactive: false);

        DrawPortrait(profile?.PortraitUrl, characterName);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();
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
        ImGui.EndGroup();
    }

    /// <summary>
    /// Portrait téléversé depuis le site, cadré en 3:4. Tant qu'il n'est pas
    /// arrivé, ou s'il n'y en a pas, les initiales tiennent la place : le bloc
    /// garde ainsi la même largeur et le texte à droite ne se déplace pas.
    /// </summary>
    public static void DrawPortrait(string? portraitUrl, string characterName,
                                    float height = 84f, Vector4? status = null)
    {
        var width = height * 3f / 4f;

        var texture = Textures.Get(portraitUrl);
        if (texture == null)
        {
            Layout.Avatar(characterName, width, status);
            return;
        }

        var size   = new Vector2(Theme.S(width), Theme.S(height));
        var origin = ImGui.GetCursorScreenPos();

        ImGui.GetWindowDrawList().AddImageRounded(
            texture.Handle, origin, origin + size,
            Vector2.Zero, Vector2.One,
            ImGui.GetColorU32(Vector4.One), Theme.S(Theme.RadiusCard));

        ImGui.Dummy(size);
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
            Chip.Draw(ThemeLabel(themes[i]), tone);
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

    /// <summary>Les noms de langue s'écrivent dans leur propre langue.</summary>
    public static string LanguageLabel(string key) => key switch
    {
        "fr" => "Français",
        "en" => "English",
        _    => key,
    };

    public static string RaceLabel(string key, Loc l) => key switch
    {
        "hyur"     => "Hyur",
        "elezen"   => "Elézen",
        "lalafell" => "Lalafell",
        "miqote"   => "Miqo'te",
        "roegadyn" => "Roegadyn",
        "aura"     => "Au Ra",
        "hrothgar" => "Hrothgar",
        "viera"    => "Viéra",
        "other"    => l.RpProfileRaceOther,
        _          => key,
    };

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
        _         => l.RpProfileRelationOther,
    };

    public static string ThemeLabel(string key) => key switch
    {
        "tavern"        => "Taverne",
        "adventure"     => "Aventure",
        "drama"         => "Drame",
        "romance"       => "Romance",
        "lore"          => "Lore-friendly",
        "dark"          => "Thèmes sombres",
        "mystery"       => "Mystère",
        "intrigue"      => "Intrigue",
        "combat"        => "Combat",
        "craft"         => "Artisanat",
        "slice_of_life" => "Tranche de vie",
        "politics"      => "Politique",
        _               => key,
    };
}

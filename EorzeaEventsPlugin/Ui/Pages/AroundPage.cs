using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Joueurs déclarés disponibles pour du RP, avec accès à leur fiche.
///
/// Jusqu'ici cette information n'existait que sous forme d'un marqueur sur les
/// nameplates : on savait que quelqu'un était disponible sans pouvoir consulter
/// sa fiche, ni savoir qui l'était ailleurs dans la zone.
///
/// Aucun appel réseau propre : <c>Plugin.AvailableEntries</c> est déjà rafraîchi
/// par la boucle de mise à jour du plugin.
/// </summary>
internal sealed class AroundPage
{
    private string _search = string.Empty;
    private bool   _myWorldOnly;

    // Filtres d'une soirée : en mémoire de page, jamais persistés.
    private string? _langFilter;
    private string? _levelFilter;
    private string? _approachFilter;

    public void Draw()
    {
        var l = Plugin.L;

        Layout.Spacer(Theme.GapXs);

        var onlineWidth = Btn.Measure(l.ViewOnline, Icons.External);
        var searchWidth = ImGui.GetContentRegionAvail().X - onlineWidth - Theme.S(Theme.GapM);
        Inputs.SearchBar("##aroundsearch", ref _search, l.AroundSearchHint, searchWidth);

        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        if (Btn.Draw(l.ViewOnline, BtnTone.Ghost, BtnSize.Medium, Icons.External, id: "around_online"))
            OpenSite("/rp-live");

        Layout.Spacer(Theme.GapS);

        var myWorldOnly = _myWorldOnly;
        if (Inputs.ToggleRow(l.AroundMyWorldOnly, ref myWorldOnly)) _myWorldOnly = myWorldOnly;

        DrawFilters(l);

        Layout.Spacer(Theme.GapS);

        var entries = Filtered();
        if (entries.Count == 0)
        {
            // Distinguer « personne n'est disponible » de « le filtre ne laisse
            // rien passer » évite de faire croire à une liste vide côté serveur.
            Feedback.EmptyState(Icons.Around,
                Plugin.AvailableEntries.Count == 0 ? l.AroundEmpty : l.AroundNoMatch);
            return;
        }

        // Lus une fois par frame : chacun interroge la table d'objets.
        var myWorld   = Plugin.CurrentWorldName();
        var territory = Plugin.ClientState.TerritoryType;

        // Trois sections, du plus proche au plus lointain : à portée de vue,
        // dans la région (même territoire), ailleurs. C'est la question qu'on se
        // pose en ouvrant la page : qui est là, qui est à deux pas, qui est loin.
        // Dans chaque section, les déclarés passent avant le seul tag allumé :
        // les premiers attendent qu'on les aborde, les seconds jouent leur
        // personnage sans avoir rien demandé.
        var visible   = new List<RpAvailabilityEntryDto>();
        var region    = new List<RpAvailabilityEntryDto>();
        var elsewhere = new List<RpAvailabilityEntryDto>();
        foreach (var e in entries)
        {
            if (Plugin.IsVisible(e))                          visible.Add(e);
            else if (Plugin.IsInMyZone(e, myWorld, territory)) region.Add(e);
            else                                               elsewhere.Add(e);
        }
        Sort(visible);
        Sort(region);
        Sort(elsewhere);

        using var scroll = ImRaii.Child("##aroundscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        // La mise en garde sur le tag une fois en tête, et non sur chaque carte
        // ni par section : répétée, elle ne se lirait plus. La pastille « Tag
        // JDR » de la carte fait le reste.
        if (entries.Any(e => !IsDeclared(e)))
        {
            Text.Small(l.AroundRpTaggedHint, Theme.TextMuted);
            Layout.Spacer(Theme.GapS);
        }

        var drawn = 0;
        DrawSection(visible,   l.AroundVisibleCount,   Icons.Show,     Theme.Online, l, inZone: true,  ref drawn);
        DrawSection(region,    l.AroundHereCount,      Icons.Location, Theme.Online, l, inZone: true,  ref drawn);
        DrawSection(elsewhere, l.AroundElsewhereCount, Icons.World,    null,         l, inZone: false, ref drawn);

        // Respiration en fin de liste, pour que la dernière carte ne soit pas
        // collée au bord bas de la zone défilante.
        Layout.Spacer(Theme.GapXl);
    }

    private static void Sort(List<RpAvailabilityEntryDto> list) =>
        list.Sort((a, b) =>
        {
            var byKind = (IsDeclared(a) ? 0 : 1).CompareTo(IsDeclared(b) ? 0 : 1);
            return byKind != 0
                ? byKind
                : StringComparer.CurrentCultureIgnoreCase.Compare(a.CharacterName, b.CharacterName);
        });

    /// <summary>Une section vide ne s'affiche pas ; la première dessinée n'a pas d'espace au-dessus.</summary>
    private static void DrawSection(List<RpAvailabilityEntryDto> list, string titleFormat,
                                    FontAwesomeIcon icon, Vector4? tone, Loc l, bool inZone, ref int drawn)
    {
        if (list.Count == 0) return;
        if (drawn > 0) Layout.Spacer(Theme.GapM);
        drawn++;

        Layout.SectionHeader(string.Format(titleFormat, list.Count), icon, tone: tone);
        foreach (var entry in list) DrawEntry(entry, l, inZone);
    }

    /// <summary>
    /// Trois rangées de pastilles à bascule, une valeur par rangée : langue,
    /// niveau, mode d'approche. Une pastille active filtre, une seconde
    /// pression la relâche.
    /// </summary>
    private void DrawFilters(Loc l)
    {
        Layout.Spacer(Theme.GapXs);

        DrawFilterRow("lang",
            [("fr", RpProfileView.LanguageLabel("fr")), ("en", RpProfileView.LanguageLabel("en"))],
            ref _langFilter);
        DrawFilterRow("level",
            [("beginner",  RpProfileView.LevelLabel("beginner",  l)),
             ("casual",    RpProfileView.LevelLabel("casual",    l)),
             ("confirmed", RpProfileView.LevelLabel("confirmed", l))],
            ref _levelFilter);
        DrawFilterRow("approach",
            [("come_to_me", RpProfileView.ApproachLabel("come_to_me", l)),
             ("i_approach", RpProfileView.ApproachLabel("i_approach", l)),
             ("either",     RpProfileView.ApproachLabel("either",     l))],
            ref _approachFilter);
    }

    private static void DrawFilterRow(string id, (string Key, string Label)[] options, ref string? selected)
    {
        for (var i = 0; i < options.Length; i++)
        {
            if (i > 0) ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            var (key, label) = options[i];
            var active = selected == key;
            if (Btn.Draw(label, active ? BtnTone.Primary : BtnTone.Ghost, BtnSize.Small,
                         id: $"around_f_{id}_{key}"))
                selected = active ? null : key;
        }
    }

    /// <summary>Une entrée sans fiche ne passe que si aucun filtre n'est posé.</summary>
    private bool PassesFilters(RpAvailabilityEntryDto e)
    {
        if (_langFilter == null && _levelFilter == null && _approachFilter == null) return true;
        if (e.Profile is not { } p) return false;
        if (_langFilter     != null && !p.Languages.Contains(_langFilter)) return false;
        if (_levelFilter    != null && p.RpLevel      != _levelFilter)     return false;
        if (_approachFilter != null && p.ApproachMode != _approachFilter)  return false;
        return true;
    }

    /// <summary>Liste filtrée par la recherche et, si demandé, par le monde courant.</summary>
    private List<RpAvailabilityEntryDto> Filtered()
    {
        var query = _search.Trim();
        var world = _myWorldOnly ? Plugin.CurrentWorldName() : null;

        return Plugin.AvailableEntries
            .Where(e => world == null
                     || string.Equals(e.Server, world, StringComparison.OrdinalIgnoreCase))
            .Where(e => query.Length == 0 || Matches(e, query))
            .Where(PassesFilters)
            .OrderBy(e => e.CharacterName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Entrée issue d'une déclaration explicite de disponibilité, par opposition
    /// au seul tag « Jeu de rôle » allumé.
    ///
    /// Un serveur antérieur à ce champ ne renvoie que des volontaires : le DTO
    /// vaut « declared » par défaut, ce qui range tout du bon côté sans
    /// condition supplémentaire.
    /// </summary>
    private static bool IsDeclared(RpAvailabilityEntryDto e) => e.Source is not "rp_tag";

    /// <summary>Recherche sur le nom du personnage comme sur son nom RP.</summary>
    private static bool Matches(RpAvailabilityEntryDto e, string query) =>
        e.CharacterName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (e.Profile?.RpName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

    private static void DrawEntry(RpAvailabilityEntryDto entry, Loc l, bool inZone)
    {
        // Le vert reste au joueur qui a levé la main. L'ambre dit « il joue,
        // mais il n'a rien demandé » sans avoir à lire quoi que ce soit.
        var declared = IsDeclared(entry);
        var status   = declared ? Theme.Online : Theme.Idle;

        using var card = Card.Begin($"around_{entry.Id}", interactive: false, accent: status);

        // Plus petit que sur la fiche : une carte de liste doit rester compacte,
        // mais 64 rendait le portrait illisible.
        RpProfileView.DrawPortrait(entry.Profile?.PortraitUrl, entry.CharacterName,
                                   height: 128f, status: status, id: entry.Id);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        Text.Body(entry.Profile?.RpName is { Length: > 0 } rpName ? rpName : entry.CharacterName);
        Text.Small($"{entry.CharacterName} · {entry.Server}");

        // Teintée quand le joueur est dans ma zone : c'est la seule différence
        // visible entre une carte de la section « dans ma zone » et les autres.
        if (entry.Zone is { Length: > 0 } zone)
            Text.WithIcon(Icons.Location, zone,
                          inZone ? Theme.Online : null, inZone ? Theme.Online : null, wrap: true);

        // Coordonnées au format du jeu : c'est ainsi qu'on se donne rendez-vous.
        if (Plugin.HasFreshPosition(entry))
        {
            var coords = $"X {entry.PosX!.Value:F1}  Y {entry.PosZ!.Value:F1}";
            if (entry.InstanceId is > 0 and var instance)
                coords += $" · {string.Format(l.AroundInstance, instance)}";
            Text.WithIcon(Icons.Map, coords);
        }

        // La citation accompagne le portrait dans la charge utile, envoyée pour
        // étoffer cette liste sans second appel. Elle n'était pas affichée.
        //
        // Elle porte la couleur d'accent du joueur, seul rappel de son habillage
        // dans cette liste : le liseré de la carte reste réservé au statut de
        // disponibilité, et la bannière n'est pas envoyée pour cette vue.
        if (entry.Profile?.Quote is { Length: > 0 } quote)
            Text.Small($"« {quote} »", RpProfileView.Accent(entry.Profile));

        // Ma note privée, tirée du registre des rencontres : c'est ici qu'on
        // décide d'aborder quelqu'un, autant se rappeler ce qu'on en sait.
        if (EncounterRegistry.NoteFor(entry.Profile?.CharacterId) is { Length: > 0 } note)
            Text.WithIcon(Icons.Edit, note, wrap: true);

        if (entry.Profile is { } profile)
        {
            Layout.Spacer(Theme.GapXs);

            // Statut d'équipe en tête des pastilles : savoir à qui s'adresser en
            // jeu est précisément ce qu'on cherche dans cette liste. Le liseré de
            // la carte n'en est pas teinté pour autant, il reste réservé à la
            // disponibilité.
            // Le badge nomme le site (« Équipe Eorzea Events »), il est donc long,
            // et cette colonne est étroite : le portrait en mange déjà 128. À la
            // largeur minimale de la fenêtre, la suite de pastilles dépassait du
            // bord droit de la carte, où elle se faisait rogner. Chacune ne reste
            // donc sur la ligne que si elle y tient. Chip.Row fait la même chose,
            // mais il impose un ton unique et pas d'icône : inutilisable ici.
            var limit = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
            var gap   = Theme.S(Theme.GapXs);

            void SameLineIfRoom(float width)
            {
                if (ImGui.GetCursorPosX() + gap + width <= limit) ImGui.SameLine(0f, gap);
            }

            // Extrait servi par la route publique des disponibilités : le
            // consentement y est déjà appliqué, `staffBadgeVisible` n'y figure pas.
            var hasBadge = RpProfileView.StaffBadge(profile, l, requireConsent: false);

            // Sur la carte aussi, pas seulement en tête de section : une liste se
            // parcourt, et la mise en garde doit tenir sur la ligne qu'on lit.
            if (!declared)
            {
                if (hasBadge) SameLineIfRoom(Chip.Measure(l.AroundRpTaggedChip, Icons.RpLive));
                Chip.Draw(l.AroundRpTaggedChip, ChipTone.Warning, Icons.RpLive);
                hasBadge = true;
            }

            var level = RpProfileView.LevelLabel(profile.RpLevel, l);
            if (hasBadge) SameLineIfRoom(Chip.Measure(level));
            Chip.Draw(level, ChipTone.Neutral);

            var approach = RpProfileView.ApproachLabel(profile.ApproachMode, l);
            SameLineIfRoom(Chip.Measure(approach));
            Chip.Draw(approach, ChipTone.Accent);

            if (profile.Nsfw)
            {
                SameLineIfRoom(Chip.Measure(l.RpProfileNsfw, Icons.Warning));
                Chip.Draw(l.RpProfileNsfw, ChipTone.Danger, Icons.Warning);
            }
        }

        ImGui.EndGroup();

        Layout.Spacer(Theme.GapS);

        if (Btn.Draw(l.RpProfileViewTitle, BtnTone.Primary, BtnSize.Medium, Icons.Profile,
                     id: $"around_view_{entry.Id}"))
            Plugin.OpenRpProfileViewer(entry);

        // Le lien n'a de sens que si la fiche a une page : la visibilité en jeu
        // et la page web sont deux consentements distincts.
        if (entry.Profile is { HasWebPage: true, CharacterId: { Length: > 0 } characterId })
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.RpProfileViewOnSite, BtnTone.Ghost, BtnSize.Medium, Icons.External,
                         id: $"around_site_{entry.Id}"))
                OpenSite($"/rp/{characterId}");
        }

        if (Plugin.HasFreshPosition(entry))
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.AroundFindOnMap, BtnTone.Ghost, BtnSize.Medium, Icons.Map,
                         id: $"around_map_{entry.Id}"))
                Plugin.OpenOnMap(entry);
        }

        // Ajouter quelqu'un ouvre SA PROPRE fiche à cette personne : rien de ce
        // qui est affiché ici ne changera. L'infobulle le dit, sans quoi le geste
        // se lit comme une demande d'amitié.
        if (entry.Profile?.CharacterId is { Length: > 0 } friendId)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));

            if (Plugin.IsFriend(friendId))
            {
                Chip.Draw(l.RpFriendChip, ChipTone.Accent, Icons.Friend);
            }
            else if (Btn.Draw(l.RpFriendAdd, BtnTone.Ghost, BtnSize.Medium, Icons.FriendAdd,
                              tooltip: l.RpFriendAddHint, id: $"around_friend_{entry.Id}"))
            {
                Plugin.AddFriend(friendId, 0, entry.CharacterName);
            }
        }
    }

    private static void OpenSite(string path) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl + path)
            { UseShellExecute = true });
}

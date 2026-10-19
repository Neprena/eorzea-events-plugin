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

    /// <summary>Carte des filtres dépliée. Repliée au départ : on filtre rarement.</summary>
    private bool _filtersOpen;

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
            Text.Hint(l.AroundRpTaggedHint);
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
    /// <summary>
    /// Filtres, regroupés dans une carte : le monde, puis une ligne titrée par
    /// critère, chacune ouverte par « Peu importe ».
    ///
    /// Trois rangées de boutons sans titre ne disaient ni ce qu'elles
    /// filtraient, ni comment revenir à « tout » : il fallait deviner qu'un
    /// second clic sur le choix actif l'annulait.
    ///
    /// Repliée par défaut, et de nouveau à la réinitialisation : la plupart du
    /// temps on ne filtre rien, et la carte dépliée repoussait la liste d'un
    /// tiers de fenêtre. Repliée, son titre compte les filtres posés.
    /// </summary>
    private void DrawFilters(Loc l)
    {
        using var card = Card.Begin("around_filters", interactive: false);

        var active = (_langFilter != null ? 1 : 0) + (_levelFilter != null ? 1 : 0)
                   + (_approachFilter != null ? 1 : 0) + (_myWorldOnly ? 1 : 0);

        var toggle  = ImGui.GetFrameHeight();
        var reset   = active > 0 ? Btn.Measure(l.AroundFilterReset, BtnSize.Small, Icons.Refresh) + Theme.S(Theme.GapS) : 0f;
        var header  = ImGui.GetCursorScreenPos();
        var right   = header.X + ImGui.GetContentRegionAvail().X - Card.RightInset;

        // « Réinitialiser » n'apparaît qu'une fois un filtre posé : sans filtre,
        // il n'y aurait rien à défaire. La flèche, elle, est toujours là.
        Layout.SectionHeader(l.AroundFilters, Icons.Filter, count: active > 0 ? active : null,
            actions: () =>
            {
                if (active > 0)
                {
                    if (Btn.Draw(l.AroundFilterReset, BtnTone.Ghost, BtnSize.Small, Icons.Refresh, id: "around_f_reset"))
                    {
                        _langFilter = _levelFilter = _approachFilter = null;
                        _myWorldOnly = false;
                        _filtersOpen = false;
                    }

                    ImGui.SameLine(0f, Theme.S(Theme.GapS));
                }

                if (Btn.Icon(_filtersOpen ? Icons.ChevronDown : Icons.Chevron, "around_f_toggle"))
                    _filtersOpen = !_filtersOpen;
            },
            actionsWidth: reset + toggle);

        // Tout l'en-tête se clique, hors boutons : la flèche seule est une
        // cible trop petite pour un geste aussi courant.
        var after = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(header);
        if (ImGui.InvisibleButton("##around_f_header",
                                  new Vector2(MathF.Max(1f, right - reset - toggle - Theme.S(Theme.GapM) - header.X),
                                              MathF.Max(1f, after.Y - header.Y - Theme.S(Theme.GapS)))))
            _filtersOpen = !_filtersOpen;
        if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        ImGui.SetCursorScreenPos(after);

        if (!_filtersOpen) return;

        var myWorldOnly = _myWorldOnly;
        if (Inputs.ToggleRow(l.AroundMyWorldOnly, ref myWorldOnly)) _myWorldOnly = myWorldOnly;

        Layout.Spacer(Theme.GapXs);

        // Une colonne de titres de même largeur : les pastilles démarrent
        // toutes à la même abscisse, et les lignes se lisent comme un tableau.
        float column;
        using (Fonts.PushSmall())
            column = new[] { l.AroundFilterLanguage, l.AroundFilterLevel, l.AroundFilterApproach }
                         .Max(t => ImGui.CalcTextSize(t.ToUpperInvariant()).X) + Theme.S(Theme.GapL);

        DrawFilterRow("lang", l.AroundFilterLanguage, column, l,
            [("fr", RpProfileView.LanguageLabel("fr"), null), ("en", RpProfileView.LanguageLabel("en"), null)],
            ref _langFilter);
        DrawFilterRow("level", l.AroundFilterLevel, column, l,
            [Level("beginner", l), Level("casual", l), Level("confirmed", l)],
            ref _levelFilter);
        DrawFilterRow("approach", l.AroundFilterApproach, column, l,
            [("come_to_me", RpProfileView.ApproachLabel("come_to_me", l), null),
             ("i_approach", RpProfileView.ApproachLabel("i_approach", l), null),
             ("either",     RpProfileView.ApproachLabel("either",     l), null)],
            ref _approachFilter);
    }

    /// <summary>
    /// Niveau en nom court, la description en infobulle : « Débutant — Je
    /// découvre le RP » sur une pastille l'étirait sur toute la ligne.
    /// </summary>
    private static (string Key, string Label, string? Tooltip) Level(string key, Loc l)
    {
        var full  = RpProfileView.LevelLabel(key, l);
        var dash  = full.IndexOf(" — ", StringComparison.Ordinal);
        return dash > 0 ? (key, full[..dash], full) : (key, full, null);
    }

    private static void DrawFilterRow(string id, string title, float column, Loc l,
                                      (string Key, string Label, string? Tooltip)[] options, ref string? selected)
    {
        var start = ImGui.GetCursorPosX();
        var lineH = MathF.Round(ImGui.GetTextLineHeight() + Theme.S(8f));

        // Titre centré sur la hauteur des pastilles, peint et non posé comme un
        // élément : posé, il imposait sa hauteur aux pastilles suivantes de la
        // ligne, et seule la première s'alignait.
        using (Fonts.PushSmall())
        {
            var origin = ImGui.GetCursorScreenPos();
            var label  = title.ToUpperInvariant();
            ImGui.GetWindowDrawList().AddText(
                new Vector2(origin.X, origin.Y + (lineH - ImGui.GetTextLineHeight()) * 0.5f),
                ImGui.GetColorU32(Theme.TextFaint), label);
        }

        ImGui.SetCursorPosX(start + column);

        var right = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Card.RightInset;
        var gap   = Theme.S(Theme.GapXs + 2f);

        void Next(string label, bool first)
        {
            if (first) return;
            if (ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + gap + Inputs.PillWidth(label) <= right)
                ImGui.SameLine(0f, gap);
            else
                ImGui.SetCursorPosX(start + column);
        }

        if (Inputs.Pill($"##around_f_{id}_any", l.AroundFilterAny, selected == null))
            selected = null;

        foreach (var (key, label, tooltip) in options)
        {
            Next(label, first: false);
            var active = selected == key;
            if (Inputs.Pill($"##around_f_{id}_{key}", label, active, tooltip))
                selected = active ? null : key;
        }

        Layout.Spacer(Theme.GapXs);
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

        // Même ordre de lecture qu'une carte d'événement : les pastilles d'état
        // en tête, le nom en titre, un résumé en retrait, puis le lieu en
        // pastilles. Tout était jusqu'ici du texte de même poids, et l'œil ne
        // trouvait ni le nom ni l'endroit.
        DrawStatusChips(entry, l, declared);

        // Nom en titre, dans la couleur choisie par le joueur sur le site, à
        // défaut dans l'or des titres.
        var profile   = entry.Profile;
        var nameColor = Theme.TryParseHex(profile?.AccentColor) != null
            ? RpProfileView.Accent(profile)
            : Theme.TitleGold;

        using (Fonts.PushH2())
            ImGui.TextColored(nameColor, Glyphs.Safe(Plugin.DisplayName(profile, entry.CharacterName)));

        Text.Small($"{entry.CharacterName} · {entry.Server}");

        // La citation accompagne le portrait dans la charge utile, envoyée pour
        // étoffer cette liste sans second appel. En retrait, comme le résumé
        // d'un événement, mais dans la couleur du joueur : c'est le seul rappel
        // de son habillage dans cette liste.
        if (profile?.Quote is { Length: > 0 } quote)
        {
            Layout.Spacer(Theme.GapXs);
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Card.RightInset);
            ImGui.TextColored(Theme.Alpha(RpProfileView.Accent(profile), 0.9f), $"« {Glyphs.Safe(quote)} »");
            ImGui.PopTextWrapPos();
        }

        DrawPlaceChips(entry, l, inZone);

        // Ma note privée, tirée du registre des rencontres : c'est ici qu'on
        // décide d'aborder quelqu'un, autant se rappeler ce qu'on en sait.
        if (EncounterRegistry.NoteFor(profile?.CharacterId) is { Length: > 0 } note)
        {
            Layout.Spacer(Theme.GapXs);
            Text.WithIcon(Icons.Edit, note, wrap: true);
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
        // Sans personnage lié, ajouter un ami est refusé par le serveur : la
        // pastille « déjà ami » reste, le bouton disparaît.
        if (entry.Profile?.CharacterId is { Length: > 0 } friendId
            && (Plugin.IsFriend(friendId) || Plugin.Api.HasToken))
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

    /// <summary>
    /// Pastilles d'état, en tête de carte : disponibilité, statut d'équipe,
    /// niveau, approche. Chacune ne reste sur la ligne que si elle y tient : la
    /// colonne est étroite, le portrait en mange déjà 128 px.
    /// </summary>
    private static void DrawStatusChips(RpAvailabilityEntryDto entry, Loc l, bool declared)
    {
        var flow = FlowRow.Fill(Theme.S(Theme.GapXs));

        // Le vert pour qui a levé la main, l'ambre pour le seul tag allumé : la
        // mise en garde doit tenir sur la ligne qu'on lit, pas seulement en tête
        // de section.
        if (declared)
        {
            flow.Next(Chip.Measure(l.AroundAvailableChip, Icons.RpLive));
            Chip.Draw(l.AroundAvailableChip, ChipTone.Success, Icons.RpLive);
        }
        else
        {
            flow.Next(Chip.Measure(l.AroundRpTaggedChip, Icons.RpLive));
            Chip.Draw(l.AroundRpTaggedChip, ChipTone.Warning, Icons.RpLive);
        }

        if (entry.Profile is not { } profile)
        {
            Layout.Spacer(Theme.GapXs);
            return;
        }

        // Extrait servi par la route publique des disponibilités : le
        // consentement y est déjà appliqué, `staffBadgeVisible` n'y figure pas.
        // Le badge nomme le site, il est long : il passe à la ligne s'il le faut.
        if (RpProfileView.StaffBadgeLabel(profile, l, requireConsent: false) is { } staff)
            flow.Next(Chip.Measure(staff, Icons.Shield));
        RpProfileView.StaffBadge(profile, l, requireConsent: false);

        // Niveau en nom court, sa description au survol : « Débutant — Je
        // découvre le RP » étirait la pastille sur toute la ligne.
        var level = RpProfileView.LevelLabel(profile.RpLevel, l);
        var dash  = level.IndexOf(" — ", StringComparison.Ordinal);
        var shortLevel = dash > 0 ? level[..dash] : level;
        flow.Next(Chip.Measure(shortLevel));
        Chip.Draw(shortLevel, ChipTone.Neutral);
        if (dash > 0) Feedback.TooltipOnHover(level);

        var approach = RpProfileView.ApproachLabel(profile.ApproachMode, l);
        flow.Next(Chip.Measure(approach));
        Chip.Draw(approach, ChipTone.Accent);

        if (profile.Nsfw)
        {
            flow.Next(Chip.Measure(l.RpProfileNsfw, Icons.Warning));
            Chip.Draw(l.RpProfileNsfw, ChipTone.Danger, Icons.Warning);
        }

        Layout.Spacer(Theme.GapXs);
    }

    /// <summary>
    /// Où trouver la personne, en pastilles comme le lieu d'un événement : la
    /// zone, puis les coordonnées au format du jeu, c'est ainsi qu'on se donne
    /// rendez-vous.
    /// </summary>
    private static void DrawPlaceChips(RpAvailabilityEntryDto entry, Loc l, bool inZone)
    {
        var zone   = entry.Zone is { Length: > 0 } z ? z : null;
        var coords = Plugin.HasFreshPosition(entry)
            ? $"X {entry.PosX!.Value:F1}  Y {entry.PosZ!.Value:F1}"
              + (entry.InstanceId is > 0 and var instance ? $" · {string.Format(l.AroundInstance, instance)}" : "")
            : null;

        if (zone == null && coords == null) return;

        Layout.Spacer(Theme.GapS);
        var flow = FlowRow.Fill(Theme.S(Theme.GapXs));

        // En vert quand la personne est dans ma zone : c'est la seule différence
        // visible entre une carte de la section « dans ma zone » et les autres.
        if (zone != null)
        {
            flow.Next(Chip.Measure(zone, Icons.Location));
            Chip.Draw(zone, inZone ? ChipTone.Success : ChipTone.Neutral, Icons.Location);
        }

        if (coords != null)
        {
            flow.Next(Chip.Measure(coords, Icons.Map));
            Chip.Draw(coords, ChipTone.Neutral, Icons.Map);
        }
    }

    private static void OpenSite(string path) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl + path)
            { UseShellExecute = true });
}

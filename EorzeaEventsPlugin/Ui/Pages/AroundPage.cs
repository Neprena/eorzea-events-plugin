using Dalamud.Bindings.ImGui;
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

        // Deux listes plutôt qu'une seule triée ensemble : on ne cherche pas la
        // même chose dans l'une et dans l'autre. Les déclarés attendent qu'on les
        // aborde, les seconds jouent leur personnage sans avoir rien demandé.
        var declared = entries.Where(IsDeclared).ToList();
        var tagged   = entries.Where(e => !IsDeclared(e)).ToList();

        using var scroll = ImRaii.Child("##aroundscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        if (declared.Count > 0)
        {
            Layout.SectionHeader(string.Format(l.AroundCount, declared.Count), Icons.Around);
            foreach (var entry in declared) DrawEntry(entry, l);
        }

        if (tagged.Count > 0)
        {
            if (declared.Count > 0) Layout.Spacer(Theme.GapM);
            Layout.SectionHeader(string.Format(l.AroundRpTaggedCount, tagged.Count), Icons.RpLive);
            // La mise en garde une fois en tête de section, et non sur chaque
            // carte : répétée, elle ne se lirait plus.
            Text.Small(l.AroundRpTaggedHint, Theme.TextMuted);
            Layout.Spacer(Theme.GapXs);
            foreach (var entry in tagged) DrawEntry(entry, l);
        }

        // Respiration en fin de liste, pour que la dernière carte ne soit pas
        // collée au bord bas de la zone défilante.
        Layout.Spacer(Theme.GapXl);
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

    private static void DrawEntry(RpAvailabilityEntryDto entry, Loc l)
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

        if (entry.Zone is { Length: > 0 } zone)
            Text.WithIcon(Icons.Location, zone, wrap: true);

        // La citation accompagne le portrait dans la charge utile, envoyée pour
        // étoffer cette liste sans second appel. Elle n'était pas affichée.
        //
        // Elle porte la couleur d'accent du joueur, seul rappel de son habillage
        // dans cette liste : le liseré de la carte reste réservé au statut de
        // disponibilité, et la bannière n'est pas envoyée pour cette vue.
        if (entry.Profile?.Quote is { Length: > 0 } quote)
            Text.Small($"« {quote} »", RpProfileView.Accent(entry.Profile));

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

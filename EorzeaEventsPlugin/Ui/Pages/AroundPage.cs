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

        Layout.SectionHeader(string.Format(l.AroundCount, entries.Count), Icons.Around);

        using var scroll = ImRaii.Child("##aroundscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        foreach (var entry in entries) DrawEntry(entry, l);

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

    /// <summary>Recherche sur le nom du personnage comme sur son nom RP.</summary>
    private static bool Matches(RpAvailabilityEntryDto e, string query) =>
        e.CharacterName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (e.Profile?.RpName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

    private static void DrawEntry(RpAvailabilityEntryDto entry, Loc l)
    {
        using var card = Card.Begin($"around_{entry.Id}", interactive: false, accent: Theme.Online);

        // Plus petit que sur la fiche : une carte de liste doit rester compacte,
        // mais 64 rendait le portrait illisible.
        RpProfileView.DrawPortrait(entry.Profile?.PortraitUrl, entry.CharacterName,
                                   height: 128f, status: Theme.Online, id: entry.Id);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        Text.Body(entry.Profile?.RpName is { Length: > 0 } rpName ? rpName : entry.CharacterName);
        Text.Small($"{entry.CharacterName} · {entry.Server}");

        if (entry.Zone is { Length: > 0 } zone)
            Text.WithIcon(Icons.Location, zone, wrap: true);

        if (entry.Profile is { } profile)
        {
            Layout.Spacer(Theme.GapXs);
            Chip.Draw(RpProfileView.LevelLabel(profile.RpLevel, l), ChipTone.Neutral);
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(RpProfileView.ApproachLabel(profile.ApproachMode, l), ChipTone.Accent);

            if (profile.Nsfw)
            {
                ImGui.SameLine(0f, Theme.S(Theme.GapXs));
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

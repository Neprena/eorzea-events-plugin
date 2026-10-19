using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Personnages croisés en jeu, mémoire locale du plugin (voir
/// <see cref="EncounterRegistry"/>).
///
/// « Autour de moi » ne montre que l'instant : un personnage en sort dès que son
/// porteur coupe son tag ou se déconnecte, et sa fiche devient introuvable.
/// Cette page garde la trace de qui on a croisé, avec une note privée par
/// personnage.
///
/// Aucun appel réseau au rendu : tout vient de la configuration.
/// </summary>
internal sealed class EncountersPage
{
    private const int MaxNote = 200;

    private string _search = string.Empty;

    /// <summary>Note en cours d'édition, enregistrée à la validation seulement.</summary>
    private string? _editingNoteId;
    private string  _noteDraft = string.Empty;

    /// <summary>Entrée vers laquelle faire défiler au prochain rendu.</summary>
    private string? _scrollToId;

    /// <summary>Ouvre l'éditeur de note d'une entrée et y fait défiler.</summary>
    public void FocusNote(string characterId)
    {
        _search        = string.Empty;
        _editingNoteId = characterId;
        _noteDraft     = EncounterRegistry.Get(characterId)?.Note ?? string.Empty;
        _scrollToId    = characterId;
    }

    public void Draw()
    {
        var l = Plugin.L;

        Layout.Spacer(Theme.GapXs);
        Feedback.Alert(Theme.Accent, Icons.Info, l.TabEncounters, l.EncountersNoticeBody);

        var all = Plugin.Config.Encounters.Values
            .OrderByDescending(EncounterRegistry.SortKey)
            .ToList();

        if (all.Count == 0)
        {
            Feedback.EmptyState(Icons.History, l.EncountersEmpty);
            return;
        }

        Inputs.SearchBar("##encsearch", ref _search, l.EncountersSearchHint);
        Layout.Spacer(Theme.GapS);

        var query = _search.Trim();
        var shown = all.Where(e => query.Length == 0 || Matches(e, query)).ToList();

        Layout.SectionHeader(string.Format(l.EncountersCount, shown.Count), Icons.History,
                             actions: DrawClearAll,
                             actionsWidth: Btn.MeasureHold(l.EncountersClearAll, Icons.Trash, BtnSize.Small));

        if (shown.Count == 0)
        {
            Feedback.EmptyState(Icons.Search, l.AroundNoMatch);
            return;
        }

        using var scroll = ImRaii.Child("##encscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        foreach (var e in shown) DrawEntry(e, l);

        Layout.Spacer(Theme.GapXl);
    }

    /// <summary>
    /// Effacement à maintenir, comme toute suppression du plugin. Plus long
    /// qu'ailleurs : il emporte toutes les notes d'un coup, sans retour.
    /// </summary>
    private void DrawClearAll()
    {
        if (!Btn.Hold(Plugin.L.EncountersClearAll, "enc_clear", Icons.Trash, seconds: 1.5f)) return;

        EncounterRegistry.Clear();
        _editingNoteId = null;
        _scrollToId    = null;
    }

    private static bool Matches(RpEncounter e, string query) =>
        e.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (e.RpName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
        || (e.Note?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

    private void DrawEntry(RpEncounter e, Loc l)
    {
        var online = Plugin.FindAvailableEntryByCharacterId(e.CharacterId) != null;
        var friend = Plugin.IsFriend(e.CharacterId);

        using var card = Card.Begin($"enc_{e.CharacterId}", interactive: false,
                                    accent: online ? Theme.Online : null);

        if (_scrollToId == e.CharacterId)
        {
            ImGui.SetScrollHereY(0f);
            _scrollToId = null;
        }

        RpProfileView.DrawPortrait(e.PortraitUrl, e.Name, height: 96f,
                                   status: online ? Theme.Online : Theme.TextFaint,
                                   id: e.CharacterId);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        // Même ordre de lecture qu'« Autour de moi » et qu'un événement : les
        // pastilles d'état en tête, le nom en titre, puis quand et où en
        // pastilles. Tout était du petit texte de même poids.
        DrawStatusChips(e, online, friend, l);

        // Nom en titre, dans la couleur choisie par le joueur sur le site, à
        // défaut dans l'or des titres.
        var color = Theme.TryParseHex(e.AccentColor) is { } custom ? Theme.EnsureReadable(custom) : Theme.TitleGold;
        using (Fonts.PushH2())
            ImGui.TextColored(color, Glyphs.Safe(e.RpName is { Length: > 0 } rpName ? rpName : e.Name));

        Text.Small($"{e.Name} · {e.World}");

        DrawWhenChips(e, l);

        if (_editingNoteId != e.CharacterId && e.Note is { Length: > 0 } note)
        {
            Layout.Spacer(Theme.GapXs);
            Text.WithIcon(Icons.Edit, note, wrap: true);
        }

        ImGui.EndGroup();

        Layout.Spacer(Theme.GapS);

        if (_editingNoteId == e.CharacterId) DrawNoteEditor(e, l);
        else                                 DrawActions(e, friend, l);
    }

    /// <summary>En ligne, ami RP, nombre de rencontres : ce qui distingue une entrée des autres.</summary>
    private static void DrawStatusChips(RpEncounter e, bool online, bool friend, Loc l)
    {
        var visits = e.Visits > 1 ? string.Format(l.EncounterVisits, e.Visits) : null;
        if (!online && !friend && visits == null) return;

        var flow = FlowRow.Fill(Theme.S(Theme.GapXs));

        if (online)
        {
            flow.Next(Chip.Measure(l.EncounterOnline, Icons.RpLive));
            Chip.Draw(l.EncounterOnline, ChipTone.Success, Icons.RpLive);
        }

        if (friend)
        {
            flow.Next(Chip.Measure(l.RpFriendChip, Icons.Friend));
            Chip.Draw(l.RpFriendChip, ChipTone.Accent, Icons.Friend);
        }

        if (visits != null)
        {
            flow.Next(Chip.Measure(visits, Icons.History));
            Chip.Draw(visits, ChipTone.Neutral, Icons.History);
        }

        Layout.Spacer(Theme.GapXs);
    }

    /// <summary>
    /// Quand et où, en pastilles comme le lieu d'un événement : le moment, puis
    /// la zone du dernier aperçu quand il y en a une.
    /// </summary>
    private static void DrawWhenChips(RpEncounter e, Loc l)
    {
        var when = WhenLine(e, l, withZone: false);
        var zone = e.LastSeenAt != null && e.LastZone is { Length: > 0 } z ? z : null;

        Layout.Spacer(Theme.GapS);
        var flow = FlowRow.Fill(Theme.S(Theme.GapXs));

        flow.Next(Chip.Measure(when, Icons.Clock));
        Chip.Draw(when, ChipTone.Neutral, Icons.Clock);

        if (zone != null)
        {
            flow.Next(Chip.Measure(zone, Icons.Location));
            Chip.Draw(zone, ChipTone.Neutral, Icons.Location);
        }
    }

    /// <summary>« Vu il y a … · zone », ou « Fiche consultée il y a … » sans aperçu.</summary>
    /// <param name="withZone">Faux quand la zone s'affiche à part, dans sa propre pastille.</param>
    private static string WhenLine(RpEncounter e, Loc l, bool withZone = true)
    {
        if (e.LastSeenAt is { } seen)
        {
            var when = Relative(seen, l);
            return withZone && e.LastZone is { Length: > 0 } zone
                ? string.Format(l.EncounterSeen, when, zone)
                : string.Format(l.EncounterSeenNoZone, when);
        }

        if (e.LastOpenedAt is { } opened)
            return string.Format(l.EncounterOpened, Relative(opened, l));

        return string.Format(l.EncounterMet, Relative(e.FirstMetAt, l));
    }

    private static string Relative(DateTime utc, Loc l)
    {
        var span = DateTime.UtcNow - utc;
        if (span < TimeSpan.FromMinutes(1)) return l.TimeJustNow;
        if (span < TimeSpan.FromHours(1))   return string.Format(l.TimeMinutesAgo, (int)span.TotalMinutes);
        if (span < TimeSpan.FromHours(24))  return string.Format(l.TimeHoursAgo, (int)span.TotalHours);
        if (span < TimeSpan.FromHours(48))  return l.TimeYesterday;
        return string.Format(l.TimeDaysAgo, (int)span.TotalDays);
    }

    private void DrawNoteEditor(RpEncounter e, Loc l)
    {
        Inputs.Field($"##encnote_{e.CharacterId}", l.RpFriendNote, ref _noteDraft, MaxNote,
                     showCounter: true);

        if (Btn.Draw(l.Save, BtnTone.Primary, BtnSize.Small, id: $"encnote_ok_{e.CharacterId}"))
        {
            EncounterRegistry.SetNote(e.CharacterId, _noteDraft);
            _editingNoteId = null;
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Small, id: $"encnote_no_{e.CharacterId}"))
            _editingNoteId = null;
    }

    private void DrawActions(RpEncounter e, bool friend, Loc l)
    {
        // L'action principale de la carte, dans l'or des actions, comme
        // « Profil RP » dans « Autour de moi ».
        if (Btn.Draw(l.MenuViewRpProfile, BtnTone.Primary, BtnSize.Small, Icons.Character,
                     id: $"enc_prof_{e.CharacterId}"))
            Plugin.OpenRpProfileViewer(e.CharacterId, e.Name, e.World);

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.RpFriendNote, BtnTone.Ghost, BtnSize.Small, Icons.Edit,
                     id: $"enc_note_{e.CharacterId}"))
        {
            _editingNoteId = e.CharacterId;
            _noteDraft     = e.Note ?? string.Empty;
        }

        if (!friend)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.RpFriendAdd, BtnTone.Ghost, BtnSize.Small, Icons.FriendAdd,
                         tooltip: l.RpFriendAddHint, id: $"enc_friend_{e.CharacterId}"))
                Plugin.AddFriend(e.CharacterId, 0, e.Name);
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        // Oubli à maintenir, même geste que partout ailleurs : lâcher avant
        // que le bouton soit plein annule.
        if (Btn.Hold(l.EncounterForget, $"enc_del_{e.CharacterId}", Icons.Trash))
            EncounterRegistry.Forget(e.CharacterId);
    }
}

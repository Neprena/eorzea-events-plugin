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

    private string  _search = string.Empty;
    private string? _confirmingId;
    private bool    _confirmingClear;

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
                             actionsWidth: Btn.Measure(l.EncountersClearAllArm, Icons.Trash));

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

    /// <summary>Effacement en deux clics, comme toute suppression du plugin.</summary>
    private void DrawClearAll()
    {
        var l     = Plugin.L;
        var armed = _confirmingClear;

        if (Btn.Draw(armed ? l.EncountersClearAllArm : l.EncountersClearAll,
                     armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Small, Icons.Trash,
                     id: "enc_clear"))
        {
            if (armed)
            {
                EncounterRegistry.Clear();
                _confirmingClear = false;
                _confirmingId    = null;
                _editingNoteId   = null;
                _scrollToId      = null;
            }
            else _confirmingClear = true;
        }

        if (armed && !ImGui.IsItemHovered()) _confirmingClear = false;
    }

    private static bool Matches(RpEncounter e, string query) =>
        e.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (e.RpName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
        || (e.Note?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

    private void DrawEntry(RpEncounter e, Loc l)
    {
        var online = Plugin.FindAvailableEntryByCharacterId(e.CharacterId) != null;
        var friend = Plugin.IsFriend(e.CharacterId);
        var accent = Theme.EnsureReadable(Theme.TryParseHex(e.AccentColor) ?? Theme.Accent);

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

        Text.Body(e.RpName is { Length: > 0 } rpName ? rpName : e.Name, accent);
        Text.Small($"{e.Name} · {e.World}");
        Text.Small(WhenLine(e, l));
        if (e.Visits > 1) Text.Small(string.Format(l.EncounterVisits, e.Visits));

        if (online || friend)
        {
            Layout.Spacer(Theme.GapXs);
            if (online) Chip.Draw(l.EncounterOnline, ChipTone.Success);
            if (online && friend) ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            if (friend) Chip.Draw(l.RpFriendChip, ChipTone.Accent, Icons.Friend);
        }

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

    /// <summary>« Vu il y a … · zone », ou « Fiche consultée il y a … » sans aperçu.</summary>
    private static string WhenLine(RpEncounter e, Loc l)
    {
        if (e.LastSeenAt is { } seen)
        {
            var when = Relative(seen, l);
            return e.LastZone is { Length: > 0 } zone
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
        if (Btn.Draw(l.MenuViewRpProfile, BtnTone.Ghost, BtnSize.Small, Icons.Character,
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

        // Oubli en deux temps : le premier clic arme, le second exécute, et
        // quitter le bouton désarme. Même geste que partout ailleurs.
        var armed = _confirmingId == e.CharacterId;
        if (Btn.Draw(armed ? l.EncounterForgetArm : l.EncounterForget,
                     armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Small, Icons.Trash,
                     id: $"enc_del_{e.CharacterId}"))
        {
            if (armed) { EncounterRegistry.Forget(e.CharacterId); _confirmingId = null; }
            else _confirmingId = e.CharacterId;
        }

        if (armed && !ImGui.IsItemHovered()) _confirmingId = null;
    }
}

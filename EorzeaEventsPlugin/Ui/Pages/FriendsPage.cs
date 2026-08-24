using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Personnages à qui la fiche du personnage courant est ouverte.
///
/// C'est une liste d'accès, pas un carnet d'adresses : elle ne dit rien des
/// autres, ne donne accès à rien, et personne n'apprend qu'il y figure. Le
/// bandeau d'en-tête existe pour ça : « ajouter comme ami » se lit spontanément
/// comme une relation mutuelle, alors que le geste n'ouvre que sa propre fiche.
///
/// Aucun appel réseau au rendu : <c>Plugin.Friends</c> est chargé par la boucle
/// du plugin et rafraîchi après chaque ajout ou retrait.
/// </summary>
internal sealed class FriendsPage
{
    private string  _search       = string.Empty;
    private string? _confirmingId;

    /// <summary>Note en cours d'édition, pour n'envoyer qu'à la validation.</summary>
    private string? _editingNoteId;
    private string  _noteDraft = string.Empty;

    public void Draw()
    {
        var l = Plugin.L;

        if (Plugin.CurrentCharacter is null)
        {
            Feedback.EmptyState(Icons.Character, l.RpProfileNoCharacter);
            return;
        }

        Layout.Spacer(Theme.GapXs);

        Feedback.Alert(Theme.Accent, Icons.Info, l.RpFriendsTitle, l.RpFriendsNoticeBody);

        var friends = Plugin.Friends;
        if (friends.Count == 0)
        {
            Feedback.EmptyState(Icons.Friend, l.RpFriendsEmpty);
            return;
        }

        // Le filtre n'apparaît qu'une fois la liste assez longue pour le mériter.
        if (friends.Count > 8)
        {
            Inputs.SearchBar("##friendsearch", ref _search, l.Search);
            Layout.Spacer(Theme.GapS);
        }

        var shown = friends
            .Where(f => _search.Length == 0
                     || f.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
                     || f.AddedAsName.Contains(_search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Layout.SectionHeader(string.Format(l.AroundCount, shown.Count), Icons.Friend);

        if (shown.Count == 0)
        {
            Feedback.EmptyState(Icons.Search, l.AroundNoMatch);
            return;
        }

        using var scroll = ImRaii.Child("##friendsscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        foreach (var friend in shown) DrawEntry(friend, l);

        Layout.Spacer(Theme.GapXl);
    }

    private void DrawEntry(Api.RpFriendDto friend, Loc l)
    {
        using var card = Card.Begin($"friend_{friend.CharacterId}", interactive: false);

        // Pas de portrait : la liste ne les connaît pas, et les charger ferait
        // une requête par ligne pour un simple écran de gestion.
        Layout.Avatar(friend.Name, 40f);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        Text.Body(friend.Name);
        Text.Small(friend.WorldName);

        // Un renommage ne casse pas l'accès, mais rend la ligne méconnaissable :
        // le nom d'origine est rappelé.
        if (!string.Equals(friend.AddedAsName, friend.Name, StringComparison.Ordinal))
            Text.Small(string.Format(l.RpFriendRenamed, friend.AddedAsName));

        if (friend.Mutual)
        {
            Layout.Spacer(Theme.GapXs);
            Chip.Draw(l.RpFriendMutual, ChipTone.Accent, Icons.Friend);
        }

        ImGui.EndGroup();

        Layout.Spacer(Theme.GapS);

        if (_editingNoteId == friend.CharacterId) DrawNoteEditor(friend, l);
        else                                      DrawNoteAndActions(friend, l);
    }

    private void DrawNoteEditor(Api.RpFriendDto friend, Loc l)
    {
        Inputs.Field($"##note_{friend.CharacterId}", l.RpFriendNote, ref _noteDraft, 120);

        if (Btn.Draw(l.Save, BtnTone.Primary, BtnSize.Small, id: $"note_ok_{friend.CharacterId}"))
        {
            Plugin.SetFriendNote(friend.CharacterId, _noteDraft);
            _editingNoteId = null;
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Small, id: $"note_no_{friend.CharacterId}"))
            _editingNoteId = null;
    }

    private void DrawNoteAndActions(Api.RpFriendDto friend, Loc l)
    {
        if (friend.Note is { Length: > 0 } note)
        {
            Text.Small(note);
            Layout.Spacer(Theme.GapXs);
        }

        // Consulter la fiche d'un ami depuis sa liste : le seul chemin existant
        // passait par « Autour de moi », donc supposait qu'il soit connecté et
        // déclaré disponible à cet instant.
        if (Btn.Draw(l.MenuViewRpProfile, BtnTone.Ghost, BtnSize.Small, Icons.Character,
                     id: $"friend_prof_{friend.CharacterId}"))
            Plugin.OpenRpProfileViewer(friend.CharacterId, friend.Name, friend.WorldName);

        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        if (Btn.Draw(l.RpFriendNote, BtnTone.Ghost, BtnSize.Small, Icons.Edit,
                     id: $"note_edit_{friend.CharacterId}"))
        {
            _editingNoteId = friend.CharacterId;
            _noteDraft     = friend.Note ?? string.Empty;
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        // Retrait en deux temps : le premier clic arme, le second exécute, et
        // quitter le bouton désarme. Même geste que partout ailleurs.
        var armed = _confirmingId == friend.CharacterId;

        if (Btn.Draw(armed ? l.RpFriendRemoveArm : l.RpFriendRemove,
                     armed ? BtnTone.Danger : BtnTone.Ghost,
                     BtnSize.Small, Icons.Trash, id: $"friend_del_{friend.CharacterId}"))
        {
            if (armed)
            {
                Plugin.RemoveFriend(friend.CharacterId, friend.Name);
                _confirmingId = null;
            }
            else
            {
                _confirmingId = friend.CharacterId;
            }
        }

        if (armed && !ImGui.IsItemHovered()) _confirmingId = null;
    }
}

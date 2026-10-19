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
    private string _search = string.Empty;

    /// <summary>
    /// Retraits demandés mais encore annulables : la ligne disparaît tout de
    /// suite, la requête ne part qu'à l'échéance de la notification. Annuler ne
    /// demande donc aucun aller-retour, et la note de l'ami n'est jamais perdue.
    /// </summary>
    private readonly HashSet<string> _pendingRemoval = [];

    /// <summary>
    /// Retraits envoyés, avec leur heure d'envoi. L'ami reste masqué tant
    /// que la liste le contient encore, le temps de l'aller-retour : sans quoi
    /// n'importe quel rafraîchissement arrivé entre-temps le ferait réapparaître.
    /// Passé <see cref="CommitGrace"/>, le serveur a répondu ; s'il a refusé,
    /// l'ami revient, comme il se doit.
    /// </summary>
    private readonly Dictionary<string, DateTime> _committedRemoval = [];

    private static readonly TimeSpan CommitGrace = TimeSpan.FromSeconds(15);

    /// <summary>Amis affichés, retraits en cours exclus. Sert aussi au compteur de la barre latérale.</summary>
    public int VisibleCount => Plugin.Friends.Count(f => !IsHidden(f.CharacterId));

    private bool IsHidden(string id) =>
        _pendingRemoval.Contains(id)
        || (_committedRemoval.TryGetValue(id, out var at) && DateTime.UtcNow - at < CommitGrace);

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

        // Sans jeton la liste reste vide de toute façon : afficher « aucun
        // ami » laisserait croire que le compte n'en a pas.
        if (!Plugin.Api.HasToken)
        {
            Feedback.EmptyState(Icons.Friend, l.NoCharacterLinked, l.RpFriendsNoToken,
                                l.LinkThisCharacter, () => Plugin.OpenSetup());
            return;
        }

        Layout.Spacer(Theme.GapXs);

        Feedback.Alert(Theme.Accent, Icons.Info, l.RpFriendsTitle, l.RpFriendsNoticeBody);

        var friends = Plugin.Friends.Where(f => !IsHidden(f.CharacterId)).ToList();
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

    private void RequestRemoval(Api.RpFriendDto friend)
    {
        var id   = friend.CharacterId;
        var name = friend.Name;

        // Figé maintenant : la requête part plus tard, peut-être après une
        // déconnexion ou un changement de personnage.
        var token = Plugin.Api.CurrentToken;

        _pendingRemoval.Add(id);

        Snackbar.Show(
            string.Format(Plugin.L.RpFriendRemovedToast, name),
            Plugin.L.Undo,
            onAction: () => _pendingRemoval.Remove(id),
            onExpire: () =>
            {
                _pendingRemoval.Remove(id);
                _committedRemoval[id] = DateTime.UtcNow;
                Plugin.RemoveFriend(id, name, token);
            });
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

        // Retrait à maintenir, puis annulable le temps de la notification.
        if (Btn.Hold(l.RpFriendRemove, $"friend_del_{friend.CharacterId}", Icons.Trash))
            RequestRemoval(friend);
    }
}

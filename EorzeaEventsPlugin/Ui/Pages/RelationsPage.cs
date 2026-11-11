using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Ui.Components;
using System;
using System.Globalization;
using System.Linq;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Les relations de la fiche publiée, et les demandes qui y mènent.
///
/// Elles vivaient au fond de l'onglet « Jeu » de la fiche, sous les accroches et
/// les limites, dans une carte qui disparaissait quand elle était vide : la
/// pastille du menu menait à une page qui ne montrait rien, et personne ne
/// trouvait ses demandes. Elles ont désormais leur page, toujours au même
/// endroit, qui dit aussi quand il n'y a rien.
///
/// Trois cartes, dans l'ordre de l'urgence : ce qui attend une réponse, ce qui
/// figure sur la fiche, ce qu'on a envoyé. Aucun appel réseau au rendu en
/// dehors de la relecture périodique des relations : les demandes sont relevées
/// par la boucle du plugin.
/// </summary>
internal sealed class RelationsPage
{
    /// <summary>Plafond de RP_MAX_RELATIONS, appliqué aussi par le serveur.</summary>
    private const int MaxRelations = 8;

    /// <summary>
    /// Âge au-delà duquel la page relit les relations. Personne ne prévient
    /// quand l'autre côté retire un lien : sans relecture, la page garderait une
    /// ligne qui n'existe plus.
    /// </summary>
    private static readonly TimeSpan Freshness = TimeSpan.FromSeconds(60);

    private string _error = string.Empty;

    public void Draw()
    {
        var l = Plugin.L;

        if (Plugin.CurrentCharacter is null)
        {
            Feedback.EmptyState(Icons.Character, l.RpProfileNoCharacter);
            return;
        }

        if (!Plugin.Api.HasToken)
        {
            Feedback.EmptyState(Icons.Link, l.NoCharacterLinked, l.RpRelationNeedProfile,
                                l.LinkThisCharacter, () => Plugin.OpenSetup());
            return;
        }

        Plugin.EnsureOwnRelationsFresh(Freshness);

        // Sans fiche, rien ne peut s'y lier, et aucune demande ne peut arriver :
        // le serveur exige une fiche des deux côtés.
        if (Plugin.OwnRelationsStatus == Plugin.OwnRelationsState.NoProfile)
        {
            Feedback.EmptyState(Icons.Link, l.RpRelationNeedProfile, null,
                                l.RpProfileTitle, Plugin.OpenRpProfileWizard);
            return;
        }

        Layout.Spacer(Theme.GapXs);

        DrawElsewhere(l);
        DrawReceived(l);
        DrawMine(l);
        DrawSent(l);

        if (_error.Length > 0)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Hint(_error, Theme.Danger);
        }
    }

    /// <summary>
    /// Demandes adressées aux autres personnages du compte. Le jeton ne désigne
    /// que celui qu'on joue : on ne peut y répondre qu'en le jouant, mais on doit
    /// au moins savoir qu'elles existent.
    /// </summary>
    private static void DrawElsewhere(Loc l)
    {
        foreach (var other in Plugin.RelationRequestsElsewhere)
        {
            var text = other.Count == 1
                ? string.Format(l.RpRelationsElsewhereOne, other.Character.Name, other.Character.WorldName)
                : string.Format(l.RpRelationsElsewhereMany, other.Character.Name, other.Character.WorldName,
                                other.Count);

            Text.WithIcon(Icons.Info, text, Theme.Accent, wrap: true);
            Layout.Spacer(Theme.GapS);
        }
    }

    // ─── À toi de répondre ────────────────────────────────────────────────────

    private void DrawReceived(Loc l)
    {
        var received = Plugin.RelationRequestsReceived;

        using var card = Card.Begin("relations_received", interactive: false);
        Layout.SectionHeader(l.RpRelationsReceivedTitle, Icons.Inbox,
                             received.Count > 0 ? received.Count : null);

        // La carte reste là même vide : c'est l'endroit où l'on vient vérifier,
        // et une absence ne dit pas « rien en attente ».
        if (received.Count == 0)
        {
            Text.Body(l.RpRelationsReceivedEmpty);
            Text.Hint(l.RpRelationsReceivedEmptyHint);
            return;
        }

        for (var i = 0; i < received.Count; i++)
        {
            if (i > 0) Layout.Divider(Theme.GapS);
            DrawReceivedRequest(received[i], l);
        }

        Layout.Spacer(Theme.GapS);
        Text.Hint(l.RpRelationsReceivedHint);
    }

    /// <summary>
    /// Une demande reçue. Accepter ouvre la fenêtre de relation, où se choisit
    /// le type de sa propre ligne ; ignorer se fait ici, en maintenant le bouton.
    /// </summary>
    private void DrawReceivedRequest(Api.RpRelationRequestDto request, Loc l)
    {
        var who = request.Character;

        DrawWho(who, request.CreatedAt, l);

        Text.Muted(l.RpRelationsProposes);
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        Chip.Draw(RpProfileView.RelationLabel(request.Kind, l), ChipTone.Accent);

        if (request.Note is { Length: > 0 } note) Text.Small(note, Theme.TextMuted);

        Layout.Spacer(Theme.GapXs);

        if (Btn.Draw(l.RpRelationAccept, BtnTone.Primary, BtnSize.Small, Icons.Check,
                     id: $"relations_ok_{request.Id}"))
            Plugin.OpenRelationResponse(request.Id, who.Name, who.WorldName, request.Kind);

        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        if (Btn.Hold(l.RpRelationDecline, $"relations_no_{request.Id}"))
        {
            _error = string.Empty;
            Plugin.DeclineRelationRequest(request.Id, ok =>
            {
                if (!ok) _error = ServerNotice.Explain(Plugin.L.RpRelationFailed);
            });
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        if (Btn.Draw(l.MenuViewRpProfile, BtnTone.Ghost, BtnSize.Small,
                     id: $"relations_view_{request.Id}"))
            Plugin.OpenRpProfileViewer(who.Id, who.Name, who.WorldName);
    }

    // ─── Sur ta fiche ─────────────────────────────────────────────────────────

    private static void DrawMine(Loc l)
    {
        var relations = Plugin.OwnRelations;

        using var card = Card.Begin("relations_mine", interactive: false);
        Layout.SectionHeader($"{l.RpRelationsMineTitle}  ·  {relations.Count}/{MaxRelations}", Icons.Link);

        if (relations.Count == 0)
        {
            if (Plugin.OwnRelationsStatus == Plugin.OwnRelationsState.Unknown)
                Text.Muted(l.Loading);
            else
                DrawWays(l);
            return;
        }

        for (var i = 0; i < relations.Count; i++)
        {
            if (i > 0) Layout.Divider(Theme.GapS);
            DrawOwnRelation(relations[i], i, l);
        }

        Layout.Spacer(Theme.GapS);

        if (relations.Count < MaxRelations
            && Btn.Draw(l.RpRelationFreeAdd, BtnTone.Ghost, BtnSize.Small, Icons.Plus,
                        id: "relations_add_free"))
            Plugin.OpenRelationFree();

        Text.Hint(l.RpRelationsLinkedHint);
    }

    /// <summary>
    /// Une ligne de sa fiche. Liée, elle vient d'une demande acceptée et figure
    /// aussi sur la fiche d'en face ; en texte libre, elle n'est qu'à soi.
    /// </summary>
    private static void DrawOwnRelation(Api.RpRelationDto relation, int index, Loc l)
    {
        ImGui.AlignTextToFramePadding();
        Text.Body(relation.TargetName);

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        Chip.Draw(RpProfileView.RelationLabel(relation.Kind, l), ChipTone.Accent, alignToFrame: true);

        ImGui.SameLine(0f, Theme.S(Theme.GapXs));
        Chip.Draw(relation.Linked ? l.RpProfileRelationLinked : l.RpRelationsFreeChip,
                  relation.Linked ? ChipTone.Success : ChipTone.Neutral, alignToFrame: true);

        if (relation.Id is { Length: > 0 } relationId)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));

            if (Btn.Draw(l.RpRelationEdit, BtnTone.Ghost, BtnSize.Small, Icons.Edit,
                         id: $"relations_edit_{index}"))
                Plugin.OpenRelationEdit(relationId, relation.TargetName, relation.Kind,
                                        relation.Note, relation.Linked);
        }

        if (relation.Note is { Length: > 0 } note) Text.Small(note, Theme.TextMuted);
    }

    /// <summary>
    /// Fiche sans relation : les deux façons d'en ajouter, dites. La page
    /// n'offre qu'un bouton, celui du texte libre ; un lien vers une fiche se
    /// propose depuis cette fiche, et la page doit le dire.
    /// </summary>
    private static void DrawWays(Loc l)
    {
        Text.Wrapped(l.RpRelationsEmptyIntro);
        Layout.Spacer(Theme.GapS);

        Text.WithIcon(Icons.Link, l.RpRelationsWayLinkedTitle, Theme.Gold, Theme.Text);
        Text.Wrapped(l.RpRelationsWayLinkedBody, Theme.TextMuted);

        Layout.Spacer(Theme.GapS);

        Text.WithIcon(Icons.Edit, l.RpRelationsWayFreeTitle, Theme.Gold, Theme.Text);
        Text.Wrapped(l.RpRelationsWayFreeBody, Theme.TextMuted);

        Layout.Spacer(Theme.GapXs);

        if (Btn.Draw(l.RpRelationFreeAdd, BtnTone.Ghost, BtnSize.Small, Icons.Plus,
                     id: "relations_add_free_empty"))
            Plugin.OpenRelationFree();
    }

    // ─── Demandes envoyées ────────────────────────────────────────────────────

    /// <summary>
    /// Demandes envoyées, sans réponse. Une demande acceptée n'y figure plus :
    /// une alerte l'a annoncée, et sa ligne est passée sur la fiche. Un refus,
    /// lui, se lit comme une attente, et rien ici ne doit laisser deviner la
    /// différence.
    /// </summary>
    private void DrawSent(Loc l)
    {
        var pending = Plugin.RelationRequestsSent.Where(r => r.Status != "accepted").ToList();
        if (pending.Count == 0) return;

        using var card = Card.Begin("relations_sent", interactive: false);
        Layout.SectionHeader(l.RpRelationSentTitle, Icons.Send, pending.Count);

        for (var i = 0; i < pending.Count; i++)
        {
            if (i > 0) Layout.Divider(Theme.GapS);

            var request = pending[i];
            DrawWho(request.Character, request.CreatedAt, l);

            Chip.Draw(RpProfileView.RelationLabel(request.Kind, l), ChipTone.Accent);
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(l.RpRelationSentPending, ChipTone.Neutral);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));

            if (Btn.Hold(l.RpRelationCancel, $"relations_cancel_{request.Id}"))
            {
                _error = string.Empty;
                Plugin.CancelRelationRequest(request.Id, ok =>
                {
                    if (!ok) _error = ServerNotice.Explain(Plugin.L.RpRelationFailed);
                });
            }
        }

        Layout.Spacer(Theme.GapS);
        Text.Hint(l.RpRelationsSentHint);
    }

    // ─── Commun ───────────────────────────────────────────────────────────────

    /// <summary>Nom, monde et ancienneté de la demande, sur une ligne.</summary>
    private static void DrawWho(Api.RpRelationPartyDto who, string createdAt, Loc l)
    {
        ImGui.AlignTextToFramePadding();
        Text.Body(who.Name);

        ImGui.SameLine(0f, Theme.S(Theme.GapXs));
        Text.Muted(who.WorldName);

        if (DateTime.TryParse(createdAt, CultureInfo.InvariantCulture,
                              DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                              out var at))
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            Text.Small(EncountersPage.Relative(at, l), Theme.TextFaint);
        }
    }
}

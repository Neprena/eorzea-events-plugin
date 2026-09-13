using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace EorzeaEventsPlugin.Windows;

/// <summary>Ce que la fenêtre fait du lien qu'elle montre.</summary>
internal enum RelationWindowMode
{
    /// <summary>Proposer un lien à quelqu'un.</summary>
    Propose,
    /// <summary>Répondre à une demande reçue.</summary>
    Respond,
    /// <summary>Corriger ou retirer une de ses lignes.</summary>
    Edit,
}

/// <summary>
/// Nouer, accepter ou corriger un lien entre deux fiches RP, en jeu.
///
/// Une relation qui désigne une fiche d'ici ne s'écrit plus d'un seul côté :
/// elle naît d'une demande et ne figure sur les deux fiches qu'une fois
/// acceptée. Les trois gestes partagent cette fenêtre parce qu'ils partagent le
/// sélecteur de type : proposer, accepter et corriger se choisissent dans la
/// même liste de neuf, et la dupliquer trois fois la ferait diverger.
///
/// La note reste un champ de trois lignes plutôt que l'éditeur Markdown : trois
/// cents caractères tiennent dans une phrase, et ouvrir une seconde fenêtre
/// par-dessus celle-ci coûterait plus qu'elle ne rapporte.
/// </summary>
public sealed class RpRelationWindow : ThemedWindow
{
    /// <summary>Plafond de RP_MAX_RELATION_NOTE, appliqué aussi par le serveur.</summary>
    private const int MaxNote = 300;

    private RelationWindowMode _mode = RelationWindowMode.Propose;

    private string _name  = string.Empty;
    private string _world = string.Empty;

    /// <summary>Cible d'une proposition : au moins l'un des deux est renseigné.</summary>
    private string? _characterId;
    private string? _contentIdHash;

    /// <summary>Demande à laquelle on répond.</summary>
    private string? _requestId;

    /// <summary>Ligne que l'on corrige.</summary>
    private string? _relationId;

    /// <summary>La ligne corrigée vient d'une demande acceptée.</summary>
    private bool _linked;

    /// <summary>Rappel de l'appelant après un succès : recharger sa liste.</summary>
    private Action? _onDone;

    private int    _kindIndex;
    private string _note = string.Empty;

    /// <summary>Le type que l'autre a proposé, affiché en mode Respond.</summary>
    private string _proposedKind = string.Empty;

    /// <summary>Un appel est en cours : les boutons se taisent le temps du retour.</summary>
    private bool _busy;

    private string _feedback      = string.Empty;
    private bool   _feedbackError;

    /// <summary>Le geste destructeur est armé : un second clic l'exécute.</summary>
    private bool _armed;

    /// <summary>Personne derrière ce nom ici : le texte libre est proposé.</summary>
    private bool _offerFree;

    /// <summary>La fenêtre doit être posée et dimensionnée à la prochaine image.</summary>
    private bool _placePending;

    /// <summary>Taille d'ouverture, en pixels logiques.</summary>
    private static readonly Vector2 OpenSize = new(480f, 440f);

    public RpRelationWindow()
        : base("##rprelation", ImGuiWindowFlags.NoCollapse)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420f, 320f),
            MaximumSize = new Vector2(900f, 900f),
        };
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (!_placePending) return;
        _placePending = false;

        var viewport = ImGui.GetMainViewport();
        var size     = Theme.S(OpenSize.X, OpenSize.Y);

        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowPos(viewport.WorkPos + (viewport.WorkSize - size) * 0.5f,
                               ImGuiCond.Always);
    }

    /// <summary>
    /// Propose un lien. <paramref name="characterId"/> quand on l'a lu sur une
    /// fiche, sinon le haché du ContentId d'un joueur qu'on a sous les yeux : le
    /// serveur accepte l'un ou l'autre et refuse le reste de la même façon.
    /// </summary>
    public void OpenPropose(string? characterId, string? contentIdHash, string name, string? world)
    {
        Reset();
        _mode          = RelationWindowMode.Propose;
        _characterId   = characterId;
        _contentIdHash = contentIdHash;
        _name          = name;
        _world         = world ?? string.Empty;
        // « Ami » plutôt que « allié », premier de la liste : c'est le lien le
        // plus souvent proposé, et il engage le moins.
        _kindIndex     = IndexOfKind("friend");
        Show();
    }

    /// <summary>
    /// Répond à une demande reçue. Le type est pré-rempli par l'inverse de celui
    /// que l'autre a proposé : mentor appelle élève, le reste se reflète. Ce
    /// n'est qu'un point de départ, chacun nomme son propre lien.
    /// </summary>
    public void OpenRespond(string requestId, string name, string? world, string proposedKind,
                            Action? onAnswered = null)
    {
        Reset();
        _mode         = RelationWindowMode.Respond;
        _requestId    = requestId;
        _name         = name;
        _world        = world ?? string.Empty;
        _proposedKind = proposedKind;
        _onDone       = onAnswered;
        _kindIndex    = IndexOfKind(RpVocab.InverseRelationKind(proposedKind));
        Show();
    }

    /// <summary>Corrige ou retire une de ses lignes.</summary>
    public void OpenEdit(string relationId, string name, string kind, string? note, bool linked,
                         Action? onChanged = null)
    {
        Reset();
        _mode       = RelationWindowMode.Edit;
        _relationId = relationId;
        _name       = name;
        _linked     = linked;
        _note       = note ?? string.Empty;
        _onDone     = onChanged;
        _kindIndex  = IndexOfKind(kind);
        Show();
    }

    /// <summary>
    /// L'index d'un type dans la liste. Une valeur inconnue retombe sur
    /// « autre » plutôt que sur le premier de la liste, qui dirait « allié » à la
    /// place de l'auteur.
    /// </summary>
    private static int IndexOfKind(string kind)
    {
        var index = Array.IndexOf(RpVocab.RelationKinds, kind);
        return index >= 0 ? index : Math.Max(0, Array.IndexOf(RpVocab.RelationKinds, "other"));
    }

    private void Reset()
    {
        _characterId   = null;
        _contentIdHash = null;
        _requestId     = null;
        _relationId    = null;
        _onDone        = null;
        _linked        = false;
        _proposedKind  = string.Empty;
        _note          = string.Empty;
        _world         = string.Empty;
        _kindIndex     = 0;
        _busy          = false;
        _armed         = false;
        _offerFree     = false;
        _feedback      = string.Empty;
        _feedbackError = false;
    }

    /// <summary>
    /// Le titre se pose ici et non dans <c>OnOpen</c> : la fenêtre peut passer
    /// d'un mode à l'autre en restant ouverte, et <c>OnOpen</c> ne serait alors
    /// pas rappelé.
    /// </summary>
    private void Show()
    {
        WindowName    = $"{Title(Plugin.L)}##rprelation";
        _placePending = true;
        IsOpen        = true;
    }

    private string Title(Loc l) => _mode switch
    {
        RelationWindowMode.Respond => string.Format(l.RpRelationRespondTitle, _name),
        RelationWindowMode.Edit    => string.Format(l.RpRelationEditTitle, _name),
        _                          => l.RpRelationPropose,
    };

    public override void Draw()
    {
        var l = Plugin.L;

        Layout.SectionHeader(Title(l), Icons.Friend);

        Text.Body(_world.Length > 0
            ? string.Format(l.RpRelationWith, _name, _world)
            : string.Format(l.RpRelationWithPlain, _name));

        if (_mode == RelationWindowMode.Respond)
            Text.Small(string.Format(l.RpRelationTheirKind, _name,
                                     RpProfileView.RelationLabel(_proposedKind, l)), Theme.Gold);

        Layout.Spacer(Theme.GapS);

        Text.Muted(_mode == RelationWindowMode.Respond
            ? l.RpRelationYourKind
            : l.RpRelationKindQuestion);
        DrawKinds(l);

        Layout.Spacer(Theme.GapS);
        Inputs.Field("##relnote", string.Empty, ref _note, MaxNote,
                     placeholder: l.RpRelationNoteHint, multiline: true, height: 70f);

        if (_mode == RelationWindowMode.Edit && _linked)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small(l.RpRelationBreakWarning, Theme.TextMuted);
        }

        Layout.Divider(Theme.GapS);
        DrawActions(l);

        if (_feedback.Length > 0)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small(_feedback, _feedbackError ? Theme.Danger : Theme.Online);
        }

        if (_offerFree) DrawFreeOffer(l);
    }

    /// <summary>
    /// Les neuf types en pastilles cliquables : la liste est courte et connue
    /// d'avance, un menu déroulant la cacherait derrière un clic de plus.
    /// </summary>
    private void DrawKinds(Loc l)
    {
        var row = FlowRow.Fill(Theme.S(Theme.GapXs));

        for (var i = 0; i < RpVocab.RelationKinds.Length; i++)
        {
            var label = RpProfileView.RelationLabel(RpVocab.RelationKinds[i], l);
            row.Next(Btn.Measure(label, BtnSize.Small));

            if (Btn.Draw(label, i == _kindIndex ? BtnTone.Primary : BtnTone.Ghost, BtnSize.Small,
                         id: $"relkind_{i}"))
                _kindIndex = i;
        }
    }

    private void DrawActions(Loc l)
    {
        switch (_mode)
        {
            case RelationWindowMode.Propose:
                if (Btn.Draw(l.RpRelationSend, BtnTone.Primary, BtnSize.Medium, Icons.Check,
                             disabled: _busy, id: "rel_send"))
                    Send();
                break;

            case RelationWindowMode.Respond:
                if (Btn.Draw(l.RpRelationAccept, BtnTone.Primary, BtnSize.Medium, Icons.Check,
                             disabled: _busy, id: "rel_accept"))
                    Respond("accept");

                ImGui.SameLine(0f, Theme.S(Theme.GapS));

                // Refus en deux temps, comme tout geste sans retour du projet : le
                // premier clic arme, le second exécute, quitter le bouton désarme.
                if (Btn.Draw(_armed ? l.RpRelationDeclineArm : l.RpRelationDecline,
                             _armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Medium,
                             disabled: _busy, id: "rel_decline"))
                {
                    if (_armed) Respond("decline");
                    else        _armed = true;
                }

                if (_armed && !ImGui.IsItemHovered()) _armed = false;
                break;

            case RelationWindowMode.Edit:
                if (Btn.Draw(l.RpRelationSave, BtnTone.Primary, BtnSize.Medium, Icons.Check,
                             disabled: _busy, id: "rel_save"))
                    SaveEdit();

                ImGui.SameLine(0f, Theme.S(Theme.GapS));

                if (Btn.Draw(_armed ? l.RpRelationRemoveArm : l.RpRelationRemove,
                             _armed ? BtnTone.Danger : BtnTone.Ghost, BtnSize.Medium, Icons.Trash,
                             disabled: _busy, id: "rel_remove"))
                {
                    if (_armed) Remove();
                    else        _armed = true;
                }

                if (_armed && !ImGui.IsItemHovered()) _armed = false;
                break;
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Medium, id: "rel_close"))
            IsOpen = false;
    }

    /// <summary>
    /// Aucune fiche derrière ce nom : la relation peut quand même s'écrire, en
    /// texte libre, sur sa seule fiche. La bascule est proposée et non faite
    /// d'office : cette ligne sera publique, et son auteur doit la voir partir.
    /// </summary>
    private void DrawFreeOffer(Loc l)
    {
        Layout.Spacer(Theme.GapXs);
        Text.Small(l.RpRelationFreeOffer, Theme.TextMuted);
        Layout.Spacer(Theme.GapXs);

        if (Btn.Draw(l.RpRelationFreeAdd, BtnTone.Secondary, BtnSize.Small, Icons.Plus,
                     disabled: _busy, id: "rel_free"))
            AddFree();
    }

    private void Send()
    {
        _busy      = true;
        _offerFree = false;
        _feedback  = string.Empty;

        var body = new CreateRelationRequestBody
        {
            CharacterId   = _characterId,
            ContentIdHash = _contentIdHash,
            Kind          = RpVocab.RelationKinds[_kindIndex],
            Note          = Trimmed(_note),
        };

        Task.Run(async () =>
        {
            var result = await Plugin.Api.CreateRelationRequestAsync(body);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _busy = false;
                ApplyRequestResult(result);
                Plugin.RequestRelationRequestsRefresh();
            });
        });
    }

    private void ApplyRequestResult(ApiClient.RelationRequestResult result)
    {
        var l = Plugin.L;

        switch (result)
        {
            case ApiClient.RelationRequestResult.Pending:
                Say(l.RpRelationSent, error: false);
                break;

            case ApiClient.RelationRequestResult.AcceptedNow:
                Say(l.RpRelationSelfAccepted, error: false);
                _onDone?.Invoke();
                break;

            case ApiClient.RelationRequestResult.NoTarget:
                // Le serveur ne dit pas si la personne a un compte : elle n'a pas
                // de fiche visible en jeu, c'est tout ce qui se sait.
                Say(l.RpRelationNoTarget, error: true);
                _offerFree = true;
                break;

            case ApiClient.RelationRequestResult.AlreadyRequested:
                Say(l.RpRelationAlreadySent, error: true);
                break;

            case ApiClient.RelationRequestResult.AlreadyLinked:
                Say(l.RpRelationAlreadyLinked, error: true);
                break;

            case ApiClient.RelationRequestResult.ProfileFull:
                Say(l.RpRelationFull, error: true);
                break;

            case ApiClient.RelationRequestResult.Self:
                Say(l.RpRelationSelf, error: true);
                break;

            case ApiClient.RelationRequestResult.NoProfile:
                Say(l.RpRelationNeedProfile, error: true);
                break;

            case ApiClient.RelationRequestResult.RateLimited:
                Say(l.RpRelationTooMany, error: true);
                break;

            default:
                Say(l.RpRelationFailed, error: true);
                break;
        }
    }

    private void Respond(string action)
    {
        if (_requestId is not { Length: > 0 } requestId) return;

        _busy     = true;
        _armed    = false;
        _feedback = string.Empty;

        var accepting = action == "accept";
        var body = new RespondRelationRequestBody
        {
            Action = action,
            Kind   = accepting ? RpVocab.RelationKinds[_kindIndex] : null,
            Note   = accepting ? Trimmed(_note) : null,
        };

        Task.Run(async () =>
        {
            var result = await Plugin.Api.RespondRelationRequestAsync(requestId, body);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _busy = false;
                var l = Plugin.L;

                switch (result)
                {
                    case ApiClient.RelationRespondResult.Accepted:
                        Say(l.RpRelationAccepted, error: false);
                        _onDone?.Invoke();
                        break;
                    case ApiClient.RelationRespondResult.Declined:
                        Say(l.RpRelationDeclined, error: false);
                        _onDone?.Invoke();
                        IsOpen = false;
                        break;
                    case ApiClient.RelationRespondResult.NotFound:
                        Say(l.RpRelationGone, error: true);
                        break;
                    case ApiClient.RelationRespondResult.AlreadyAnswered:
                        Say(l.RpRelationAnswered, error: true);
                        break;
                    case ApiClient.RelationRespondResult.AlreadyLinked:
                        Say(l.RpRelationAlreadyLinked, error: true);
                        break;
                    case ApiClient.RelationRespondResult.ProfileFull:
                        Say(l.RpRelationFull, error: true);
                        break;
                    default:
                        Say(l.RpRelationFailed, error: true);
                        break;
                }

                // La demande a quitté la file, dans un sens ou dans l'autre : la
                // pastille doit le montrer sans attendre la minute.
                Plugin.RequestRelationRequestsRefresh();
            });
        });
    }

    private void SaveEdit()
    {
        if (_relationId is not { Length: > 0 } relationId) return;

        _busy     = true;
        _feedback = string.Empty;

        var body = new UpdateRelationBody
        {
            Kind = RpVocab.RelationKinds[_kindIndex],
            // Vidé, le champ efface la note : les valeurs nulles étant omises à
            // la sérialisation, la chaîne vide est le seul moyen de le dire.
            Note = _note.Trim(),
        };

        Task.Run(async () =>
        {
            var ok = await Plugin.Api.UpdateRelationAsync(relationId, body);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _busy = false;
                var l = Plugin.L;

                if (!ok)
                {
                    Say(l.RpRelationFailed, error: true);
                    return;
                }

                Say(l.RpRelationSaved, error: false);
                _onDone?.Invoke();
            });
        });
    }

    private void Remove()
    {
        if (_relationId is not { Length: > 0 } relationId) return;

        _busy     = true;
        _armed    = false;
        _feedback = string.Empty;

        Task.Run(async () =>
        {
            var ok = await Plugin.Api.RemoveRelationAsync(relationId);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _busy = false;

                if (!ok)
                {
                    Say(Plugin.L.RpRelationFailed, error: true);
                    return;
                }

                // La ligne n'existe plus : il n'y a plus rien à montrer ici, et la
                // liste qui l'affichait vient d'être prévenue.
                _onDone?.Invoke();
                Plugin.RequestRelationRequestsRefresh();
                IsOpen = false;
            });
        });
    }

    private void AddFree()
    {
        _busy      = true;
        _offerFree = false;
        _feedback  = string.Empty;

        var body = new FreeRelationBody
        {
            TargetName = _name,
            Kind       = RpVocab.RelationKinds[_kindIndex],
            Note       = Trimmed(_note),
        };

        Task.Run(async () =>
        {
            var result = await Plugin.Api.AddFreeRelationAsync(body);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _busy = false;
                var l = Plugin.L;

                switch (result)
                {
                    case ApiClient.FreeRelationResult.Added:
                        Say(l.RpRelationFreeAdded, error: false);
                        _onDone?.Invoke();
                        break;
                    case ApiClient.FreeRelationResult.ProfileFull:
                        Say(l.RpRelationFull, error: true);
                        break;
                    case ApiClient.FreeRelationResult.NoProfile:
                        Say(l.RpRelationNeedProfile, error: true);
                        break;
                    case ApiClient.FreeRelationResult.RateLimited:
                        Say(l.RpRelationTooMany, error: true);
                        break;
                    default:
                        Say(l.RpRelationFailed, error: true);
                        break;
                }
            });
        });
    }

    private void Say(string message, bool error)
    {
        _feedback      = message;
        _feedbackError = error;
    }

    private static string? Trimmed(string text)
        => text.Trim().Length > 0 ? text.Trim() : null;
}

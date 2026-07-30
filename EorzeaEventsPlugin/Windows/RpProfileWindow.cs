using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Ui.Shell;
using EorzeaEventsPlugin.Api;
using System.Numerics;

using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Fiche RP d'un autre joueur, en lecture seule, et aperçu de la sienne.
///
/// Cette fenêtre portait aussi un assistant de première configuration, remplacé
/// depuis par la page « Mon profil RP » de la coque. Il n'était plus atteignable,
/// mais son enregistrement construisait une requête vierge dont les booléens
/// partaient à leurs valeurs par défaut : le rouvrir aurait republié une fiche
/// masquée, réactivé la page web et effacé le marquage sensible. Supprimé plutôt
/// que laissé en embuscade.
/// </summary>
public class RpProfileWindow : ThemedWindow
{
    // Viewer state
    private RpAvailabilityEntryDto? _viewTarget;

    /// <summary>Fiche complète récupérée par le réseau, null tant qu'elle n'est pas là.</summary>
    private RpProfileDto? _viewFull;

    /// <summary>Personnage consulté. Sert à ignorer une réponse devenue obsolète.</summary>
    private string? _viewCharacterId;

    /// <summary>
    /// Vrai quand on consulte sa propre fiche telle que les autres la voient.
    /// Change deux choses : un bandeau le rappelle, et l'absence de réponse est
    /// expliquée au lieu d'afficher « ce joueur n'a pas de fiche ».
    /// </summary>
    private bool _isPreview;

    /// <summary>La requête a répondu, sans fiche. Distingue « rien » de « pas encore ».</summary>
    private bool _viewFetchEmpty;

    /// <summary>
    /// En aperçu, vue simulée : publique par défaut, ou celle d'un ami. Régler une
    /// section sur « Mes amis RP » sans pouvoir en constater l'effet reviendrait à
    /// demander de la confiance à l'aveugle.
    /// </summary>
    private bool _previewAsFriend;

    public RpProfileWindow()
        : base("##rpprofile")
    {
        // Redimensionnable, contrairement au wizard d'origine : une fiche
        // complète porte une biographie et des relations qui ne tiennent pas
        // dans une hauteur figée.
        // Minimum relevé avec le portrait : à 600 de large, la colonne de texte de
        // l'en-tête retrouve la place qu'elle avait à 500 avec une vignette de 63,
        // et la hauteur absorbe un en-tête passé de 106 à 222 px.
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 620),
            MaximumSize = new Vector2(800, 960),
        };
    }

    /// <summary>
    /// Sa propre fiche, telle que le serveur la sert aux autres joueurs.
    ///
    /// L'aperçu ne simule pas la redaction côté plugin : il interroge la même
    /// route publique que les autres clients. C'est le seul moyen d'être sûr que
    /// ce qu'on montre correspond à ce qui sort réellement du serveur, sans qu'une
    /// logique locale puisse dériver de la sienne.
    ///
    /// La fiche locale ne sert donc pas d'amorce : elle est complète, et
    /// l'afficher ferait apparaître une fraction de seconde ce que l'aperçu est
    /// précisément censé masquer.
    /// </summary>
    public void OpenPreview(string characterId, string characterName, string? server)
    {
        _isPreview  = true;
        _viewTarget = new RpAvailabilityEntryDto
        {
            CharacterName = characterName,
            Server        = server ?? string.Empty,
        };
        _viewFull        = null;
        _viewFetchEmpty  = false;
        _viewCharacterId = characterId;
        _previewAsFriend = false;

        WindowName = $"{Plugin.L.RpProfilePreviewTitle}##rpprofile";
        IsOpen = true;

        FetchFullProfile(characterId);
    }

    /// <summary>Open the viewer to display another player's profile.</summary>
    public void OpenViewer(RpAvailabilityEntryDto entry)
    {
        _isPreview  = false;
        _viewTarget = entry;
        _viewFull   = null;
        _viewFetchEmpty  = false;
        _viewCharacterId = entry.Profile?.CharacterId;

        WindowName = $"{Plugin.L.RpProfileViewTitle} : {Glyphs.Safe(entry.CharacterName)}##rpprofile";
        IsOpen = true;

        // La liste des disponibilités ne porte qu'un extrait de la fiche : on
        // complète en tâche de fond dès l'ouverture.
        if (_viewCharacterId is { Length: > 0 } characterId) FetchFullProfile(characterId);
    }

    public override void Draw() => DrawViewer();

    // ── Viewer ────────────────────────────────────────────────────────────────

    private void DrawViewer()
    {
        var l = Plugin.L;
        var entry = _viewTarget;
        if (entry == null) { IsOpen = false; return; }

        // La fiche complète remplace celle de la liste dès qu'elle est arrivée.
        // En aperçu il n'y a pas d'amorce locale : on attend la réponse du serveur.
        var profile = _viewFull ?? (_isPreview ? null : entry.Profile);

        var footer = Layout.FooterHeight(Theme.GapS);
        using (var body = ImRaii.Child("##rpviewbody", new Vector2(-1f, -footer)))
        {
            if (body)
            {
                if (_isPreview)
                {
                    Feedback.Alert(Theme.Idle, Icons.Show, l.RpProfilePreviewTitle,
                                   l.RpProfilePreviewHint);
                }

                if (_isPreview) DrawPreviewTabs(l);

            if (_isPreview && profile == null)
                {
                    // Le serveur a répondu sans fiche : c'est un refus, pas une
                    // attente. En aperçu, cela veut dire que la fiche n'est pas
                    // visible en jeu, et le dire vaut mieux qu'un écran vide.
                    if (_viewFetchEmpty)
                        Feedback.EmptyState(Icons.Hide, l.RpProfilePreviewHidden);
                    else
                        Feedback.SkeletonCards(2);
                }
                else
                {
                    RpProfileView.Draw(profile, entry.CharacterName, entry.Server, l);
                }
            }
        }

        Layout.Divider(Theme.GapS);

        if (Btn.Draw(l.Cancel, BtnTone.Secondary, BtnSize.Medium, id: "rpview_close"))
            IsOpen = false;

        // Le rebond vers le site donne accès à la fiche telle que la voient les
        // joueurs sans le plugin. Conditionné à l'existence de la page : la
        // visibilité en jeu et la page web sont deux consentements distincts, et
        // proposer le lien sans vérifier mènerait à un 404.
        if (profile?.HasWebPage == true && _viewCharacterId is { Length: > 0 } characterId)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.RpProfileViewOnSite, BtnTone.Ghost, BtnSize.Medium, Icons.External,
                         id: "rpview_site"))
                OpenSite($"/rp/{characterId}");
        }

        // Pas d'ajout en aperçu : on ne s'ouvre pas sa propre fiche. Et l'ajout
        // n'ouvre que la nôtre, ce que dit l'infobulle : cette fenêtre-ci ne
        // montrera pas davantage après coup.
        if (!_isPreview && _viewCharacterId is { Length: > 0 } friendId)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));

            if (Plugin.IsFriend(friendId))
            {
                Chip.Draw(l.RpFriendChip, ChipTone.Accent, Icons.Friend);
            }
            else if (Btn.Draw(l.RpFriendAdd, BtnTone.Ghost, BtnSize.Medium, Icons.FriendAdd,
                              tooltip: l.RpFriendAddHint, id: "rpview_friend"))
            {
                Plugin.AddFriend(friendId, 0, entry.CharacterName);
            }
        }
    }

    /// <summary>
    /// Complète la fiche affichée avec les champs absents de la liste des
    /// disponibilités : biographie, relations, traits physiques, appartenances.
    ///
    /// Même logique que <c>RpProfilePage.Load</c> : on montre d'abord ce qu'on a
    /// déjà, le réseau ne fait que compléter. Un échec laisse donc la fiche
    /// partielle à l'écran plutôt qu'un écran vide.
    /// </summary>
    private void DrawPreviewTabs(Loc l)
    {
        var asFriend = _previewAsFriend;

        if (Btn.Draw(l.RpProfilePreviewAsPublic,
                     asFriend ? BtnTone.Ghost : BtnTone.Primary,
                     BtnSize.Medium, id: "rpview_as_public") && asFriend)
        {
            _previewAsFriend = false;
            if (_viewCharacterId is { Length: > 0 } id) FetchFullProfile(id);
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));

        if (Btn.Draw(l.RpProfilePreviewAsFriend,
                     asFriend ? BtnTone.Primary : BtnTone.Ghost,
                     BtnSize.Medium, Icons.Friend, id: "rpview_as_friend") && !asFriend)
        {
            _previewAsFriend = true;
            if (_viewCharacterId is { Length: > 0 } id) FetchFullProfile(id);
        }

        Layout.Spacer(Theme.GapS);
    }

    private void FetchFullProfile(string characterId)
    {
        _viewFull = null;
        Task.Run(async () =>
        {
            var full = await Plugin.Api.GetPublicRpProfileAsync(characterId, _previewAsFriend);

            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                // La fenêtre a pu être rouverte sur un autre personnage entre-temps.
                if (_viewCharacterId != characterId) return;

                if (full != null) _viewFull = full;
                // Une réponse vide se distingue d'une attente : sur un aperçu, elle
                // signifie que la fiche n'est pas visible, ce qu'il faut expliquer.
                else _viewFetchEmpty = true;
            });
        });
    }

    private static void OpenSite(string path) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl + path)
            { UseShellExecute = true });
}

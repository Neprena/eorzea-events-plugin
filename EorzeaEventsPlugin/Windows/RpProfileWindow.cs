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
/// Dual-mode window:
///   - Wizard mode: first-time setup of the user's own RP profile
///   - Viewer mode: display another player's RP profile (read-only)
/// </summary>
public class RpProfileWindow : ThemedWindow
{
    private enum Mode { Wizard, Viewer }

    private readonly Configuration _config;
    private Mode _mode = Mode.Wizard;

    // Wizard state
    private int  _levelIdx    = 1; // 0=beginner 1=casual 2=confirmed
    private bool _langFr      = true;
    private bool _langEn      = false;
    private int  _approachIdx = 0; // 0=come_to_me 1=i_approach 2=either
    private string _status    = string.Empty;
    private bool _saving      = false;

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

    private static readonly string[] LevelKeys    = ["beginner", "casual", "confirmed"];
    private static readonly string[] ApproachKeys = ["come_to_me", "i_approach", "either"];

    public RpProfileWindow(Configuration config)
        : base("##rpprofile")
    {
        // Redimensionnable, contrairement au wizard d'origine : une fiche
        // complète porte une biographie et des relations qui ne tiennent pas
        // dans une hauteur figée.
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 490),
            MaximumSize = new Vector2(760, 900),
        };
        _config = config;
    }

    /// <summary>Open the wizard to set up the user's own profile.</summary>
    public void OpenWizard()
    {
        _mode = Mode.Wizard;
        _status = string.Empty;
        LoadFromConfig();
        WindowName = $"{Plugin.L.RpProfileWizardTitle}##rpprofile";
        IsOpen = true;
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
        _mode = Mode.Viewer;
        _isPreview  = true;
        _viewTarget = new RpAvailabilityEntryDto
        {
            CharacterName = characterName,
            Server        = server ?? string.Empty,
        };
        _viewFull        = null;
        _viewFetchEmpty  = false;
        _viewCharacterId = characterId;

        WindowName = $"{Plugin.L.RpProfilePreviewTitle}##rpprofile";
        IsOpen = true;

        FetchFullProfile(characterId);
    }

    /// <summary>Open the viewer to display another player's profile.</summary>
    public void OpenViewer(RpAvailabilityEntryDto entry)
    {
        _mode = Mode.Viewer;
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

    private void LoadFromConfig()
    {
        _levelIdx    = Array.IndexOf(LevelKeys,    _config.RpProfileLevel       ?? "casual");
        _approachIdx = Array.IndexOf(ApproachKeys, _config.RpProfileApproachMode ?? "come_to_me");
        if (_levelIdx    < 0) _levelIdx    = 1;
        if (_approachIdx < 0) _approachIdx = 0;
        var langs = _config.RpProfileLanguages ?? "[\"fr\"]";
        _langFr = langs.Contains("\"fr\"");
        _langEn = langs.Contains("\"en\"");
        if (!_langFr && !_langEn) _langFr = true;
    }

    public override void Draw()
    {
        if (_mode == Mode.Viewer)
            DrawViewer();
        else
            DrawWizard();
    }

    // ── Wizard ────────────────────────────────────────────────────────────────

    private void DrawWizard()
    {
        var l = Plugin.L;

        ImGui.TextWrapped(l.RpProfileWizardIntro);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── RP Level ──
        ImGui.TextUnformatted(l.RpProfileLevel);
        ImGui.Spacing();
        DrawRadio(l.RpProfileLevelBeginner,  ref _levelIdx, 0);
        DrawRadio(l.RpProfileLevelCasual,    ref _levelIdx, 1);
        DrawRadio(l.RpProfileLevelConfirmed, ref _levelIdx, 2);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Approach mode ──
        ImGui.TextUnformatted(l.RpProfileApproach);
        ImGui.Spacing();
        DrawRadioWithHint(l.RpProfileApproachCome,   l.RpProfileApproachComeHint,   ref _approachIdx, 0);
        DrawRadioWithHint(l.RpProfileApproachIGo,    l.RpProfileApproachIGoHint,    ref _approachIdx, 1);
        DrawRadioWithHint(l.RpProfileApproachEither, l.RpProfileApproachEitherHint, ref _approachIdx, 2);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Languages ──
        ImGui.TextUnformatted(l.RpProfileLanguages);
        ImGui.Spacing();
        ImGui.Checkbox("Français", ref _langFr);
        ImGui.SameLine();
        ImGui.Checkbox("English",  ref _langEn);
        if (!_langFr && !_langEn) _langFr = true;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (!string.IsNullOrEmpty(_status))
        {
            ImGui.TextUnformatted(_status);
            ImGui.SameLine();
        }

        var btnLabel = _saving ? l.Processing : l.Save;
        if (ImGui.Button(btnLabel) && !_saving)
            SaveProfileAsync();

        ImGui.SameLine();
        if (ImGui.Button(l.Cancel))
            IsOpen = false;
    }

    private static void DrawRadio(string label, ref int current, int value)
    {
        var active = current == value;
        if (ImGui.RadioButton(label, active))
            current = value;
    }

    private static void DrawRadioWithHint(string label, string hint, ref int current, int value)
    {
        var active = current == value;
        if (ImGui.RadioButton(label, active))
            current = value;
        ImGui.Indent(22f);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 0.45f));
        ImGui.TextUnformatted(hint);
        ImGui.PopStyleColor();
        ImGui.Unindent(22f);
    }

    private void SaveProfileAsync()
    {
        _saving = true;
        _status = string.Empty;

        var langs = new List<string>();
        if (_langFr) langs.Add("fr");
        if (_langEn) langs.Add("en");

        var req = new SaveRpProfileRequest
        {
            RpLevel      = LevelKeys[_levelIdx],
            ApproachMode = ApproachKeys[_approachIdx],
            Languages    = [.. langs],
        };

        // Cache local immédiat
        _config.RpProfileLevel        = req.RpLevel;
        _config.RpProfileApproachMode = req.ApproachMode;
        _config.RpProfileLanguages    = System.Text.Json.JsonSerializer.Serialize(langs);
        _config.RpProfileSetupDone    = true;
        _config.Save();

        Task.Run(async () =>
        {
            var result = await Plugin.Api.SaveRpProfileAsync(req);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _saving = false;
                _status = result != null ? Plugin.L.RpProfileSaved : Plugin.L.RpProfileError;
            });
            if (result != null)
            {
                await Task.Delay(1200);
                await Plugin.Framework.RunOnFrameworkThread(() => IsOpen = false);
            }
        });
    }

    // ── Viewer ────────────────────────────────────────────────────────────────

    private void DrawViewer()
    {
        var l = Plugin.L;
        var entry = _viewTarget;
        if (entry == null) { IsOpen = false; return; }

        // La fiche complète remplace celle de la liste dès qu'elle est arrivée.
        // En aperçu il n'y a pas d'amorce locale : on attend la réponse du serveur.
        var profile = _viewFull ?? (_isPreview ? null : entry.Profile);

        var footer = ImGui.GetFrameHeightWithSpacing() + Theme.S(Theme.GapS * 2f + 6f);
        using (var body = ImRaii.Child("##rpviewbody", new Vector2(-1f, -footer)))
        {
            if (body)
            {
                if (_isPreview)
                {
                    Feedback.Alert(Theme.Idle, Icons.Show, l.RpProfilePreviewTitle,
                                   l.RpProfilePreviewHint);
                }

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
    }

    /// <summary>
    /// Complète la fiche affichée avec les champs absents de la liste des
    /// disponibilités : biographie, relations, traits physiques, appartenances.
    ///
    /// Même logique que <c>RpProfilePage.Load</c> : on montre d'abord ce qu'on a
    /// déjà, le réseau ne fait que compléter. Un échec laisse donc la fiche
    /// partielle à l'écran plutôt qu'un écran vide.
    /// </summary>
    private void FetchFullProfile(string characterId)
    {
        _viewFull = null;
        Task.Run(async () =>
        {
            var full = await Plugin.Api.GetPublicRpProfileAsync(characterId);

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

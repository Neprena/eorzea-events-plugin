using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Création d'une fiche RP, pas à pas.
///
/// Une fiche vierge de plus dans le sélecteur ne dit pas quoi en faire : on se
/// retrouve devant une dizaine de sections vides sans savoir laquelle compte.
/// L'assistant pose les quelques questions qui rendent une fiche utile, dans
/// l'ordre où on y répond, et laisse le reste à l'édition ordinaire.
///
/// Rien n'est créé tant que la dernière étape n'est pas validée : abandonner en
/// route ne laisse aucune trace. Le serveur exige au moins une langue, elle est
/// donc cochée d'office et ne peut pas être entièrement décochée.
/// </summary>
public sealed class RpProfileWizardWindow : ThemedWindow
{
    private const int StepCount = 4;

    private int    _step;
    private bool   _busy;
    private string _error = string.Empty;

    /// <summary>Appelé avec l'identifiant de la fiche créée, sur le thread de jeu.</summary>
    private Action<string>? _created;

    // Étape 1, l'identité.
    private string _rpName   = string.Empty;
    private int    _race;
    private string _pronouns = string.Empty;
    private string _age      = string.Empty;

    // Étape 2, la façon de jouer.
    private int  _level    = 1;   // « occasionnel », le milieu
    private int  _approach = 2;   // « les deux »
    private bool _langFr   = true;
    private bool _langEn;

    // Étape 3, les thèmes.
    private readonly List<string> _themes      = [];
    private readonly List<string> _avoidThemes = [];

    // Étape 4, la visibilité.
    private bool _isPublic = true;
    private bool _nsfw;

    /// <summary>
    /// Taille d'ouverture, en pixels logiques. Assez large pour que les
    /// pastilles de thèmes tiennent sur deux ou trois lignes plutôt que huit,
    /// et assez haute pour que l'étape la plus fournie ne défile pas.
    /// </summary>
    private static readonly Vector2 OpenSize = new(760f, 620f);

    /// <summary>
    /// La fenêtre doit être posée et dimensionnée à la prochaine image.
    ///
    /// Le geste ne vaut qu'à l'ouverture : le refaire à chaque image empêcherait
    /// de la déplacer ou de la redimensionner.
    /// </summary>
    private bool _placePending;

    public RpProfileWizardWindow()
        : base("##rpwizard", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520f, 420f),
            MaximumSize = new Vector2(1400f, 1100f),
        };
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (!_placePending) return;
        _placePending = false;

        // Centrée sur l'espace utile de l'écran, et non sur l'écran entier :
        // la barre des tâches et les marges du jeu en font partie.
        var viewport = ImGui.GetMainViewport();
        var size     = Theme.S(OpenSize.X, OpenSize.Y);

        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowPos(viewport.WorkPos + (viewport.WorkSize - size) * 0.5f,
                               ImGuiCond.Always);
    }

    /// <summary>Ouvre l'assistant sur une fiche neuve, tous champs remis à zéro.</summary>
    public void Open(Action<string> created)
    {
        _created = created;
        _step    = 0;
        _busy    = false;
        _error   = string.Empty;

        _rpName   = string.Empty;
        _race     = 0;
        _pronouns = string.Empty;
        _age      = string.Empty;
        _level    = 1;
        _approach = 2;
        _langFr   = true;
        _langEn   = false;
        _themes.Clear();
        _avoidThemes.Clear();
        _isPublic = true;
        _nsfw     = false;

        _placePending = true;
        IsOpen        = true;
    }

    public override void Draw()
    {
        var l = Plugin.L;

        Layout.SectionHeader(l.RpWizardTitle, Icons.Plus);
        Text.Small(string.Format(l.RpWizardStep, _step + 1, StepCount), Theme.TextMuted);
        Layout.Spacer(Theme.GapS);

        var avail  = ImGui.GetContentRegionAvail();
        var height = Math.Max(Theme.S(140f), avail.Y - Theme.S(52f));

        using (var body = ImRaii.Child("##rpwizardbody", new Vector2(-1f, height)))
        {
            if (body)
            {
                switch (_step)
                {
                    case 0:  DrawIdentity(l);   break;
                    case 1:  DrawPlay(l);       break;
                    case 2:  DrawThemes(l);     break;
                    default: DrawVisibility(l); break;
                }
            }
        }

        Layout.Divider(Theme.GapXs);
        DrawFooter(l);
    }

    private void DrawIdentity(Loc l)
    {
        Text.Small(l.RpWizardIdentityHint, Theme.TextMuted);
        Layout.Spacer(Theme.GapS);

        Inputs.Field("##wname", l.RpProfileRpName, ref _rpName, 80, help: l.RpWizardNameHint);

        Inputs.Select("##wrace", l.RpProfileRace, ref _race,
                      [.. RpVocab.Races.Select(k => k.Length == 0
                          ? l.RpProfileUnset
                          : RpProfileView.RaceLabel(k, l))]);

        Inputs.Field("##wpronouns", l.RpProfilePronouns, ref _pronouns, 30);
        Inputs.Field("##wage", l.RpProfileAge, ref _age, 30);
    }

    private void DrawPlay(Loc l)
    {
        Text.Small(l.RpWizardPlayHint, Theme.TextMuted);
        Layout.Spacer(Theme.GapS);

        Inputs.Select("##wlevel", l.RpProfileLevel, ref _level,
                      [.. RpVocab.Levels.Select(k => RpProfileView.LevelLabel(k, l))]);

        Inputs.Select("##wapproach", l.RpProfileApproach, ref _approach,
                      [.. RpVocab.Approaches.Select(k => RpProfileView.ApproachLabel(k, l))]);

        Layout.Spacer(Theme.GapS);
        Text.Small(l.RpProfileLanguages, Theme.TextMuted);

        var fr = _langFr;
        if (Inputs.ToggleRow("Français", ref fr)) _langFr = fr;
        var en = _langEn;
        if (Inputs.ToggleRow("English", ref en)) _langEn = en;

        // Le serveur en exige une : la rétablir ici évite un refus que rien à
        // l'écran n'expliquerait.
        if (!_langFr && !_langEn) _langFr = true;
    }

    private void DrawThemes(Loc l)
    {
        Text.Small(l.RpWizardThemesHint, Theme.TextMuted);
        Layout.Spacer(Theme.GapS);

        DrawThemeRow(l.RpProfileThemes, _themes, _avoidThemes, "w_want", l);
        Layout.Spacer(Theme.GapS);
        DrawThemeRow(l.RpProfileAvoidThemes, _avoidThemes, _themes, "w_avoid", l);
    }

    private static void DrawThemeRow(string title, List<string> list, List<string> other,
                                     string id, Loc l)
    {
        Text.Small($"{title} ({list.Count}/{RpVocab.MaxThemes})", Theme.TextMuted);
        Layout.Spacer(Theme.GapXs);

        var limit = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        var gap   = Theme.S(Theme.GapXs);
        var first = true;

        foreach (var key in RpVocab.Themes)
        {
            var label  = RpProfileView.ThemeLabel(key, l);
            var active = list.Contains(key);

            if (!first && ImGui.GetCursorPosX() + gap + Btn.Measure(label) <= limit)
                ImGui.SameLine(0f, gap);
            first = false;

            if (!Btn.Draw(label, active ? BtnTone.Primary : BtnTone.Ghost, BtnSize.Small,
                          id: $"theme_{id}_{key}"))
                continue;

            if (active) list.Remove(key);
            else if (list.Count < RpVocab.MaxThemes)
            {
                list.Add(key);
                other.Remove(key);
            }
        }
    }

    private void DrawVisibility(Loc l)
    {
        Text.Small(l.RpWizardVisibilityHint, Theme.TextMuted);
        Layout.Spacer(Theme.GapS);

        var visible = _isPublic;
        if (Inputs.ToggleRow(l.RpProfileVisInGame, ref visible)) _isPublic = visible;
        Text.Small(l.RpWizardVisibleHint, Theme.TextMuted);

        Layout.Spacer(Theme.GapS);
        var nsfw = _nsfw;
        if (Inputs.ToggleRow(l.RpProfileNsfw, ref nsfw)) _nsfw = nsfw;
        Text.Small(l.RpWizardNsfwHint, Theme.TextMuted);

        Layout.Spacer(Theme.GapM);
        Text.Small(l.RpWizardRest, Theme.TextMuted);
    }

    private void DrawFooter(Loc l)
    {
        using (ImRaii.Disabled(_busy))
        {
            if (_step > 0
                && Btn.Draw(l.RpWizardBack, BtnTone.Ghost, BtnSize.Medium, id: "wiz_back"))
                _step--;

            if (_step > 0) ImGui.SameLine(0f, Theme.S(Theme.GapS));

            if (_step < StepCount - 1)
            {
                if (Btn.Draw(l.RpWizardNext, BtnTone.Primary, BtnSize.Medium, id: "wiz_next"))
                    _step++;
            }
            else if (Btn.Draw(_busy ? l.Processing : l.RpWizardCreate, BtnTone.Primary,
                              BtnSize.Medium, Icons.Check, id: "wiz_create"))
                Create();

            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.RpWizardCancel, BtnTone.Ghost, BtnSize.Medium, id: "wiz_cancel"))
                IsOpen = false;
        }

        if (_error.Length > 0)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            ImGui.AlignTextToFramePadding();
            Text.Small(_error, Theme.Danger);
        }
    }

    /// <summary>
    /// Crée la fiche, puis y écrit les réponses.
    ///
    /// Deux appels et non un : la création dit au serveur « une fiche de plus »,
    /// l'enregistrement dit ce qu'elle contient. Les enchaîner ici plutôt que
    /// d'inventer une route de création complète évite un troisième chemin
    /// d'écriture, avec sa propre validation à tenir à jour.
    /// </summary>
    private void Create()
    {
        _busy  = true;
        _error = string.Empty;

        var request = new SaveRpProfileRequest
        {
            RpName       = _rpName.Trim(),
            Race         = RpVocab.Races[_race],
            Pronouns     = _pronouns.Trim(),
            Age          = _age.Trim(),
            RpLevel      = RpVocab.Levels[_level],
            ApproachMode = RpVocab.Approaches[_approach],
            Languages    = [.. Languages()],
            Themes       = [.. _themes],
            AvoidThemes  = [.. _avoidThemes],
            Nsfw         = _nsfw,
            IsPublic     = _isPublic,
        };

        _ = Task.Run(async () =>
        {
            var (result, profileId) = await Plugin.Api.CreateRpProfileAsync();

            if (result != RpSlotResult.Ok || profileId is not { Length: > 0 })
            {
                await Plugin.Framework.RunOnFrameworkThread(() =>
                {
                    _busy  = false;
                    _error = result == RpSlotResult.Quota
                        ? Plugin.L.RpProfileSlotErrQuota
                        : Plugin.L.RpProfileSlotErr;
                });
                return;
            }

            request.ProfileId = profileId;
            var saved = await Plugin.Api.SaveRpProfileAsync(request);

            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                _busy = false;

                // La fiche existe même si l'enregistrement a échoué : la signaler
                // quand même vaut mieux que de laisser une fiche vide orpheline
                // dont personne ne saurait qu'elle a été créée.
                _created?.Invoke(profileId);
                IsOpen = false;

                if (saved == null)
                    Plugin.Log.Warning("[EorzeaEvents] Fiche créée mais réponses non enregistrées.");
            });
        });
    }

    private IEnumerable<string> Languages()
    {
        if (_langFr) yield return "fr";
        if (_langEn) yield return "en";
    }
}

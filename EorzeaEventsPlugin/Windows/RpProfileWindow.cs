using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Dual-mode window:
///   - Wizard mode: first-time setup of the user's own RP profile
///   - Viewer mode: display another player's RP profile (read-only)
/// </summary>
public class RpProfileWindow : Window
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

    private static readonly string[] LevelKeys    = ["beginner", "casual", "confirmed"];
    private static readonly string[] ApproachKeys = ["come_to_me", "i_approach", "either"];

    public RpProfileWindow(Configuration config)
        : base("##rpprofile", ImGuiWindowFlags.NoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 490),
            MaximumSize = new Vector2(500, 490),
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

    /// <summary>Open the viewer to display another player's profile.</summary>
    public void OpenViewer(RpAvailabilityEntryDto entry)
    {
        _mode = Mode.Viewer;
        _viewTarget = entry;
        WindowName = $"{Plugin.L.RpProfileViewTitle} — {entry.CharacterName}##rpprofile";
        IsOpen = true;
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

        ImGui.TextUnformatted(entry.CharacterName);
        if (!string.IsNullOrEmpty(entry.Server))
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"  [{entry.Server}]");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var profile = entry.Profile;
        if (profile == null)
        {
            ImGui.TextDisabled("(aucun profil configuré)");
        }
        else
        {
            DrawRow(l.RpProfileLevel,    LevelLabel(l, profile.RpLevel));
            DrawRow(l.RpProfileApproach, ApproachLabel(l, profile.ApproachMode));
            DrawRow(l.RpProfileLanguages,
                string.Join(" / ", profile.Languages.Select(lang => lang == "fr" ? "Français" : "English")));

            if (profile.Themes.Length > 0)
            {
                var themes = string.Join(" · ", profile.Themes.Select(t => t switch
                {
                    "tavern"    => "Taverne",
                    "adventure" => "Aventure",
                    "drama"     => "Drame",
                    "romance"   => "Romance",
                    "lore"      => "Lore-friendly",
                    "dark"      => "Dark themes",
                    _           => t,
                }));
                DrawRow("Thèmes", themes);
            }
        }

        ImGui.Spacing();

        ImGui.Spacing();
        if (ImGui.Button(l.Cancel)) IsOpen = false;
    }

    private static void DrawRow(string label, string value)
    {
        ImGui.TextDisabled(label + " :");
        ImGui.SameLine(150);
        ImGui.TextUnformatted(value);
    }

    private static string LevelLabel(Loc l, string key) => key switch
    {
        "beginner"  => l.RpProfileLevelBeginner,
        "casual"    => l.RpProfileLevelCasual,
        "confirmed" => l.RpProfileLevelConfirmed,
        _           => key,
    };

    private static string ApproachLabel(Loc l, string key) => key switch
    {
        "come_to_me" => l.RpProfileApproachCome,
        "i_approach" => l.RpProfileApproachIGo,
        "either"     => l.RpProfileApproachEither,
        _            => key,
    };
}

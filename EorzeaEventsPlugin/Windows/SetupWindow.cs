using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class SetupWindow : Window
{
    private readonly Configuration            _config;
    private          ISharedImmediateTexture? _banner;
    private int    _step         = 0;
    private string _tokenBuf     = string.Empty;
    private bool   _tokenMasked  = true;
    private string _error        = string.Empty;
    private bool   _tokenInvalid = false;

    public void Restart(bool tokenInvalid = false)
    {
        _step         = tokenInvalid ? 1 : 0;
        _tokenBuf     = string.Empty;
        _error        = string.Empty;
        _tokenInvalid = tokenInvalid;
        IsOpen        = true;
    }

    public SetupWindow(Configuration config)
        : base("Eorzea Events — Configuration##setup", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620, 400),
            MaximumSize = new Vector2(900, 640),
        };
        _config = config;

        var bannerFile = new FileInfo(
            Path.Combine(Plugin.PluginInterface.AssemblyLocation.DirectoryName!, "banner.png"));
        if (bannerFile.Exists)
            _banner = Plugin.TextureProvider.GetFromFile(bannerFile);
    }

    public override void Draw()
    {
        switch (_step)
        {
            case 0: DrawWelcome(); break;
            case 1: DrawToken();   break;
            case 2: DrawDone();    break;
        }
    }

    private void DrawBanner(float maxHeight = 120f)
    {
        if (_banner == null) return;
        IDalamudTextureWrap? wrap = _banner.GetWrapOrDefault();
        if (wrap == null) return;

        var availW  = ImGui.GetContentRegionAvail().X;
        var aspect  = wrap.Width / (float)wrap.Height;
        var h       = Math.Min(availW / aspect, maxHeight);
        var w       = h * aspect;
        var offsetX = (availW - w) / 2f;
        if (offsetX > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
        ImGui.Image(wrap.Handle, new Vector2(w, h));
        ImGui.Spacing();
    }

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    // ─── Étape 0 : Bienvenue ──────────────────────────────────────────────────

    private void DrawWelcome()
    {
        var l = Plugin.L;
        DrawBanner();

        ImGui.Text(l.SetupWelcomeL1);
        ImGui.SameLine(0, 4);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.7f, 1f, 1f));
        ImGui.Text("eorzea.events");
        if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked()) OpenUrl("https://eorzea.events");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(UiStyle.TextMuted, l.SetupWelcomeL2);
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextMuted, l.SetupWelcomeL3);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (UiPrimitives.ColorButton(l.SetupStart, UiStyle.MediumButton,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
            _step = 1;
    }

    // ─── Étape 1 : Token — instructions (gauche 45%) | saisie (droite 55%) ───

    private void DrawToken()
    {
        var l = Plugin.L;
        DrawBanner(80f);

        if (_tokenInvalid)
            UiPrimitives.DrawAlert(new Vector4(1f, 0.7f, 0.2f, 1f),
                "⚠  " + l.SetupTokenInvalid, string.Empty, () => { });

        if (!ImGui.BeginTable("##tokenform", 2, ImGuiTableFlags.None)) return;
        ImGui.TableSetupColumn("desc",  ImGuiTableColumnFlags.WidthStretch, 0.45f);
        ImGui.TableSetupColumn("input", ImGuiTableColumnFlags.WidthStretch, 0.55f);
        ImGui.TableNextRow();

        // Instructions (gauche)
        ImGui.TableSetColumnIndex(0);
        ImGui.TextColored(UiStyle.TextSection, l.SetupStepTitle.ToUpper());
        ImGui.Spacing();
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(UiStyle.TextMuted, l.SetupStepDesc);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UiPrimitives.ColorButton(l.SetupOpenDashboard, new Vector2(-1, 0),
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
            OpenUrl(_config.BaseUrl.TrimEnd('/') + "/dashboard/profil#plugin-token");

        // Saisie (droite)
        ImGui.TableSetColumnIndex(1);
        ImGui.TextColored(UiStyle.TextMuted, l.SetupTokenLabel);
        ImGui.SetNextItemWidth(-(UiStyle.SmallButton.X + ImGui.GetStyle().ItemSpacing.X));
        if (_tokenMasked)
            ImGui.InputText("##token", ref _tokenBuf, 256, ImGuiInputTextFlags.Password);
        else
            ImGui.InputText("##token", ref _tokenBuf, 256);
        ImGui.SameLine();
        if (ImGui.Button(_tokenMasked ? l.Show : l.Hide, UiStyle.SmallButton))
            _tokenMasked = !_tokenMasked;

        if (!string.IsNullOrEmpty(_error))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 0.35f, 0.35f, 1), _error);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var canSave = !string.IsNullOrWhiteSpace(_tokenBuf);
        if (!canSave) ImGui.BeginDisabled();
        if (UiPrimitives.ColorButton(l.Save, UiStyle.MediumButton,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
        {
            var trimmed = _tokenBuf.Trim();
            if (!trimmed.StartsWith("ee_"))
            {
                _error = l.SetupErrPrefix;
            }
            else
            {
                _config.ApiToken = trimmed;
                _config.Save();
                Plugin.RebuildApiClient();
                _tokenInvalid = false;
                _step  = 2;
                _error = string.Empty;
            }
        }
        if (!canSave) ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button(l.SetupSkip, UiStyle.SmallButton))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }

        ImGui.EndTable();
    }

    // ─── Étape 2 : Terminé ────────────────────────────────────────────────────

    private void DrawDone()
    {
        var l = Plugin.L;
        DrawBanner();

        UiPrimitives.DrawCard(() =>
        {
            ImGui.TextColored(UiStyle.StatusOpen, l.SetupDoneTitle);
            ImGui.Spacing();
            ImGui.PushTextWrapPos(0);
            ImGui.TextColored(UiStyle.TextMuted, l.SetupDoneL1);
            ImGui.TextColored(UiStyle.TextMuted, l.SetupDoneL2);
            ImGui.Spacing();
            ImGui.TextColored(UiStyle.TextSubtle, l.SetupDoneHint);
            ImGui.PopTextWrapPos();
        });

        ImGui.Spacing();
        if (UiPrimitives.ColorButton(l.SetupOpenPlugin, UiStyle.WideButton,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }
    }
}

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
    private int    _step           = 0;
    private string _tokenBuf       = string.Empty;
    private bool   _tokenMasked    = true;
    private string _error          = string.Empty;
    private bool   _tokenInvalid   = false;

    public void Restart(bool tokenInvalid = false)
    {
        _step         = tokenInvalid ? 1 : 0;
        _tokenBuf     = string.Empty;
        _error        = string.Empty;
        _tokenInvalid = tokenInvalid;
        IsOpen        = true;
    }

    public SetupWindow(Configuration config)
        : base("Eorzea Events — Configuration##setup",
               ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
    {
        Size = new Vector2(480, 420);
        SizeCondition = ImGuiCond.Always;
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

    private void DrawBanner()
    {
        if (_banner == null) return;
        IDalamudTextureWrap? wrap = _banner.GetWrapOrDefault();
        if (wrap == null) return;

        var w = ImGui.GetContentRegionAvail().X;
        var h = Math.Min(w * (wrap.Height / (float)wrap.Width), 160f);
        ImGui.Image(wrap.Handle, new Vector2(w, h));
        ImGui.Spacing();
    }

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    private void DrawWelcome()
    {
        var l = Plugin.L;
        DrawBanner();

        // Ligne 1 + lien inline
        ImGui.Text(l.SetupWelcomeL1);
        ImGui.SameLine(0, 4);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.7f, 1f, 1f));
        ImGui.Text("eorzea.events");
        if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked()) OpenUrl("https://eorzea.events");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.TextWrapped(l.SetupWelcomeL2);
        ImGui.Spacing();
        ImGui.TextWrapped(l.SetupWelcomeL3);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(l.SetupStart, new Vector2(120, 0)))
            _step = 1;
    }

    private void DrawToken()
    {
        var l = Plugin.L;
        DrawBanner();

        if (_tokenInvalid)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.6f, 0.3f, 0.0f, 0.35f));
            ImGui.BeginChild("##tokenctx", new Vector2(0, 50), false);
            ImGui.SetCursorPos(new Vector2(8, 6));
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.2f, 1f), $"⚠  {l.SetupTokenInvalid}");
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }

        ImGui.Text(l.SetupStepTitle);
        ImGui.Spacing();
        ImGui.TextWrapped(l.SetupStepDesc);
        ImGui.Spacing();

        if (ImGui.Button(l.SetupOpenDashboard))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                _config.BaseUrl.TrimEnd('/') + "/dashboard") { UseShellExecute = true });

        ImGui.Spacing();
        ImGui.Text(l.SetupTokenLabel);
        ImGui.SetNextItemWidth(-80);
        if (_tokenMasked)
            ImGui.InputText("##token", ref _tokenBuf, 256, ImGuiInputTextFlags.Password);
        else
            ImGui.InputText("##token", ref _tokenBuf, 256);
        ImGui.SameLine();
        if (ImGui.Button(_tokenMasked ? l.Show : l.Hide))
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
        if (ImGui.Button(l.Save, new Vector2(120, 0)))
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
                _step = 2;
                _error = string.Empty;
            }
        }
        if (!canSave) ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button(l.SetupSkip, new Vector2(80, 0)))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }
    }

    private void DrawDone()
    {
        var l = Plugin.L;
        DrawBanner();
        ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), l.SetupDoneTitle);
        ImGui.Spacing();
        ImGui.TextWrapped(l.SetupDoneL1);
        ImGui.TextWrapped(l.SetupDoneL2);
        ImGui.Spacing();
        ImGui.TextDisabled(l.SetupDoneHint);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(l.SetupOpenPlugin, new Vector2(160, 0)))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }
    }
}

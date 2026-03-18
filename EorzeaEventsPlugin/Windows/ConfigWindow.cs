using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;
    private string _tokenBuf    = string.Empty;
    private string _urlBuf      = string.Empty;
    private bool   _tokenMasked = true;
    private bool   _notifyRpLive;
    private bool   _notifyMyWorld;

    public ConfigWindow(Configuration config) : base("Eorzea Events — Configuration##config")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 300),
            MaximumSize = new Vector2(700, 480),
        };
        _config        = config;
        _tokenBuf      = config.ApiToken;
        _urlBuf        = config.BaseUrl;
        _notifyRpLive  = config.NotifyRpLive;
        _notifyMyWorld = config.NotifyMyWorld;
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Collez ici le token API généré depuis votre dashboard Eorzea Events.");
        ImGui.SameLine();
        if (ImGui.SmallButton("Ouvrir le dashboard"))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                _urlBuf.TrimEnd('/') + "/dashboard") { UseShellExecute = true });
        ImGui.Spacing();

        // Token
        ImGui.Text("Token API :");
        ImGui.SetNextItemWidth(-80);
        if (_tokenMasked)
            ImGui.InputText("##token", ref _tokenBuf, 256, ImGuiInputTextFlags.Password);
        else
            ImGui.InputText("##token", ref _tokenBuf, 256);
        ImGui.SameLine();
        if (ImGui.Button(_tokenMasked ? "Afficher" : "Masquer"))
            _tokenMasked = !_tokenMasked;

        ImGui.Spacing();

        // URL
        ImGui.Text("URL du site :");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##url", ref _urlBuf, 256);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Notifications
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), "Notifications");
        ImGui.Spacing();
        ImGui.Checkbox("Notifier les nouvelles sessions RP Live", ref _notifyRpLive);
        if (_notifyRpLive)
        {
            ImGui.Indent();
            ImGui.Checkbox("Limiter aux sessions de mon monde", ref _notifyMyWorld);
            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Enregistrer"))
        {
            _config.ApiToken      = _tokenBuf.Trim();
            _config.BaseUrl       = _urlBuf.TrimEnd('/');
            _config.NotifyRpLive  = _notifyRpLive;
            _config.NotifyMyWorld = _notifyMyWorld;
            _config.Save();
            Plugin.RebuildApiClient();
            IsOpen = false;
        }
        ImGui.SameLine();
        if (ImGui.Button("Annuler"))
        {
            _tokenBuf      = _config.ApiToken;
            _urlBuf        = _config.BaseUrl;
            _notifyRpLive  = _config.NotifyRpLive;
            _notifyMyWorld = _config.NotifyMyWorld;
            IsOpen = false;
        }
    }
}

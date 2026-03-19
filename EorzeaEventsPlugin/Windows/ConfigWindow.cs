using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;
    private string _tokenBuf    = string.Empty;
    private bool   _tokenMasked = true;
    private bool   _notifyRpLive;
    private bool   _notifyRpLiveChat;
    private bool   _notifyMyWorld;
    private bool   _alertOnZoneChange;
    private bool   _alertOnRpTagRemoved;
    private bool   _suggestSessionOnRpTag;

    public ConfigWindow(Configuration config) : base("Eorzea Events — Configuration##config")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 300),
            MaximumSize = new Vector2(700, 480),
        };
        _config        = config;
        _tokenBuf      = config.ApiToken;
        _notifyRpLive        = config.NotifyRpLive;
        _notifyRpLiveChat    = config.NotifyRpLiveChat;
        _notifyMyWorld       = config.NotifyMyWorld;
        _alertOnZoneChange     = config.AlertOnZoneChange;
        _alertOnRpTagRemoved   = config.AlertOnRpTagRemoved;
        _suggestSessionOnRpTag = config.SuggestSessionOnRpTag;
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Collez ici le token API généré depuis votre dashboard Eorzea Events.");
        ImGui.SameLine();
        if (ImGui.SmallButton("Ouvrir le dashboard"))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                _config.BaseUrl + "/dashboard") { UseShellExecute = true });
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
        ImGui.Separator();
        ImGui.Spacing();

        // Notifications
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), "Notifications");
        ImGui.Spacing();
        ImGui.Checkbox("Toast — Notifier les nouvelles sessions RP Live", ref _notifyRpLive);
        ImGui.Checkbox("Chat — Annoncer les nouvelles sessions RP dans le chat", ref _notifyRpLiveChat);
        if (_notifyRpLive || _notifyRpLiveChat)
        {
            ImGui.Indent();
            ImGui.Checkbox("Limiter aux sessions de mon monde", ref _notifyMyWorld);
            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Alertes session
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), "Alertes de session RP");
        ImGui.Spacing();
        ImGui.Checkbox("Proposer de démarrer une session quand le tag RP est activé", ref _suggestSessionOnRpTag);
        ImGui.Checkbox("Alerter après un changement de zone ou un TP", ref _alertOnZoneChange);
        ImGui.Checkbox("Alerter quand le tag RP est retiré", ref _alertOnRpTagRemoved);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Enregistrer"))
        {
            _config.ApiToken      = _tokenBuf.Trim();
            _config.NotifyRpLive        = _notifyRpLive;
            _config.NotifyRpLiveChat    = _notifyRpLiveChat;
            _config.NotifyMyWorld       = _notifyMyWorld;
            _config.SuggestSessionOnRpTag = _suggestSessionOnRpTag;
            _config.AlertOnZoneChange     = _alertOnZoneChange;
            _config.AlertOnRpTagRemoved   = _alertOnRpTagRemoved;
            _config.Save();
            Plugin.RebuildApiClient();
            IsOpen = false;
        }
        ImGui.SameLine();
        if (ImGui.Button("Annuler"))
        {
            _tokenBuf      = _config.ApiToken;
            _notifyRpLive        = _config.NotifyRpLive;
            _notifyRpLiveChat    = _config.NotifyRpLiveChat;
            _notifyMyWorld       = _config.NotifyMyWorld;
            _suggestSessionOnRpTag = _config.SuggestSessionOnRpTag;
            _alertOnZoneChange     = _config.AlertOnZoneChange;
            _alertOnRpTagRemoved   = _config.AlertOnRpTagRemoved;
            IsOpen = false;
        }
    }
}

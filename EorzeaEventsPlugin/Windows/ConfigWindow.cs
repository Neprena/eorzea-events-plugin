using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;
    private bool _notifyRpLiveScreen;
    private bool _notifyRpLive;
    private bool _notifyRpLiveChat;
    private bool _notifyMyWorld;
    private bool _notifyNearbyZone;
    private bool _alertOnZoneChange;
    private bool _alertOnRpTagRemoved;
    private bool _suggestSessionOnRpTag;
    private int  _languageIndex;

    public ConfigWindow(Configuration config) : base("Eorzea Events — Configuration##config")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(460, 440),
            MaximumSize = new Vector2(700, 660),
        };
        _config              = config;
        _notifyRpLiveScreen  = config.NotifyRpLiveScreen;
        _notifyRpLive        = config.NotifyRpLive;
        _notifyRpLiveChat    = config.NotifyRpLiveChat;
        _notifyMyWorld       = config.NotifyMyWorld;
        _notifyNearbyZone    = config.NotifyNearbyZone;
        _alertOnZoneChange     = config.AlertOnZoneChange;
        _alertOnRpTagRemoved   = config.AlertOnRpTagRemoved;
        _suggestSessionOnRpTag = config.SuggestSessionOnRpTag;
        _languageIndex         = (int)config.Language;
    }

    public override void OnOpen()
    {
        _notifyRpLiveScreen    = _config.NotifyRpLiveScreen;
        _notifyRpLive          = _config.NotifyRpLive;
        _notifyRpLiveChat      = _config.NotifyRpLiveChat;
        _notifyMyWorld         = _config.NotifyMyWorld;
        _notifyNearbyZone      = _config.NotifyNearbyZone;
        _alertOnZoneChange     = _config.AlertOnZoneChange;
        _alertOnRpTagRemoved   = _config.AlertOnRpTagRemoved;
        _suggestSessionOnRpTag = _config.SuggestSessionOnRpTag;
        _languageIndex         = (int)_config.Language;
    }

    public override void Draw()
    {
        var l = Plugin.L;

        // Token API
        ImGui.Text(l.CfgTokenLabel);
        ImGui.SameLine();
        var hasToken = !string.IsNullOrWhiteSpace(_config.ApiToken);
        if (hasToken)
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f), l.CfgTokenOk + " ✓");
        else
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), l.CfgTokenMissing);
        ImGui.SameLine();
        if (ImGui.SmallButton(l.CfgTokenEdit + "##token"))
            Plugin.OpenSetup(tokenInvalid: !hasToken || !Plugin.Api.IsTokenValid);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Notifications
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), l.CfgNotifHeader);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifScreen + "##screen", ref _notifyRpLiveScreen);
        ImGui.SameLine();
        if (ImGui.SmallButton(l.CfgTest + "##testscreen"))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifNewRpScreen, "Clair de lune", "La Noscea", "Odin"),
                    new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow }));
        ImGui.TextDisabled(l.CfgNotifScreenHint);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifDalamud + "##dalamud", ref _notifyRpLive);
        ImGui.SameLine();
        if (ImGui.SmallButton(l.CfgTest + "##testdalamud"))
            Plugin.NotificationMgr.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title           = l.NotifNewRpTitle,
                Content         = "Clair de lune — La Noscea (Odin)",
                Type            = Dalamud.Interface.ImGuiNotification.NotificationType.Info,
                InitialDuration = System.TimeSpan.FromSeconds(6),
            });
        ImGui.TextDisabled(l.CfgNotifDalamudHint);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifChat + "##chat", ref _notifyRpLiveChat);
        ImGui.SameLine();
        if (ImGui.SmallButton(l.CfgTest + "##testchat"))
            Plugin.ChatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                .AddUiForeground(32)
                .AddText("[Eorzea Events] ")
                .AddUiForegroundOff()
                .AddText(string.Format(l.NotifNewRpChat, "Clair de lune", "La Noscea", "Odin"))
                .Build());
        ImGui.Spacing();

        if (_notifyRpLiveScreen || _notifyRpLive || _notifyRpLiveChat)
        {
            ImGui.Indent();
            ImGui.Checkbox(l.CfgNotifMyWorld, ref _notifyMyWorld);
            ImGui.Unindent();
            ImGui.Spacing();
        }

        ImGui.Checkbox(l.CfgNotifNearby + "##nearby", ref _notifyNearbyZone);
        ImGui.SameLine();
        if (ImGui.SmallButton(l.CfgTest + "##testnearby"))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowQuest(
                    string.Format(l.NotifNearbyRp, "Clair de lune"),
                    new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true, DisplayCheckmark = false }));
        ImGui.TextDisabled(l.CfgNotifNearbyHint);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Alertes session
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), l.CfgSessionHeader);
        ImGui.Spacing();
        ImGui.Checkbox(l.CfgSuggestOnTag, ref _suggestSessionOnRpTag);
        ImGui.Checkbox(l.CfgAlertZone,    ref _alertOnZoneChange);
        ImGui.Checkbox(l.CfgAlertTag,     ref _alertOnRpTagRemoved);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Langue
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), l.CfgLangHeader);
        ImGui.Spacing();
        var langs = new[] { l.CfgLangAuto, l.CfgLangFr, l.CfgLangEn };
        ImGui.SetNextItemWidth(240);
        ImGui.Combo("##lang", ref _languageIndex, langs, langs.Length);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(l.Save, new Vector2(120, 0)))
        {
            _config.NotifyRpLiveScreen    = _notifyRpLiveScreen;
            _config.NotifyRpLive          = _notifyRpLive;
            _config.NotifyRpLiveChat      = _notifyRpLiveChat;
            _config.NotifyMyWorld         = _notifyMyWorld;
            _config.NotifyNearbyZone      = _notifyNearbyZone;
            _config.SuggestSessionOnRpTag = _suggestSessionOnRpTag;
            _config.AlertOnZoneChange     = _alertOnZoneChange;
            _config.AlertOnRpTagRemoved   = _alertOnRpTagRemoved;
            _config.Language              = (PluginLanguage)_languageIndex;
            _config.Save();
            Plugin.RebuildApiClient();
            IsOpen = false;
        }
        ImGui.SameLine();
        if (ImGui.Button(l.Cancel, new Vector2(80, 0)))
        {
            _notifyRpLiveScreen    = _config.NotifyRpLiveScreen;
            _notifyRpLive          = _config.NotifyRpLive;
            _notifyRpLiveChat      = _config.NotifyRpLiveChat;
            _notifyMyWorld         = _config.NotifyMyWorld;
            _notifyNearbyZone      = _config.NotifyNearbyZone;
            _suggestSessionOnRpTag = _config.SuggestSessionOnRpTag;
            _alertOnZoneChange     = _config.AlertOnZoneChange;
            _alertOnRpTagRemoved   = _config.AlertOnRpTagRemoved;
            _languageIndex         = (int)_config.Language;
            IsOpen = false;
        }
    }
}

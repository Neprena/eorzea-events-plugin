using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class ConfigWindow : Window
{
    private const string SampleVenue = "Établissement";
    private const string SampleEvent = "Titre de l'événement";
    private const string SampleRpTitle = "RP ouvert";
    private const string SampleZone = "Zone";
    private const string SampleServer = "Serveur";

    private readonly Configuration _config;
    private bool _notifyRpLiveScreen;
    private bool _notifyRpLive;
    private bool _notifyRpLiveChat;
    private bool _notifyMyWorld;
    private bool _notifyNearbyZone;
    private bool _notifyEventStartScreen;
    private bool _notifyEventStartChat;
    private bool _alertOnZoneChange;
    private bool _alertOnRpTagRemoved;
    private bool _alertOnSessionExpiring;
    private bool _suggestSessionOnRpTag;
    private bool _showDtrRp;
    private bool _showDtrEvents;
    private int  _languageIndex;

    public ConfigWindow(Configuration config) : base("Eorzea Events — Configuration##config")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 620),
            MaximumSize = new Vector2(700, 760),
        };
        _config              = config;
        _notifyRpLiveScreen  = config.NotifyRpLiveScreen;
        _notifyRpLive        = config.NotifyRpLive;
        _notifyRpLiveChat    = config.NotifyRpLiveChat;
        _notifyMyWorld       = config.NotifyMyWorld;
        _notifyNearbyZone    = config.NotifyNearbyZone;
        _notifyEventStartScreen  = config.NotifyEventStartDalamud;
        _notifyEventStartChat    = config.NotifyEventStartChat;
        _alertOnZoneChange      = config.AlertOnZoneChange;
        _alertOnRpTagRemoved    = config.AlertOnRpTagRemoved;
        _alertOnSessionExpiring = config.AlertOnSessionExpiring;
        _suggestSessionOnRpTag  = config.SuggestSessionOnRpTag;
        _showDtrRp             = config.ShowDtrRp;
        _showDtrEvents         = config.ShowDtrEvents;
        _languageIndex         = (int)config.Language;
    }

    public override void OnOpen()
    {
        _notifyRpLiveScreen    = _config.NotifyRpLiveScreen;
        _notifyRpLive          = _config.NotifyRpLive;
        _notifyRpLiveChat      = _config.NotifyRpLiveChat;
        _notifyMyWorld         = _config.NotifyMyWorld;
        _notifyNearbyZone      = _config.NotifyNearbyZone;
        _notifyEventStartScreen  = _config.NotifyEventStartDalamud;
        _notifyEventStartChat    = _config.NotifyEventStartChat;
        _alertOnZoneChange      = _config.AlertOnZoneChange;
        _alertOnRpTagRemoved    = _config.AlertOnRpTagRemoved;
        _alertOnSessionExpiring = _config.AlertOnSessionExpiring;
        _suggestSessionOnRpTag  = _config.SuggestSessionOnRpTag;
        _showDtrRp              = _config.ShowDtrRp;
        _showDtrEvents         = _config.ShowDtrEvents;
        _languageIndex         = (int)_config.Language;
    }

    public override void Draw()
    {
        var l = Plugin.L;

        var footerHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y * 2 + 8f;
        if (ImGui.BeginChild("##configscroll", new Vector2(0, -footerHeight), false))
        {
            DrawTokenSection(l);
            DrawRpNotificationSection(l);
            DrawEventNotificationSection(l);
            DrawSessionSection(l);
            DrawDtrSection(l);
            DrawLanguageSection(l);
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(l.Save, UiSizes.MediumButton))
        {
            _config.NotifyRpLiveScreen    = _notifyRpLiveScreen;
            _config.NotifyRpLive          = _notifyRpLive;
            _config.NotifyRpLiveChat      = _notifyRpLiveChat;
            _config.NotifyMyWorld         = _notifyMyWorld;
            _config.NotifyNearbyZone      = _notifyNearbyZone;
            _config.NotifyEventStartDalamud = _notifyEventStartScreen;
            _config.NotifyEventStartChat    = _notifyEventStartChat;
            _config.SuggestSessionOnRpTag   = _suggestSessionOnRpTag;
            _config.AlertOnZoneChange       = _alertOnZoneChange;
            _config.AlertOnRpTagRemoved     = _alertOnRpTagRemoved;
            _config.AlertOnSessionExpiring  = _alertOnSessionExpiring;
            _config.ShowDtrRp             = _showDtrRp;
            _config.ShowDtrEvents         = _showDtrEvents;
            _config.Language              = (PluginLanguage)_languageIndex;
            _config.Save();
            Plugin.RebuildApiClient();
            Plugin.ApplyDtrVisibility();
            IsOpen = false;
        }
        ImGui.SameLine();
        if (ImGui.Button(l.Cancel, UiSizes.SmallButton))
        {
            _notifyRpLiveScreen    = _config.NotifyRpLiveScreen;
            _notifyRpLive          = _config.NotifyRpLive;
            _notifyRpLiveChat      = _config.NotifyRpLiveChat;
            _notifyMyWorld         = _config.NotifyMyWorld;
            _notifyNearbyZone      = _config.NotifyNearbyZone;
            _notifyEventStartScreen  = _config.NotifyEventStartDalamud;
            _notifyEventStartChat    = _config.NotifyEventStartChat;
            _suggestSessionOnRpTag  = _config.SuggestSessionOnRpTag;
            _alertOnZoneChange      = _config.AlertOnZoneChange;
            _alertOnRpTagRemoved    = _config.AlertOnRpTagRemoved;
            _alertOnSessionExpiring = _config.AlertOnSessionExpiring;
            _showDtrRp              = _config.ShowDtrRp;
            _showDtrEvents         = _config.ShowDtrEvents;
            _languageIndex         = (int)_config.Language;
            IsOpen = false;
        }
    }

    private void DrawTokenSection(Loc l)
    {
        if (!ImGui.CollapsingHeader("API##token", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.Text(l.CfgTokenLabel);
        ImGui.SameLine();
        var hasToken = !string.IsNullOrWhiteSpace(_config.ApiToken);
        if (hasToken)
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f), l.CfgTokenOk + " ✓");
        else
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), l.CfgTokenMissing);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTokenEdit + "##token", UiSizes.SmallButton))
            Plugin.OpenSetup(tokenInvalid: !hasToken || !Plugin.Api.IsTokenValid);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawRpNotificationSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgNotifHeader + "##rpnotif", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.Checkbox(l.CfgNotifScreen + "##screen", ref _notifyRpLiveScreen);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testscreen", UiSizes.SmallButton))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifNewRpScreen, SampleRpTitle, SampleZone, SampleServer),
                    new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow }));
        ImGui.TextDisabled(l.CfgNotifScreenHint);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifDalamud + "##dalamud", ref _notifyRpLive);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testdalamud", UiSizes.SmallButton))
            Plugin.NotificationMgr.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title           = l.NotifNewRpTitle,
                Content         = $"{SampleRpTitle} — {SampleZone} ({SampleServer})",
                Type            = Dalamud.Interface.ImGuiNotification.NotificationType.Info,
                InitialDuration = System.TimeSpan.FromSeconds(6),
            });
        ImGui.TextDisabled(l.CfgNotifDalamudHint);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifChat + "##chat", ref _notifyRpLiveChat);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testchat", UiSizes.SmallButton))
            Plugin.ChatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                .AddUiForeground(32)
                .AddText("[Eorzea Events] ")
                .AddUiForegroundOff()
                .AddText(string.Format(l.NotifNewRpChat, SampleRpTitle, SampleZone, SampleServer))
                .Build());
        ImGui.Spacing();

        if (_notifyRpLiveScreen || _notifyRpLive || _notifyRpLiveChat)
            ImGui.Checkbox(l.CfgNotifMyWorld, ref _notifyMyWorld);

        ImGui.Checkbox(l.CfgNotifNearby + "##nearby", ref _notifyNearbyZone);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testnearby", UiSizes.SmallButton))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowQuest(
                    string.Format(l.NotifNearbyRp, SampleRpTitle),
                    new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true, DisplayCheckmark = false }));
        ImGui.TextDisabled(l.CfgNotifNearbyHint);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawEventNotificationSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgEventNotifHeader + "##eventnotif", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.Checkbox(l.CfgEventNotifScreen + "##eventscreen", ref _notifyEventStartScreen);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testeventscreen", UiSizes.SmallButton))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifEventStartScreen, SampleEvent, SampleVenue),
                    new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow }));
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgEventNotifChat + "##eventchat", ref _notifyEventStartChat);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testeventchat", UiSizes.SmallButton))
            Plugin.ChatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                .AddUiForeground(32)
                .AddText("[Eorzea Events] ")
                .AddUiForegroundOff()
                .AddText(string.Format(l.NotifEventStartChat, $"{SampleVenue} — {SampleEvent} | 21:00 → 00:00 | {SampleServer} | {SampleZone}, {string.Format(l.HousingWard, 5)}, {l.FieldPlot} 30"))
                .Build());
        ImGui.TextDisabled(l.CfgEventNotifHint);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawSessionSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgSessionHeader + "##session", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.Checkbox(l.CfgSuggestOnTag, ref _suggestSessionOnRpTag);
        ImGui.Checkbox(l.CfgAlertZone, ref _alertOnZoneChange);
        ImGui.Checkbox(l.CfgAlertTag, ref _alertOnRpTagRemoved);
        ImGui.Checkbox(l.CfgAlertExpiry, ref _alertOnSessionExpiring);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawDtrSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgDtrHeader + "##dtr", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        ImGui.Checkbox(l.CfgDtrRp + "##dtrRp", ref _showDtrRp);
        ImGui.Checkbox(l.CfgDtrEvents + "##dtrEvents", ref _showDtrEvents);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawLanguageSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgLangHeader + "##langsection", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.Indent();
        var langs = new[] { l.CfgLangAuto, l.CfgLangFr, l.CfgLangEn };
        ImGui.SetNextItemWidth(240);
        ImGui.Combo("##lang", ref _languageIndex, langs, langs.Length);
        ImGui.Unindent();
        ImGui.Spacing();
    }
}

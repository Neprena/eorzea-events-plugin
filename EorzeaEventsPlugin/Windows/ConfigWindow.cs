using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class ConfigWindow : Window
{
    private const string SampleVenue   = "Établissement";
    private const string SampleEvent   = "Titre de l'événement";
    private const string SampleRpTitle = "RP ouvert";
    private const string SampleZone    = "Zone";
    private const string SampleServer  = "Serveur";

    private readonly Configuration _config;
    private bool _notifyRpLiveScreen;
    private bool _notifyRpLive;
    private bool _notifyRpLiveChat;
    private bool _notifyMyWorld;
    private bool _notifyNearbyZone;
    private bool _notifyRpLanguageFilter;
    private bool _notifyEventStartScreen;
    private bool _notifyEventStartChat;
    private bool _alertOnZoneChange;
    private bool _alertOnRpTagRemoved;
    private bool _alertOnSessionExpiring;
    private bool _suggestSessionOnRpTag;
    private bool _showDtrRp;
    private bool _showDtrEvents;
    private int  _languageIndex;
#if DEBUG
    private string _baseUrl = string.Empty;
#endif

    public ConfigWindow(Configuration config) : base("Eorzea Events — Configuration##config")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620, 480),
            MaximumSize = new Vector2(1100, 900),
        };
        _config = config;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        _notifyRpLiveScreen     = _config.NotifyRpLiveScreen;
        _notifyRpLive           = _config.NotifyRpLive;
        _notifyRpLiveChat       = _config.NotifyRpLiveChat;
        _notifyMyWorld          = _config.NotifyMyWorld;
        _notifyNearbyZone       = _config.NotifyNearbyZone;
        _notifyRpLanguageFilter = _config.NotifyRpLanguageFilter;
        _notifyEventStartScreen = _config.NotifyEventStartDalamud;
        _notifyEventStartChat   = _config.NotifyEventStartChat;
        _alertOnZoneChange      = _config.AlertOnZoneChange;
        _alertOnRpTagRemoved    = _config.AlertOnRpTagRemoved;
        _alertOnSessionExpiring = _config.AlertOnSessionExpiring;
        _suggestSessionOnRpTag  = _config.SuggestSessionOnRpTag;
        _showDtrRp              = _config.ShowDtrRp;
        _showDtrEvents          = _config.ShowDtrEvents;
        _languageIndex          = (int)_config.Language;
#if DEBUG
        _baseUrl                = _config.BaseUrl;
#endif
    }

    public override void OnOpen() => LoadFromConfig();

    public override void Draw()
    {
        var l = Plugin.L;

        var footerHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y * 2 + 8f;
        if (ImGui.BeginChild("##configscroll", new Vector2(0, -footerHeight), false))
        {
            // Token — pleine largeur en card compacte
            DrawTokenSection(l);
            ImGui.Spacing();

            ImGui.PushTextWrapPos(0);
            DrawRpNotificationSection(l);
            DrawEventNotificationSection(l);
            DrawSessionSection(l);
            DrawDtrSection(l);
            DrawLanguageSection(l);
#if DEBUG
            DrawDevSection();
#endif
            ImGui.PopTextWrapPos();
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGui.Spacing();

        if (UiPrimitives.ColorButton(l.Save, UiStyle.MediumButton,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
        {
            _config.NotifyRpLiveScreen      = _notifyRpLiveScreen;
            _config.NotifyRpLive            = _notifyRpLive;
            _config.NotifyRpLiveChat        = _notifyRpLiveChat;
            _config.NotifyMyWorld           = _notifyMyWorld;
            _config.NotifyNearbyZone        = _notifyNearbyZone;
            _config.NotifyRpLanguageFilter  = _notifyRpLanguageFilter;
            _config.NotifyEventStartDalamud = _notifyEventStartScreen;
            _config.NotifyEventStartChat    = _notifyEventStartChat;
            _config.SuggestSessionOnRpTag   = _suggestSessionOnRpTag;
            _config.AlertOnZoneChange       = _alertOnZoneChange;
            _config.AlertOnRpTagRemoved     = _alertOnRpTagRemoved;
            _config.AlertOnSessionExpiring  = _alertOnSessionExpiring;
            _config.ShowDtrRp               = _showDtrRp;
            _config.ShowDtrEvents           = _showDtrEvents;
            _config.Language                = (PluginLanguage)_languageIndex;
#if DEBUG
            _config.BaseUrl                 = _baseUrl.TrimEnd('/');
#endif
            _config.Save();
            Plugin.RebuildApiClient();
            Plugin.ApplyDtrVisibility();
            IsOpen = false;
        }
        ImGui.SameLine();
        if (ImGui.Button(l.Cancel, UiStyle.SmallButton))
        {
            LoadFromConfig();
            IsOpen = false;
        }
    }

    private void DrawTokenSection(Loc l)
    {
        UiPrimitives.DrawCard(() =>
        {
            ImGui.TextColored(UiStyle.TextSection, "API");
            ImGui.SameLine(0, 12);
            var hasToken = !string.IsNullOrWhiteSpace(_config.ApiToken);
            if (hasToken)
                ImGui.TextColored(UiStyle.StatusOpen, l.CfgTokenOk + " ✓");
            else
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), l.CfgTokenMissing);
            ImGui.SameLine(0, 12);
            if (UiPrimitives.ColorButton(l.CfgTokenEdit + "##token", UiStyle.SmallButton,
                UiStyle.SecondaryNormal, UiStyle.SecondaryHovered, UiStyle.SecondaryActive))
                Plugin.OpenSetup(tokenInvalid: !hasToken || !Plugin.Api.IsTokenValid);
        });
    }

    private void DrawRpNotificationSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgNotifHeader + "##rpnotif")) return;
        ImGui.Indent();

        ImGui.Checkbox(l.CfgNotifScreen + "##screen", ref _notifyRpLiveScreen);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testscreen", UiStyle.SmallButton))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifNewRpScreen, SampleRpTitle, SampleZone, SampleServer),
                    new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow }));
        ImGui.TextColored(UiStyle.TextSubtle, l.CfgNotifScreenHint);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifDalamud + "##dalamud", ref _notifyRpLive);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testdalamud", UiStyle.SmallButton))
            Plugin.NotificationMgr.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title           = l.NotifNewRpTitle,
                Content         = $"{SampleRpTitle} — {SampleZone} ({SampleServer})",
                Type            = Dalamud.Interface.ImGuiNotification.NotificationType.Info,
                InitialDuration = System.TimeSpan.FromSeconds(6),
            });
        ImGui.TextColored(UiStyle.TextSubtle, l.CfgNotifDalamudHint);
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgNotifChat + "##chat", ref _notifyRpLiveChat);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testchat", UiStyle.SmallButton))
            Plugin.ChatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                .AddUiForeground(32).AddText("[Eorzea Events] ").AddUiForegroundOff()
                .AddText(string.Format(l.NotifNewRpChat, SampleRpTitle, SampleZone, SampleServer)).Build());
        ImGui.Spacing();

        if (_notifyRpLiveScreen || _notifyRpLive || _notifyRpLiveChat)
            ImGui.Checkbox(l.CfgNotifMyWorld, ref _notifyMyWorld);

        if (_notifyRpLiveScreen || _notifyRpLive || _notifyRpLiveChat || _notifyNearbyZone)
        {
            ImGui.Checkbox(l.CfgNotifLanguageFilter + "##lang", ref _notifyRpLanguageFilter);
            ImGui.TextColored(UiStyle.TextSubtle, l.CfgNotifLanguageFilterHint);
            ImGui.Spacing();
        }

        ImGui.Checkbox(l.CfgNotifNearby + "##nearby", ref _notifyNearbyZone);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testnearby", UiStyle.SmallButton))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowQuest(
                    string.Format(l.NotifNearbyRp, SampleRpTitle),
                    new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true, DisplayCheckmark = false }));
        ImGui.TextColored(UiStyle.TextSubtle, l.CfgNotifNearbyHint);

        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawEventNotificationSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgEventNotifHeader + "##eventnotif")) return;
        ImGui.Indent();

        ImGui.Checkbox(l.CfgEventNotifScreen + "##eventscreen", ref _notifyEventStartScreen);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testeventscreen", UiStyle.SmallButton))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifEventStartScreen, SampleEvent, SampleVenue),
                    new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow }));
        ImGui.Spacing();

        ImGui.Checkbox(l.CfgEventNotifChat + "##eventchat", ref _notifyEventStartChat);
        ImGui.SameLine();
        if (ImGui.Button(l.CfgTest + "##testeventchat", UiStyle.SmallButton))
            Plugin.ChatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                .AddUiForeground(32).AddText("[Eorzea Events] ").AddUiForegroundOff()
                .AddText(string.Format(l.NotifEventStartChat,
                    $"{SampleVenue} — {SampleEvent} | 21:00 → 00:00 | {SampleServer} | {SampleZone}, {string.Format(l.HousingWard, 5)}, {l.FieldPlot} 30"))
                .Build());
        ImGui.TextColored(UiStyle.TextSubtle, l.CfgEventNotifHint);

        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawSessionSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgSessionHeader + "##session")) return;
        ImGui.Indent();
        ImGui.Checkbox(l.CfgSuggestOnTag,  ref _suggestSessionOnRpTag);
        ImGui.Checkbox(l.CfgAlertZone,     ref _alertOnZoneChange);
        ImGui.Checkbox(l.CfgAlertTag,      ref _alertOnRpTagRemoved);
        ImGui.Checkbox(l.CfgAlertExpiry,   ref _alertOnSessionExpiring);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawDtrSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgDtrHeader + "##dtr")) return;
        ImGui.Indent();
        ImGui.Checkbox(l.CfgDtrRp + "##dtrRp",        ref _showDtrRp);
        ImGui.Checkbox(l.CfgDtrEvents + "##dtrEvents", ref _showDtrEvents);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawLanguageSection(Loc l)
    {
        if (!ImGui.CollapsingHeader(l.CfgLangHeader + "##langsection")) return;
        ImGui.Indent();
        var langs = new[] { l.CfgLangAuto, l.CfgLangFr, l.CfgLangEn };
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##lang", ref _languageIndex, langs, langs.Length);
        ImGui.Unindent();
        ImGui.Spacing();
    }

#if DEBUG
    private void DrawDevSection()
    {
        ImGui.PushStyleColor(ImGuiCol.Header,        new Vector4(0.5f, 0.1f, 0.1f, 1f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.6f, 0.15f, 0.15f, 1f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive,  new Vector4(0.7f, 0.2f, 0.2f, 1f));
        var open = ImGui.CollapsingHeader("⚙ Dev — API Server##dev");
        ImGui.PopStyleColor(3);
        if (!open) return;
        ImGui.Indent();
        ImGui.TextColored(new Vector4(1f, 0.5f, 0.3f, 1f), "DEBUG BUILD ONLY");
        ImGui.Spacing();
        ImGui.Text("Base URL :");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##baseurl", ref _baseUrl, 256);
        ImGui.Spacing();
        if (ImGui.Button("Production##devprod", UiStyle.MediumButton))
            _baseUrl = "https://eorzea.events";
        ImGui.SameLine();
        if (ImGui.Button("Local dev (3000)##devlocal", UiStyle.MediumButton))
            _baseUrl = "http://localhost:3000";
        ImGui.Unindent();
        ImGui.Spacing();
    }
#endif
}

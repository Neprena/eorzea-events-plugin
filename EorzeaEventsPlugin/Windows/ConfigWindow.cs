using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;
    private bool   _notifyRpLiveScreen;
    private bool   _notifyRpLive;
    private bool   _notifyRpLiveChat;
    private bool   _notifyMyWorld;
    private bool   _notifyNearbyZone;
    private bool   _alertOnZoneChange;
    private bool   _alertOnRpTagRemoved;
    private bool   _suggestSessionOnRpTag;

    public ConfigWindow(Configuration config) : base("Eorzea Events — Configuration##config")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(460, 400),
            MaximumSize = new Vector2(700, 620),
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
    }

    public override void OnOpen()
    {
        _notifyRpLiveScreen  = _config.NotifyRpLiveScreen;
        _notifyRpLive        = _config.NotifyRpLive;
        _notifyRpLiveChat    = _config.NotifyRpLiveChat;
        _notifyMyWorld       = _config.NotifyMyWorld;
        _notifyNearbyZone    = _config.NotifyNearbyZone;
        _alertOnZoneChange   = _config.AlertOnZoneChange;
        _alertOnRpTagRemoved = _config.AlertOnRpTagRemoved;
        _suggestSessionOnRpTag = _config.SuggestSessionOnRpTag;
    }

    public override void Draw()
    {
        // Token API — affiché en lecture seule, modifiable via l'assistant
        ImGui.Text("Token API :");
        ImGui.SameLine();
        var hasToken = !string.IsNullOrWhiteSpace(_config.ApiToken);
        if (hasToken)
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1f), "Configuré ✓");
        else
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "Non configuré");
        ImGui.SameLine();
        if (ImGui.SmallButton("Modifier##token"))
            Plugin.OpenSetup(tokenInvalid: !hasToken || !Plugin.Api.IsTokenValid);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Notifications
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), "Quand une nouvelle session RP est annoncee");
        ImGui.Spacing();

        ImGui.Checkbox("Afficher une alerte au centre de l'ecran##screen", ref _notifyRpLiveScreen);
        ImGui.SameLine();
        if (ImGui.SmallButton("Tester##testscreen"))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    "Nouvelle session RP !\nClair de lune — La Noscea (Odin)",
                    new Dalamud.Game.Gui.Toast.ToastOptions { Speed = Dalamud.Game.Gui.Toast.ToastSpeed.Slow }));
        ImGui.TextDisabled("   Style natif FFXIV, comme les messages de bienvenue");
        ImGui.Spacing();

        ImGui.Checkbox("Afficher une bulle de notification##dalamud", ref _notifyRpLive);
        ImGui.SameLine();
        if (ImGui.SmallButton("Tester##testdalamud"))
            Plugin.NotificationMgr.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title           = "Nouvelle session RP Live",
                Content         = "Clair de lune — La Noscea (Odin)",
                Type            = Dalamud.Interface.ImGuiNotification.NotificationType.Info,
                InitialDuration = System.TimeSpan.FromSeconds(6),
            });
        ImGui.TextDisabled("   Petite carte dans le coin superieur droit");
        ImGui.Spacing();

        ImGui.Checkbox("Ecrire un message dans le chat##chat", ref _notifyRpLiveChat);
        ImGui.SameLine();
        if (ImGui.SmallButton("Tester##testchat"))
            Plugin.ChatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                .AddUiForeground(32)
                .AddText("[Eorzea Events] ")
                .AddUiForegroundOff()
                .AddText("Nouvelle session RP : Clair de lune — La Noscea (Odin)")
                .Build());
        ImGui.Spacing();

        if (_notifyRpLiveScreen || _notifyRpLive || _notifyRpLiveChat)
        {
            ImGui.Indent();
            ImGui.Checkbox("Ignorer les sessions sur d'autres serveurs", ref _notifyMyWorld);
            ImGui.Unindent();
            ImGui.Spacing();
        }

        ImGui.Checkbox("Alerte prioritaire si la session est dans ma zone actuelle##nearby", ref _notifyNearbyZone);
        ImGui.SameLine();
        if (ImGui.SmallButton("Tester##testnearby"))
            Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowQuest(
                    "Session RP dans votre zone !\nClair de lune",
                    new Dalamud.Game.Gui.Toast.QuestToastOptions { PlaySound = true, DisplayCheckmark = false }));
        ImGui.TextDisabled("   Meme serveur et meme zone — s'affiche meme si les options ci-dessus sont desactivees");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Alertes session
        ImGui.TextColored(new Vector4(0.78f, 0.64f, 0.35f, 1), "Quand j'ai une session RP en cours");
        ImGui.Spacing();
        ImGui.Checkbox("Me proposer de demarrer une session quand j'active le tag RP", ref _suggestSessionOnRpTag);
        ImGui.Checkbox("Me prevenir si je change de zone ou effectue un TP", ref _alertOnZoneChange);
        ImGui.Checkbox("Me prevenir si je retire le tag RP", ref _alertOnRpTagRemoved);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Enregistrer"))
        {
            _config.NotifyRpLiveScreen  = _notifyRpLiveScreen;
            _config.NotifyRpLive        = _notifyRpLive;
            _config.NotifyRpLiveChat    = _notifyRpLiveChat;
            _config.NotifyMyWorld       = _notifyMyWorld;
            _config.NotifyNearbyZone    = _notifyNearbyZone;
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
            _notifyRpLiveScreen  = _config.NotifyRpLiveScreen;
            _notifyRpLive        = _config.NotifyRpLive;
            _notifyRpLiveChat    = _config.NotifyRpLiveChat;
            _notifyMyWorld       = _config.NotifyMyWorld;
            _notifyNearbyZone    = _config.NotifyNearbyZone;
            _suggestSessionOnRpTag = _config.SuggestSessionOnRpTag;
            _alertOnZoneChange     = _config.AlertOnZoneChange;
            _alertOnRpTagRemoved   = _config.AlertOnRpTagRemoved;
            IsOpen = false;
        }
    }
}

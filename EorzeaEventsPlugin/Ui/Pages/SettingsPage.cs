using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Réglages du plugin.
///
/// Chaque changement est enregistré immédiatement, sans bouton de validation :
/// la fenêtre précédente perdait toutes les modifications si elle était fermée
/// sans valider, ce qui est un piège pour des réglages que l'on vient ajuster
/// un par un.
/// </summary>
internal sealed class SettingsPage(Configuration config)
{
    private const string SampleRpTitle = "Veillée au coin du feu";
    private const string SampleZone    = "Le Lavandier";
    private const string SampleServer  = "Ragnarok";
    private const string SampleEvent   = "Soirée contes";
    private const string SampleVenue   = "La Chandelle Verte";

    private string _baseUrl = config.BaseUrl;

    public void Draw()
    {
        var l = Plugin.L;

        using var scroll = ImRaii.Child("##settingsscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        DrawCharacters(l);
        DrawRpNotifications(l);
        DrawEventNotifications(l);
        DrawSession(l);
        DrawRpProfile(l);
        DrawStatusBar(l);
        DrawLanguage(l);
        DrawAbout(l);
#if DEBUG
        DrawDeveloper();
#endif

        // Respiration en fin de page : la dernière carte ne doit pas être collée
        // au bord bas, sous peine d'empêcher ses listes de s'ouvrir.
        Layout.Spacer(Theme.GapXl);
    }

    // ─── Personnages liés ─────────────────────────────────────────────────────

    private void DrawCharacters(Loc l)
    {
        using var card = Card.Begin("set_characters", interactive: false);

        Layout.SectionHeader(l.CfgCharactersHeader, Icons.Character, config.CharacterTokens.Count);

        // Couplage en cours : l'utilisateur doit confirmer côté navigateur.
        var link = Plugin.ActiveLinkState;
        if (link is { Status: "pending" } && DateTime.UtcNow < link.ExpiresAt)
        {
            Text.WithIcon(Icons.Clock,
                string.Format(l.CfgLinkPending, $"{link.CharacterName}@{link.WorldName}"),
                Theme.Idle, Theme.Idle);
            Text.Small(l.CfgLinkPendingHint);
            Layout.Spacer(Theme.GapS);

            if (Btn.Draw(l.CfgLinkReopen, BtnTone.Secondary, BtnSize.Medium, Icons.External))
            {
                try { Dalamud.Utility.Util.OpenLink(link.LinkUrl); }
                catch { /* le navigateur peut être indisponible */ }
            }
            Layout.Spacer(Theme.GapS);
        }

        var player = Plugin.ObjectTable.LocalPlayer;
        if (player == null)
        {
            Text.Small(l.CfgLinkNeedLogin);
        }
        else
        {
            var name      = player.Name.TextValue;
            var worldId   = (int)player.HomeWorld.RowId;
            var worldName = player.HomeWorld.Value.Name.ToString();

            if (config.FindCharacterToken(name, worldId) != null)
            {
                Text.WithIcon(Icons.Check, $"{name}@{worldName}", Theme.Online, Theme.Text);
                ImGui.SameLine(0f, Theme.S(Theme.GapM));
                if (Btn.Draw(l.CfgLinkAgain, BtnTone.Ghost, BtnSize.Medium, Icons.Refresh))
                    _ = Plugin.StartCharacterLinkAsync();
            }
            else if (Btn.Draw(string.Format(l.CfgLinkCharacter, $"{name}@{worldName}"),
                              BtnTone.Primary, BtnSize.Medium, Icons.Plus))
            {
                _ = Plugin.StartCharacterLinkAsync();
            }
        }

        if (config.CharacterTokens.Count == 0) return;

        Layout.Divider(Theme.GapS);

        int? forget = null;
        for (var i = 0; i < config.CharacterTokens.Count; i++)
        {
            var entry = config.CharacterTokens[i];

            Layout.Avatar(entry.CharacterName, 24f);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));

            ImGui.AlignTextToFramePadding();
            Text.Body($"{entry.CharacterName}@{entry.WorldName}");

            ImGui.SameLine();
            Layout.RightAlign(ImGui.GetFrameHeight());
            if (Btn.Icon(Icons.Trash, $"forget_{i}", BtnTone.Ghost, l.CfgLinkForget))
                forget = i;
        }

        if (forget is not { } index) return;
        config.CharacterTokens.RemoveAt(index);
        config.Save();
    }

    // ─── Notifications RP ─────────────────────────────────────────────────────

    private void DrawRpNotifications(Loc l)
    {
        using var card = Card.Begin("set_rpnotif", interactive: false);

        Layout.SectionHeader(l.CfgNotifHeader, Icons.RpLive);

        Row(l.CfgNotifScreen, l.CfgNotifScreenHint,
            () => config.NotifyRpLiveScreen, v => config.NotifyRpLiveScreen = v,
            () => Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifNewRpScreen, SampleRpTitle, SampleZone, SampleServer),
                    new ToastOptions { Speed = ToastSpeed.Slow })));

        Row(l.CfgNotifDalamud, l.CfgNotifDalamudHint,
            () => config.NotifyRpLive, v => config.NotifyRpLive = v,
            () => Plugin.NotificationMgr.AddNotification(new Notification
            {
                Title           = l.NotifNewRpTitle,
                Content         = $"{SampleRpTitle} - {SampleZone} ({SampleServer})",
                Type            = NotificationType.Info,
                InitialDuration = TimeSpan.FromSeconds(6),
            }));

        Row(l.CfgNotifChat, null,
            () => config.NotifyRpLiveChat, v => config.NotifyRpLiveChat = v,
            () => Plugin.ChatGui.Print(new SeStringBuilder()
                .AddUiForeground(32).AddText("[Eorzea Events] ").AddUiForegroundOff()
                .AddText(string.Format(l.NotifNewRpChat, SampleRpTitle, SampleZone, SampleServer))
                .Build()));

        Row(l.CfgNotifNearby, l.CfgNotifNearbyHint,
            () => config.NotifyNearbyZone, v => config.NotifyNearbyZone = v,
            () => Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowQuest(
                    string.Format(l.NotifNearbyRp, SampleRpTitle),
                    new QuestToastOptions { PlaySound = true, DisplayCheckmark = false })));

        // Les filtres n'ont de sens que si au moins un canal est actif.
        var anyChannel = config.NotifyRpLiveScreen || config.NotifyRpLive || config.NotifyRpLiveChat;
        if (!anyChannel && !config.NotifyNearbyZone) return;

        Layout.Divider(Theme.GapS);

        if (anyChannel)
            Row(l.CfgNotifMyWorld, null, () => config.NotifyMyWorld, v => config.NotifyMyWorld = v);

        Row(l.CfgNotifLanguageFilter, l.CfgNotifLanguageFilterHint,
            () => config.NotifyRpLanguageFilter, v => config.NotifyRpLanguageFilter = v);
    }

    // ─── Notifications d'événements ───────────────────────────────────────────

    private void DrawEventNotifications(Loc l)
    {
        using var card = Card.Begin("set_eventnotif", interactive: false);

        Layout.SectionHeader(l.CfgEventNotifHeader, Icons.Events);

        Row(l.CfgEventNotifScreen, null,
            () => config.NotifyEventStartDalamud, v => config.NotifyEventStartDalamud = v,
            () => Plugin.Framework.RunOnFrameworkThread(() =>
                Plugin.ToastGui.ShowNormal(
                    string.Format(l.NotifEventStartScreen, SampleEvent, SampleVenue),
                    new ToastOptions { Speed = ToastSpeed.Slow })));

        Row(l.CfgEventNotifChat, l.CfgEventNotifHint,
            () => config.NotifyEventStartChat, v => config.NotifyEventStartChat = v,
            () => Plugin.ChatGui.Print(new SeStringBuilder()
                .AddUiForeground(32).AddText("[Eorzea Events] ").AddUiForegroundOff()
                .AddText(string.Format(l.NotifEventStartChat,
                    $"{SampleVenue} - {SampleEvent} | 21:00 → 00:00 | {SampleServer}"))
                .Build()));
    }

    // ─── Ma session ───────────────────────────────────────────────────────────

    private void DrawSession(Loc l)
    {
        using var card = Card.Begin("set_session", interactive: false);

        Layout.SectionHeader(l.CfgSessionHeader, Icons.Edit);

        Row(l.CfgSuggestOnTag,   null, () => config.SuggestSessionOnRpTag,  v => config.SuggestSessionOnRpTag = v);
        Row(l.CfgAlertZone,      null, () => config.AlertOnZoneChange,      v => config.AlertOnZoneChange = v);
        Row(l.CfgAlertTag,       null, () => config.AlertOnRpTagRemoved,    v => config.AlertOnRpTagRemoved = v);
        Row(l.CfgAlertExpiry,    null, () => config.AlertOnSessionExpiring, v => config.AlertOnSessionExpiring = v);
        Row(l.CfgAutoRefreshPos, null, () => config.AutoRefreshPosition,    v => config.AutoRefreshPosition = v);

        // Le réglage reste visible sans Lifestream, mais dit alors pourquoi il
        // ne produit rien : plus utile qu'une ligne qui disparaît sans raison.
        var installed = Plugin.Lifestream.IsAvailable;
        Row(l.CfgTravel, installed ? l.CfgTravelHint : l.CfgTravelMissing,
            () => config.EnableLifestreamTravel,
            v => config.EnableLifestreamTravel = v,
            disabled: !installed);
    }

    // ─── Profil RP ────────────────────────────────────────────────────────────

    private void DrawRpProfile(Loc l)
    {
        using var card = Card.Begin("set_rpprofile", interactive: false);

        Layout.SectionHeader(l.CfgRpProfileHeader, Icons.Profile);

        // La disponibilité se propage au serveur, en plus d'être enregistrée.
        Row(l.RpAvailableEnable, null,
            () => Plugin.CurrentCharacterAvailable,
            v => { Plugin.CurrentCharacterAvailable = v; Plugin.PublishAvailability(v); });

        Row(l.CfgRpIndicator, null,
            () => config.ShowRpAvailableIndicator, v => config.ShowRpAvailableIndicator = v);

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw(l.RpProfileSetup, BtnTone.Secondary, BtnSize.Medium, Icons.Edit))
            Plugin.OpenRpProfileWizard();
    }

    // ─── Barre de statut du serveur ───────────────────────────────────────────

    private void DrawStatusBar(Loc l)
    {
        using var card = Card.Begin("set_dtr", interactive: false);

        Layout.SectionHeader(l.CfgDtrHeader, Icons.World);

        var changed = Row(l.CfgDtrRp,      null, () => config.ShowDtrRp,      v => config.ShowDtrRp = v);
        changed    |= Row(l.CfgDtrEvents,  null, () => config.ShowDtrEvents,  v => config.ShowDtrEvents = v);
        changed    |= Row(l.CfgDtrRpAvail, null, () => config.ShowDtrRpAvail, v => config.ShowDtrRpAvail = v);

        if (changed) Plugin.ApplyDtrVisibility();
    }

    // ─── Langue ───────────────────────────────────────────────────────────────

    private void DrawLanguage(Loc l)
    {
        using var card = Card.Begin("set_language", interactive: false);

        Layout.SectionHeader(l.CfgLangHeader, Icons.Language);

        var options = new[] { l.CfgLangAuto, l.CfgLangFr, l.CfgLangEn };
        var current = (int)config.Language;

        ImGui.SetNextItemWidth(Card.FullWidth);
        if (!ImGui.Combo("##language", ref current, options, options.Length)) return;

        config.Language = (PluginLanguage)current;
        config.Save();
    }

    // ─── À propos ─────────────────────────────────────────────────────────────

    private void DrawAbout(Loc l)
    {
        using var card = Card.Begin("set_about", interactive: false);

        Layout.SectionHeader(l.CfgAboutHeader, Icons.Info);

        Text.Small($"v{Plugin.VersionLabel()}");
        Layout.Spacer(Theme.GapS);

        Row(l.CfgWhatsNewAuto, l.CfgWhatsNewAutoHint,
            () => config.AutoOpenWhatsNew, v => config.AutoOpenWhatsNew = v);

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw(l.CfgWhatsNew, BtnTone.Secondary, BtnSize.Medium, Icons.Sparkle))
            Plugin.OpenWhatsNew();
    }

    // ─── Développement ────────────────────────────────────────────────────────

#if DEBUG
    private void DrawDeveloper()
    {
        using var card = Card.Begin("set_dev", interactive: false,
                                    background: Theme.Mix(Theme.BgSurface, Theme.Danger, 0.12f),
                                    border: Theme.Alpha(Theme.Danger, 0.4f),
                                    accent: Theme.Danger);

        Layout.SectionHeader("Développement", Icons.Debug, tone: Theme.Danger);
        Text.Small("Présent uniquement dans les compilations de débogage.");
        Layout.Spacer(Theme.GapS);

        if (Inputs.Field("##baseurl", "URL de l'API", ref _baseUrl, 256))
        {
            config.BaseUrl = _baseUrl.TrimEnd('/');
            config.Save();
            Plugin.RebuildApiClient();
        }

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw("Production", BtnTone.Secondary, BtnSize.Medium)) SetBaseUrl("https://eorzea.events");
        ImGui.SameLine(0f, Theme.S(Theme.GapXs));
        if (Btn.Draw("Local", BtnTone.Secondary, BtnSize.Medium)) SetBaseUrl("http://localhost:3000");

        Layout.Divider(Theme.GapS);
        if (!Btn.Draw("Simuler la migration", BtnTone.Danger, BtnSize.Medium, Icons.Refresh)) return;

        config.MigrationNoticeSeen = false;
        config.CharacterTokens.Clear();
        config.Save();
        Plugin.OpenSetup(migration: true);
    }

    private void SetBaseUrl(string url)
    {
        _baseUrl       = url;
        config.BaseUrl = url;
        config.Save();
        Plugin.RebuildApiClient();
    }
#endif

    // ─── Raccourci ────────────────────────────────────────────────────────────

    /// <summary>
    /// Ligne de réglage enregistrée dès le changement, avec un bouton d'essai
    /// facultatif. Les réglages étant des propriétés, ils ne peuvent pas être
    /// passés par référence : d'où les accesseurs.
    /// </summary>
    private bool Row(string label, string? description,
                     Func<bool> get, Action<bool> set, Action? test = null,
                     bool disabled = false)
    {
        var value   = get();
        var changed = Inputs.ToggleRow(label, ref value, description, disabled: disabled);

        if (changed)
        {
            set(value);
            config.Save();
        }

        if (test != null)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            if (Btn.Draw(Plugin.L.CfgTest, BtnTone.Ghost, BtnSize.Small, id: $"test_{label}"))
                test();
        }

        return changed;
    }
}

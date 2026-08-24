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

        // La discussion vient juste après l'identité du personnage, avant les
        // cartes de notification et de barre de statut : c'est ce qu'un rôliste
        // vient régler, et la reléguer en bas de page revenait à ne pas la
        // proposer du tout.
        DrawChat(l);

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

        // Même chemin que la fiche RP et la barre de statut : publication,
        // rétablissement si le serveur refuse, et barre de statut remise à jour.
        Row(l.RpAvailableEnable, l.RpAvailableEnableHint,
            () => Plugin.CurrentCharacterAvailabilityWanted,
            Plugin.SetRpAvailability);

        Row(l.CfgRpIndicator, null,
            () => config.ShowRpAvailableIndicator, v => config.ShowRpAvailableIndicator = v);

        // Forme de l'écran d'édition, pas contenu de la fiche : il vit avec les
        // réglages du profil parce que c'est là qu'on le cherchera.
        Row(l.CfgRpProfileTabs, l.CfgRpProfileTabsHint,
            () => config.RpProfileTabs, v => config.RpProfileTabs = v);

        Layout.Divider(Theme.GapS);

        Row(l.CfgRpTooltip, l.CfgRpTooltipHint,
            () => config.RpTooltipEnabled, v => config.RpTooltipEnabled = v);

        // Les réglages fins ne s'affichent que si l'infobulle est active : sans
        // elle, ils n'ont rien à régler et n'ajoutent que du bruit.
        if (config.RpTooltipEnabled)
        {
            Row(l.CfgRpTooltipHover, l.CfgRpTooltipHoverHint,
                () => config.RpTooltipOnHover, v => config.RpTooltipOnHover = v);

            Layout.Spacer(Theme.GapS);
            Text.Small(l.CfgRpTooltipModifier);

            var keys = new[] { l.CfgRpTooltipModNone, l.CfgRpTooltipModCtrl, l.CfgRpTooltipModAlt };
            var key  = (int)config.RpTooltipModifier;

            ImGui.SetNextItemWidth(Card.FullWidth);
            if (ImGui.Combo("##rptooltipkey", ref key, keys, keys.Length))
            {
                config.RpTooltipModifier = (RpTooltipKey)key;
                config.Save();
            }

            Text.Small(l.CfgRpTooltipModifierHint, Theme.TextFaint);

            Layout.Spacer(Theme.GapS);

            // Consentement du lecteur, jamais de l'auteur : décoché, l'infobulle
            // dit le marquage mais n'en montre pas le contenu.
            Row(l.CfgRpNsfwShow, l.CfgRpNsfwShowHint,
                () => config.ShowNsfwProfiles, v => config.ShowNsfwProfiles = v);
        }

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw(l.RpProfileSetup, BtnTone.Secondary, BtnSize.Medium, Icons.Edit))
            Plugin.OpenRpProfileWizard();
    }

    // ─── Discussion ───────────────────────────────────────────────────────────

    /// <summary>
    /// Facilités de discussion, à la manière de Total RP.
    ///
    /// La carte porte un accent quand le module tourne : c'est le seul réglage
    /// de la page qui change l'apparence du jeu en continu, et savoir d'un coup
    /// d'œil s'il est actif vaut mieux qu'aller relire une case.
    /// </summary>
    private void DrawChat(Loc l)
    {
        var on = config.ChatFormatEnabled;

        using var card = Card.Begin("set_chat", interactive: false,
                                    accent: on ? Theme.Accent : null);

        Layout.SectionHeader(l.CfgChatHeader, Icons.Chat);

        Row(l.CfgChatEnabled, l.CfgChatEnabledHint,
            () => config.ChatFormatEnabled, v => config.ChatFormatEnabled = v);

        // État en toutes lettres sous l'interrupteur. Les cases qui suivent
        // restent visibles même éteintes : les masquer laissait croire que le
        // module tenait en une case, et cachait que tout dépend d'elle.
        Text.Small(on ? l.CfgChatOn : l.CfgChatOff, on ? Theme.Online : Theme.TextFaint);

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatEmote, l.CfgChatEmoteHint,
                () => config.ChatFormatEmote, v => config.ChatFormatEmote = v);

        if (on && config.ChatFormatEmote)
        {
            var styles = new[] { l.CfgChatEmoteStyleStars, l.CfgChatEmoteStyleAngle, l.CfgChatEmoteStyleBoth };
            var style  = (int)config.ChatEmoteStyle;

            ImGui.SetNextItemWidth(Card.FullWidth);
            if (ImGui.Combo("##chatemotestyle", ref style, styles, styles.Length))
            {
                config.ChatEmoteStyle = (ChatEmoteStyle)style;
                config.Save();
            }

            ChatColorRow("emote", Chat.ChatPalette.EmoteDefault,
                         () => config.ChatEmoteColor, v => config.ChatEmoteColor = v);
        }

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatOoc, l.CfgChatOocHint,
                () => config.ChatFormatOoc, v => config.ChatFormatOoc = v);
        if (on && config.ChatFormatOoc)
            ChatColorRow("ooc", Chat.ChatPalette.OocDefault,
                         () => config.ChatOocColor, v => config.ChatOocColor = v);

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatSpeech, l.CfgChatSpeechHint,
                () => config.ChatFormatSpeech, v => config.ChatFormatSpeech = v);
        if (on && config.ChatFormatSpeech)
            ChatColorRow("speech", Chat.ChatPalette.SpeechDefault,
                         () => config.ChatSpeechColor, v => config.ChatSpeechColor = v);

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatRpNames, l.CfgChatRpNamesHint,
                () => config.ChatRpNames, v => config.ChatRpNames = v);
        if (on && config.ChatRpNames)
            ChatColorRow("rpname", Chat.ChatPalette.NameDefault,
                         () => config.ChatRpNameColor, v => config.ChatRpNameColor = v);

        // Le détail des canaux et les jetons de saisie ne servent qu'une fois le
        // module en marche : les afficher éteints allongerait la carte sans rien
        // apprendre, alors que les motifs ci-dessus disent, eux, ce que fait le
        // module.
        if (!on) return;

        Layout.Divider(Theme.GapS);

        Text.Small(l.CfgChatChannels);
        Text.Small(l.CfgChatChannelsHint, Theme.TextFaint);
        Layout.Spacer(Theme.GapXs);

        Row(l.CfgChatChanSay,         null, () => config.ChatChannelSay,         v => config.ChatChannelSay = v);
        Row(l.CfgChatChanTell,        null, () => config.ChatChannelTell,        v => config.ChatChannelTell = v);
        Row(l.CfgChatChanShout,       null, () => config.ChatChannelShout,       v => config.ChatChannelShout = v);
        Row(l.CfgChatChanYell,        null, () => config.ChatChannelYell,        v => config.ChatChannelYell = v);
        Row(l.CfgChatChanParty,       null, () => config.ChatChannelParty,       v => config.ChatChannelParty = v);
        Row(l.CfgChatChanLinkshell,   null, () => config.ChatChannelLinkshell,   v => config.ChatChannelLinkshell = v);
        Row(l.CfgChatChanFreeCompany, null, () => config.ChatChannelFreeCompany, v => config.ChatChannelFreeCompany = v);
        Row(l.CfgChatChanEmote,       null, () => config.ChatChannelEmote,       v => config.ChatChannelEmote = v);

        Layout.Divider(Theme.GapS);

        Text.Small(l.CfgChatTokens);
        Text.Small(l.CfgChatTokensHint, Theme.TextFaint);
    }

    /// <summary>
    /// Case fille du module de discussion.
    ///
    /// Cocher une fille alors que l'interrupteur général est éteint l'allume par
    /// la même occasion. Une case cochée qui ne fait rien est un piège, et c'est
    /// exactement ce qui a fait croire que le remplacement des noms RP était
    /// cassé : le réglage était bien coché, mais le module n'écoutait pas le
    /// chat. Décocher ne coupe rien en retour, éteindre le module restant
    /// l'affaire de l'interrupteur.
    /// </summary>
    private void ChatRow(string label, string? hint, Func<bool> get, Action<bool> set)
    {
        if (!Row(label, hint, get, set)) return;
        if (!get() || config.ChatFormatEnabled) return;

        config.ChatFormatEnabled = true;
        config.Save();
    }

    /// <summary>
    /// Nuancier d'une couleur de chat.
    ///
    /// Les pastilles viennent de la feuille UIColor du jeu, seules teintes que
    /// le chat sache afficher : un sélecteur RVB libre laisserait choisir des
    /// couleurs qui ne sortiraient jamais à l'écran. La première pastille rend
    /// la main au plugin, qui reprend la teinte de son interface.
    /// </summary>
    private void ChatColorRow(string id, Vector4 fallback, Func<ushort> get, Action<ushort> set)
    {
        Layout.Spacer(Theme.GapXs);
        Text.Small(Plugin.L.CfgChatColor, Theme.TextFaint);

        var current = get();
        var size    = new Vector2(ImGui.GetFrameHeight() * 0.75f);
        var spacing = Theme.S(Theme.GapXs);

        if (Swatch($"##chatcol_{id}_off", fallback, current == Chat.ChatPalette.Off, size,
                   Plugin.L.CfgChatColorDefault))
        {
            set(Chat.ChatPalette.Off);
            config.Save();
        }

        var keys  = Chat.ChatPalette.Keys;
        var perRow = 12;

        for (var i = 0; i < keys.Count; i++)
        {
            // Retour à la ligne tous les douze : au-delà, la grille déborde de
            // la carte sur les interfaces les plus étroites.
            if ((i + 1) % perRow != 0) ImGui.SameLine(0f, spacing);

            var key = keys[i];
            if (!Swatch($"##chatcol_{id}_{key}", Chat.ChatPalette.Color(key), current == key, size)) continue;

            set(key);
            config.Save();
        }
    }

    /// <summary>Pastille de couleur, cerclée quand elle est celle retenue.</summary>
    private static bool Swatch(string id, Vector4 color, bool selected, Vector2 size, string? tooltip = null)
    {
        var value   = color;
        var clicked = ImGui.ColorButton(id, in value,
            ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop,
            size);

        if (selected)
            ImGui.GetWindowDrawList().AddRect(
                ImGui.GetItemRectMin() - new Vector2(2f),
                ImGui.GetItemRectMax() + new Vector2(2f),
                ImGui.ColorConvertFloat4ToU32(Theme.Text), 2f, ImDrawFlags.None, 2f);

        if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);

        return clicked;
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

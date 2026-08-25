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

    // Sélecteur de couleur du chat : identifiant du réglage déplié (un seul à la
    // fois), et teinte en cours de manipulation. Elle ne vit que le temps du
    // dépliage : la configuration ne retient que la clé de palette, seule chose
    // que le chat sache afficher.
    private string? _colorPickerOpen;
    private readonly Dictionary<string, Vector4> _colorDrafts = [];

    // Identifiants d'onglets. Constantes plutôt que chaînes libres : ils servent
    // à la fois à construire la barre et à l'aiguillage, et une faute de frappe
    // entre les deux se verrait à l'écran, pas à la compilation.
    private const string TabCharacters    = "characters";
    private const string TabChat          = "chat";
    private const string TabRp            = "rp";
    private const string TabNotifications = "notif";
    private const string TabMisc          = "misc";

    private string _tab = TabCharacters;

    /// <summary>
    /// Réglages, rangés en onglets.
    ///
    /// Les dix cartes tenaient jusqu'ici dans une seule colonne défilante, où
    /// trouver un réglage demandait de la parcourir en entier. Elles sont
    /// regroupées par ce que l'on vient régler, et non par ordre d'apparition.
    ///
    /// La discussion garde un onglet à elle : c'est le module le plus fourni de
    /// la page, et le ranger avec le reste reviendrait à ne pas le proposer.
    ///
    /// La barre d'onglets est dessinée hors du Child défilant, sans quoi elle
    /// disparaîtrait dès le premier défilement.
    /// </summary>
    public void Draw()
    {
        var l = Plugin.L;

        Tabs.Tab[] tabs =
        [
            new(TabCharacters,    l.CfgTabCharacters,    Icons.Character),
            new(TabChat,          l.CfgTabChat,          Icons.Chat),
            new(TabRp,            l.CfgTabRp,            Icons.RpLive),
            new(TabNotifications, l.CfgTabNotifications, Icons.Notification),
            new(TabMisc,          l.CfgTabMisc,          Icons.Misc),
        ];

        _tab = Tabs.Draw("settingstabs", tabs, _tab, Theme.Accent);

        Layout.Spacer(Theme.GapS);

        using var scroll = ImRaii.Child("##settingsscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        switch (_tab)
        {
            case TabChat:
                DrawChat(l);
                break;

            case TabRp:
                DrawRpProfile(l);
                DrawRpTooltip(l);
                DrawRpNotifications(l);
                break;

            case TabNotifications:
                DrawEventNotifications(l);
                DrawSession(l);
                DrawStatusBar(l);
                break;

            case TabMisc:
                DrawLanguage(l);
                DrawAbout(l);
#if DEBUG
                DrawDeveloper();
#endif
                break;

            // Les personnages liés ouvrent la page : c'est le seul réglage sans
            // lequel rien d'autre ne fonctionne. C'est aussi le repli de Tabs
            // quand l'onglet retenu n'existe plus.
            default:
                DrawCharacters(l);
                break;
        }

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

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw(l.RpProfileSetup, BtnTone.Secondary, BtnSize.Medium, Icons.Edit))
            Plugin.OpenRpProfileWizard();
    }

    // ─── Infobulle de survol ──────────────────────────────────────────────────

    /// <summary>
    /// Réglages de l'infobulle de ciblage.
    ///
    /// Carte à part, et non quelques lignes au bas du profil RP : c'est le
    /// réglage que l'on vient chercher quand la bulle gêne, et il était introuvable
    /// noyé sous le reste.
    /// </summary>
    private void DrawRpTooltip(Loc l)
    {
        using var card = Card.Begin("set_rptooltip", interactive: false);

        Layout.SectionHeader(l.CfgRpTooltipCard, Icons.Tooltip);

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
                         () => config.ChatEmoteColor, v => config.ChatEmoteColor = v,
                         () => config.ChatEmoteColorCustom, v => config.ChatEmoteColorCustom = v);
        }

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatOoc, l.CfgChatOocHint,
                () => config.ChatFormatOoc, v => config.ChatFormatOoc = v);
        if (on && config.ChatFormatOoc)
            ChatColorRow("ooc", Chat.ChatPalette.OocDefault,
                         () => config.ChatOocColor, v => config.ChatOocColor = v,
                         () => config.ChatOocColorCustom, v => config.ChatOocColorCustom = v);

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatSpeech, l.CfgChatSpeechHint,
                () => config.ChatFormatSpeech, v => config.ChatFormatSpeech = v);
        if (on && config.ChatFormatSpeech)
            ChatColorRow("speech", Chat.ChatPalette.SpeechDefault,
                         () => config.ChatSpeechColor, v => config.ChatSpeechColor = v,
                         () => config.ChatSpeechColorCustom, v => config.ChatSpeechColorCustom = v);

        Layout.Divider(Theme.GapS);

        ChatRow(l.CfgChatRpNames, l.CfgChatRpNamesHint,
                () => config.ChatRpNames, v => config.ChatRpNames = v);
        // Pas de nuancier ici : la couleur d'un nom RP est celle de la fiche de
        // son propriétaire, et laisser choisir une couleur qui ne s'appliquerait
        // qu'aux fiches sans accent revenait à proposer un réglage sans effet
        // visible.
        if (on && config.ChatRpNames)
            Text.Small(l.CfgChatRpNameAccent, Theme.TextFaint);

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
    /// le chat sache afficher. La grille reste courte : elle donne en un clic
    /// les couleurs franches, le sélecteur qui la suit se charge de la nuance.
    /// La première pastille rend la main au plugin, qui reprend la teinte de son
    /// interface.
    /// </summary>
    private void ChatColorRow(string id, Vector4 fallback, Func<ushort> get, Action<ushort> set,
                              Func<uint> getCustom, Action<uint> setCustom)
    {
        Layout.Spacer(Theme.GapXs);
        Text.Small(Plugin.L.CfgChatColor, Theme.TextFaint);

        var current = get();
        var custom  = Chat.ChatPalette.Decode(getCustom());
        var size    = new Vector2(ImGui.GetFrameHeight() * 0.75f);
        var spacing = Theme.S(Theme.GapXs);

        // La pastille montre la couleur que le chat rendra, et non celle du
        // thème du plugin : le jeu ne sait afficher que sa propre palette, et
        // promettre le jaune exact de l'interface pour le voir sortir autrement
        // est ce qui nous a été signalé.
        var auto = Chat.ChatPalette.Rendered(fallback);

        // Une teinte libre est rendue par une clé de la palette : sans cette
        // réserve, la pastille correspondante s'afficherait comme le choix retenu
        // et la couleur personnalisée passerait pour un choix de nuancier.
        if (Swatch($"##chatcol_{id}_off", auto, custom == null && current == Chat.ChatPalette.Off, size,
                   Plugin.L.CfgChatColorDefault))
        {
            set(Chat.ChatPalette.Off);
            setCustom(0);
            config.Save();
            SyncColorDraft(id, auto);
        }

        var keys   = Chat.ChatPalette.Keys;
        var perRow = 12;

        // La pastille « automatique » occupe déjà la première place de la
        // rangée : compter les pastilles réellement posées, et non le rang dans
        // la boucle, sinon le retour à la ligne se décale d'un cran et la
        // dernière colonne déborde de la carte.
        var drawn = 1;

        for (var i = 0; i < keys.Count; i++)
        {
            if (drawn % perRow != 0) ImGui.SameLine(0f, spacing);
            drawn++;

            var key   = keys[i];
            var color = Chat.ChatPalette.Color(key);
            if (!Swatch($"##chatcol_{id}_{key}", color, custom == null && current == key, size)) continue;

            // Choisir au nuancier abandonne la teinte libre : c'est un choix qui
            // en remplace un autre, pas qui s'y ajoute.
            set(key);
            setCustom(0);
            config.Save();
            SyncColorDraft(id, color);
        }

        ChatColorPicker(id, fallback, get, set, getCustom, setCustom);
    }

    /// <summary>
    /// Aligne le sélecteur ouvert sur une couleur choisie à la pastille, pour que
    /// son aperçu ne montre pas autre chose que le réglage en vigueur.
    /// </summary>
    private void SyncColorDraft(string id, Vector4 color)
    {
        if (_colorPickerOpen == id) _colorDrafts[id] = color;
    }

    /// <summary>
    /// Sélecteur de couleur libre pour un réglage de chat.
    ///
    /// Le chat ne sait afficher qu'une ligne de la feuille UIColor : la teinte
    /// choisie ici est donc ramenée à la plus proche de la palette du jeu. Plutôt
    /// que de refuser le choix libre pour cette raison, on le montre tel qu'il
    /// sortira, côte à côte avec ce qui a été demandé. Un joueur qui voit les
    /// deux pastilles comprend l'écart ; un joueur à qui l'on cache l'ajustement
    /// croit à un bug.
    ///
    /// Le panneau est déplié dans la carte plutôt qu'ouvert en fenêtre flottante :
    /// c'est ce que fait déjà le reste de la page, et le plugin n'ouvre aucune
    /// autre fenêtre surgissante.
    /// </summary>
    private void ChatColorPicker(string id, Vector4 fallback, Func<ushort> get, Action<ushort> set,
                                 Func<uint> getCustom, Action<uint> setCustom)
    {
        var l      = Plugin.L;
        var open   = _colorPickerOpen == id;
        var custom = Chat.ChatPalette.Decode(getCustom());

        Layout.Spacer(Theme.GapXs);

        // Taille ajustée au contenu : la petite taille est de largeur fixe et
        // rognait le libellé.
        if (Btn.Draw(l.CfgChatColorCustom, BtnTone.Secondary, BtnSize.Medium,
                     id: $"chatcolopen_{id}"))
        {
            if (open)
            {
                _colorPickerOpen = null;
                return;
            }

            // La teinte enregistrée prime sur la couleur rendue : rouvrir les
            // réglages doit remettre sous les yeux ce qui a été demandé, et non
            // la couleur du nuancier qui s'en est approchée.
            //
            // Sans teinte enregistrée, on repart de la couleur effectivement
            // rendue : c'est ce que le joueur a sous les yeux dans son chat, et
            // c'est un point fixe du rapprochement, donc rouvrir sans rien
            // toucher ne déplace pas le réglage.
            _colorDrafts[id] = custom
                            ?? Chat.ChatPalette.Color(Chat.ChatPalette.Resolve(get(), fallback));
            _colorPickerOpen = id;
            open = true;
        }

        // Rappel de la teinte retenue, à côté du bouton : replié, le sélecteur ne
        // montrait plus rien, et le réglage semblait perdu.
        if (custom is { } chosen)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            Swatch($"##chatcolcustom_{id}", chosen, true,
                   new Vector2(ImGui.GetFrameHeight() * 0.75f), l.CfgChatColorPicked);
        }

        if (!open) return;

        var draft = _colorDrafts.TryGetValue(id, out var held) ? held : fallback;

        Layout.Spacer(Theme.GapXs);

        // Le sélecteur est carré : sa hauteur suit la largeur qu'on lui donne, et
        // l'étendre à toute la carte remplissait la fenêtre entière. Cette
        // largeur-là suffit à viser une teinte au doigt.
        ImGui.SetNextItemWidth(Theme.S(170f));
        if (ImGui.ColorPicker4($"##chatcolwheel_{id}", ref draft,
                ImGuiColorEditFlags.NoAlpha
              | ImGuiColorEditFlags.NoSidePreview   // l'aperçu ci-dessous en dit plus
              | ImGuiColorEditFlags.NoSmallPreview
              | ImGuiColorEditFlags.DisplayHex))
        {
            _colorDrafts[id] = draft;

            // La teinte demandée est enregistrée à chaque mouvement, elle : c'est
            // elle que l'on veut retrouver en rouvrant, et elle change à chaque
            // pixel parcouru là où la clé de palette, elle, reste longtemps la
            // même. L'écriture du fichier reste conditionnée au changement de
            // clé, pour ne pas le réécrire en continu pendant le glissement.
            setCustom(Chat.ChatPalette.Encode(draft));

            var picked = Chat.ChatPalette.Nearest(draft);
            if (picked != get())
            {
                set(picked);
                config.Save();
            }
        }

        // Filet indispensable : une teinte peut bouger longuement sans jamais
        // changer de clé de palette, et le bloc ci-dessus n'écrirait alors rien.
        // Elle ne vivrait qu'en mémoire, c'est-à-dire jusqu'à la fermeture de la
        // fenêtre, soit exactement le défaut que ces lignes corrigent.
        if (ImGui.IsItemDeactivatedAfterEdit()) config.Save();

        var rendered = Chat.ChatPalette.Rendered(draft);
        var preview  = new Vector2(ImGui.GetFrameHeight());

        Layout.Spacer(Theme.GapXs);

        Swatch($"##chatcolpicked_{id}", draft, false, preview, l.CfgChatColorPicked);
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        Swatch($"##chatcolshown_{id}", rendered, false, preview, l.CfgChatColorRendered);

        Layout.Spacer(Theme.GapXs);
        Text.Small(l.CfgChatColorHint, Theme.TextFaint);
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

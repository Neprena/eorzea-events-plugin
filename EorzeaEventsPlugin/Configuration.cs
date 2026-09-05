using Dalamud.Configuration;

namespace EorzeaEventsPlugin;

public enum PluginLanguage { Auto, French, English }

/// <summary>
/// Touche à maintenir pour faire apparaître l'infobulle de ciblage. Sert à qui
/// joue en ville et ne veut pas d'une bulle qui suit chaque passant.
/// </summary>
public enum RpTooltipKey { None, Ctrl, Alt }

/// <summary>
/// Jeu de délimiteurs reconnus comme une emote dans le chat.
///
/// Les deux écoles cohabitent sur les serveurs francophones : les astérisques
/// venus des messageries, les chevrons venus des vieux salons. Trancher pour
/// l'une reviendrait à ne rien colorer pour la moitié des joueurs.
/// </summary>
public enum ChatEmoteStyle { Stars, Angles, Both }

/// <summary>
/// Token API lié à un personnage FFXIV spécifique (workflow web-link).
/// Le token est de la forme `ec_*`. Le plugin sélectionne automatiquement
/// le bon token selon le perso connecté in-game.
/// </summary>
[Serializable]
public class CharacterTokenEntry
{
    public string CharacterName { get; set; } = string.Empty;
    public int    WorldId       { get; set; }
    public string WorldName     { get; set; } = string.Empty;
    public string Token         { get; set; } = string.Empty;
    public DateTime LinkedAt    { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ContentId Dalamud du personnage (identité stable, survit au rename).
    /// 0 = entrée legacy liée avant cette fonctionnalité (backfillée à la volée).
    /// </summary>
    public ulong ContentId { get; set; }
}

/// <summary>
/// Copie locale de la fiche RP d'un personnage.
///
/// Sert à afficher la fiche sans attendre le réseau, et à la renvoyer telle
/// quelle quand seul un champ change. Les listes sont stockées en JSON, comme
/// côté serveur, pour éviter une conversion à chaque échange.
/// </summary>
[Serializable]
public class RpProfileCache
{
    public string  RpLevel       { get; set; } = "casual";
    public string  ApproachMode  { get; set; } = "come_to_me";
    public string? ContactMode   { get; set; }
    public string? SessionLength { get; set; }
    public string  Languages     { get; set; } = "[\"fr\"]";
    public string? Themes        { get; set; }
    public string? AvoidThemes   { get; set; }

    public string? RpName       { get; set; }
    public string? Nickname     { get; set; }
    public string? Pronouns     { get; set; }
    public string? Race         { get; set; }
    public string? Age          { get; set; }
    public string? Origin       { get; set; }
    public string? Occupation   { get; set; }
    public string? Appearance   { get; set; }
    public string? Personality  { get; set; }
    public string? Background   { get; set; }
    public string? Hooks        { get; set; }

    /// <summary>
    /// Coup d'œil, en chaîne JSON comme les autres listes du cache : un tableau
    /// [{ icon, title, body, active }]. Nullable comme les ajouts tardifs, pour
    /// distinguer « jamais synchronisé » de « aucun emplacement ».
    /// </summary>
    public string? Glances      { get; set; }
    public string? CurrentQuest { get; set; }
    public string? Limits       { get; set; }

    /// <summary>
    /// Instant présent : statut du moment et état de jeu. Nullables comme les
    /// autres ajouts tardifs, et pour la même raison : `null` dit « jamais
    /// synchronisé », une chaîne vide dirait « statut effacé », et un cache
    /// antérieur à ces champs ferait sinon afficher « hors RP » à des fiches qui
    /// n'ont rien réglé de tel.
    /// </summary>
    public string? Currently { get; set; }
    public string? IcState   { get; set; }
    public string? Availability { get; set; }
    public string? ExternalUrl  { get; set; }

    /// <summary>
    /// Codes de sync, sous la forme brute stockée. Nullable comme les autres
    /// ajouts tardifs : `null` distingue « jamais synchronisé » de « aucun code ».
    /// </summary>
    public string? Syncshells { get; set; }

    public string? PortraitUrl { get; set; }
    public string? Height      { get; set; }
    public string? Build       { get; set; }
    public string? Marks       { get; set; }
    public string? Voice       { get; set; }
    public string? FreeCompany { get; set; }
    public string? Allegiance  { get; set; }
    public string? Deity       { get; set; }
    public string? Quote       { get; set; }

    public bool Nsfw     { get; set; }
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Reste de la confidentialité, mis en cache comme le reste de la fiche.
    ///
    /// Ceux-là manquaient, si bien qu'une fiche reconstituée depuis le cache
    /// affichait « indexation coupée, sections aux défauts » quels qu'aient été
    /// les réglages réels. L'écran mentait, et un enregistrement dans cet état
    /// les aurait imposés au serveur. Le null distingue « jamais synchronisé »
    /// de « réglé ainsi ».
    /// </summary>
    public bool?   SearchIndexable   { get; set; }
    public string? SectionVisibility { get; set; }

    /// <summary>Identifiant serveur du personnage, pour l'aperçu et les liens.</summary>
    public string? CharacterId { get; set; }

    public string? ThemeSongUrl { get; set; }

    /// <summary>Dernière synchronisation réussie avec le serveur.</summary>
    public DateTime FetchedAt { get; set; } = DateTime.MinValue;
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 5;

    /// <summary>
    /// Token API legacy (User.apiToken, préfixe ee_). Conservé pour la transition.
    /// Sera retiré dans une release future une fois tous les utilisateurs migrés.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Tokens API par personnage (préfixe ec_). Le plugin sélectionne automatiquement
    /// l'entrée correspondant au personnage actuellement connecté in-game.
    /// </summary>
    public List<CharacterTokenEntry> CharacterTokens { get; set; } = [];

    /// <summary>
    /// Trouve le token attaché au personnage donné (clé naturelle name+worldId).
    /// Retourne null si aucun token n'est lié.
    /// </summary>
    public CharacterTokenEntry? FindCharacterToken(string characterName, int worldId)
    {
        return CharacterTokens.Find(c =>
            string.Equals(c.CharacterName, characterName, StringComparison.Ordinal)
            && c.WorldId == worldId);
    }

    /// <summary>
    /// Trouve le token par ContentId (identité stable qui survit au rename).
    /// Retourne null si contentId vaut 0 ou si aucune entrée ne correspond.
    /// </summary>
    public CharacterTokenEntry? FindCharacterTokenByContentId(ulong contentId)
    {
        if (contentId == 0) return null;
        return CharacterTokens.Find(c => c.ContentId == contentId);
    }

    /// <summary>
    /// Sélectionne le meilleur token disponible pour le perso donné :
    /// 1. Token de personnage si lié.
    /// 2. Sinon, token legacy (ApiToken).
    /// 3. Sinon, chaîne vide.
    /// </summary>
    public string ResolveTokenForCharacter(string? characterName, int? worldId)
    {
        if (!string.IsNullOrEmpty(characterName) && worldId.HasValue)
        {
            var entry = FindCharacterToken(characterName, worldId.Value);
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Token))
                return entry.Token;
        }
        return ApiToken ?? string.Empty;
    }

    /// <summary>Identifiant anonyme unique généré automatiquement pour les heartbeats de présence.</summary>
    public string ClientId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// URL de base de l'API (sans slash final).
    /// La valeur par défaut dépend du build : Release → prod, Debug → localhost.
    /// Toujours modifiable via Config → Dev section (DEBUG uniquement).
    /// </summary>
    public string BaseUrl { get; set; } =
#if DEBUG
        "http://localhost:3000";
#else
        "https://eorzea.events";
#endif

    /// <summary>ID de la session RP en cours (null si aucune).</summary>
    public string? ActiveSessionId { get; set; }

    /// <summary>Alerte écran (toast natif FFXIV) pour les nouvelles sessions RP Live.</summary>
    public bool NotifyRpLiveScreen { get; set; } = true;

    /// <summary>Notifier quand une nouvelle session RP Live démarre (toast Dalamud, coin de l'écran).</summary>
    public bool NotifyRpLive { get; set; } = true;

    /// <summary>Annoncer les nouvelles sessions RP dans le chat du jeu.</summary>
    public bool NotifyRpLiveChat { get; set; } = true;

    /// <summary>Limiter les notifications au monde courant du joueur.</summary>
    public bool NotifyMyWorld { get; set; } = true;

    /// <summary>Limiter les notifications de RP ouverts à la langue du plugin.</summary>
    public bool NotifyRpLanguageFilter { get; set; } = true;

    /// <summary>Proposer de mettre à jour l'emplacement après un changement de zone ou un TP.</summary>
    public bool AlertOnZoneChange { get; set; } = true;

    /// <summary>Proposer de terminer la session quand le tag RP est retiré.</summary>
    public bool AlertOnRpTagRemoved { get; set; } = true;

    /// <summary>Proposer de prolonger ou d'arrêter la session quand elle est sur le point d'expirer.</summary>
    public bool AlertOnSessionExpiring { get; set; } = true;

    /// <summary>Mettre à jour automatiquement la position de la session RP active toutes les 5 min (sans propager au fil Discord).</summary>
    public bool AutoRefreshPosition { get; set; } = true;

    /// <summary>Proposer de démarrer une session quand le tag RP est activé sans session en cours.</summary>
    public bool SuggestSessionOnRpTag { get; set; } = true;

    /// <summary>Notifier (toast) quand une nouvelle session RP démarre dans la zone courante du joueur.</summary>
    public bool NotifyNearbyZone { get; set; } = true;

    /// <summary>Notifier quand un événement communautaire démarre via notification Dalamud.</summary>
    public bool NotifyEventStartDalamud { get; set; } = true;

    /// <summary>Notifier quand un événement communautaire démarre via message chat.</summary>
    public bool NotifyEventStartChat { get; set; } = true;

    /// <summary>Afficher l'entrée "RP" dans la barre de statut du serveur.</summary>
    public bool ShowDtrRp { get; set; } = true;

    /// <summary>Afficher l'entrée "Events" dans la barre de statut du serveur.</summary>
    public bool ShowDtrEvents { get; set; } = true;

    /// <summary>Afficher l'entrée de disponibilité RP (♦) dans la barre de statut du serveur.</summary>
    public bool ShowDtrRpAvail { get; set; } = true;

    /// <summary>Langue de l'interface du plugin (Auto = détection depuis le client FFXIV).</summary>
    public PluginLanguage Language { get; set; } = PluginLanguage.Auto;

    /// <summary>
    /// Proposer le bouton « Y aller » quand Lifestream est installé. Reste sans
    /// effet en son absence : le bouton ne s'affiche pas pour autant.
    /// </summary>
    public bool EnableLifestreamTravel { get; set; } = true;

    /// <summary>IDs d'événements masqués localement.</summary>
    public List<string> HiddenEventIds { get; set; } = [];

    /// <summary>IDs d'établissements masqués localement.</summary>
    public List<string> HiddenEstablishmentIds { get; set; } = [];

    // ─── Profil RP ───────────────────────────────────────────────────────────

    /// <summary>
    /// Édite la fiche RP en cinq onglets plutôt qu'en une seule page déroulante.
    ///
    /// Allumé par défaut : quatorze blocs bout à bout se parcourent au jugé, et
    /// ce qui se règle une fois par an y côtoie ce qu'on change tous les soirs.
    /// Qui préfère l'ancienne forme la retrouve d'une case dans les réglages.
    /// </summary>
    public bool RpProfileTabs { get; set; } = true;

    /// <summary>True si l'utilisateur a vu ou ignoré le wizard de migration vers les tokens de personnage.</summary>
    public bool MigrationNoticeSeen { get; set; } = false;

    /// <summary>L'annonce de la fonctionnalité "Profil RP & Disponibilité" a été vue.</summary>
    public bool RpAnnouncementSeen { get; set; } = false;

    /// <summary>Le wizard de profil RP a été complété au moins une fois.</summary>
    public bool RpProfileSetupDone { get; set; } = false;

    /// <summary>Afficher le marqueur ♦ sur les nameplates des joueurs disponibles pour du RP.</summary>
    public bool ShowRpAvailableIndicator { get; set; } = true;

    /// <summary>
    /// Afficher le nom RP à la place du nom de personnage sur les nameplates.
    ///
    /// Indépendant du marqueur ci-dessus, et volontairement plus large que lui :
    /// le titre « Dispo RP » ne revient qu'aux joueurs qui se sont déclarés, alors
    /// qu'un nom RP est simplement celui sous lequel son porteur joue. Le tag
    /// « Jeu de rôle » allumé suffit donc, dès lors que la fiche est publique et
    /// qu'un nom y figure.
    ///
    /// Le nom de personnage n'est jamais perdu : il reste sur la fiche, dans
    /// l'infobulle de survol et dans « Autour de moi », et le clic droit continue
    /// de viser le vrai joueur.
    /// </summary>
    public bool NameplateRpNames { get; set; } = true;

    // ─── Infobulle de ciblage ────────────────────────────────────────────────
    //
    // Trois de ces réglages ont suffi d'une valeur par défaut : une configuration
    // antérieure les reçoit telle qu'ils sont écrits ici, sans migration.
    // RpTooltipModifier fait exception, voir MigrateTooltipModifier.

    /// <summary>Afficher l'infobulle de fiche RP sur le joueur ciblé ou survolé.</summary>
    public bool RpTooltipEnabled { get; set; } = true;

    /// <summary>
    /// Autoriser le simple survol à déclencher l'infobulle. Décoché, seule la
    /// cible dure la fait apparaître, ce qui demande un clic délibéré.
    /// </summary>
    public bool RpTooltipOnHover { get; set; } = true;

    /// <summary>
    /// Touche à maintenir pour que l'infobulle apparaisse.
    ///
    /// Ctrl par défaut : livrée sans modificateur, l'infobulle suivait le curseur
    /// sur chaque passant, et une place bondée la faisait clignoter sans répit.
    /// Elle ne se montre donc plus que si on la demande.
    /// </summary>
    public RpTooltipKey RpTooltipModifier { get; set; } = RpTooltipKey.Ctrl;

    /// <summary>
    /// Consentement local à voir le contenu des fiches marquées sensibles.
    /// Désactivé par défaut : c'est au lecteur de le demander, jamais à l'auteur
    /// de la fiche de le lui imposer au détour d'un survol.
    /// </summary>
    public bool ShowNsfwProfiles { get; set; } = false;

    // ─── Facilités de discussion ──────────────────────────────────────────────
    //
    // Tout se joue à la réception, sur cette machine seulement : le message
    // envoyé n'est jamais retouché, et un interlocuteur sans le plugin voit ce
    // qui a été tapé.
    //
    // La mise en couleur est allumée d'emblée, interrupteur principal compris.
    // Elle était éteinte au départ par prudence, ce qui revenait à livrer le
    // module à personne : sans le savoir, on ne va pas le chercher, et l'unique
    // retour reçu portait sur des réglages cochés qui ne faisaient rien. Le
    // risque est faible et réversible, puisque seules les couleurs de sa propre
    // fenêtre changent, et seulement sur « dire » et les messages privés.
    // Substituer un NOM suit désormais la même règle : voir ChatRpNames.

    /// <summary>Interrupteur général du module : rien n'est touché tant qu'il est éteint.</summary>
    public bool ChatFormatEnabled { get; set; } = true;

    /// <summary>Colorer les emotes.</summary>
    public bool ChatFormatEmote { get; set; } = true;

    /// <summary>Délimiteurs tenus pour une emote.</summary>
    public ChatEmoteStyle ChatEmoteStyle { get; set; } = ChatEmoteStyle.Both;

    /// <summary>Colorer le hors jeu, entre parenthèses.</summary>
    public bool ChatFormatOoc { get; set; } = true;

    /// <summary>Colorer le discours, entre guillemets.</summary>
    public bool ChatFormatSpeech { get; set; } = true;

    // Les couleurs sont des clés de la feuille UIColor du jeu et non des valeurs
    // RVB : le chat ne sait pas afficher autre chose. Les défauts valent 0,
    // c'est-à-dire « couleur du canal », le temps que la palette soit lue dans
    // les données du jeu : un identifiant écrit en dur ici deviendrait faux au
    // premier remaniement de la feuille.
    public ushort ChatEmoteColor  { get; set; } = 0;
    public ushort ChatOocColor    { get; set; } = 0;
    public ushort ChatSpeechColor { get; set; } = 0;

    // Teinte libre choisie à la roue chromatique, encodée par ChatPalette.Encode,
    // 0 signifiant « aucune ». Le chat ne sait toujours afficher que la clé de
    // palette ci-dessus : ces valeurs ne servent qu'aux réglages.
    //
    // Sans elles, la teinte demandée disparaissait à la fermeture de la fenêtre.
    // Seule la couleur approchée subsistait, si bien qu'en rouvrant les réglages
    // plus rien ne disait qu'une couleur personnalisée avait été choisie, ni
    // laquelle.
    public uint ChatEmoteColorCustom  { get; set; } = 0;
    public uint ChatOocColorCustom    { get; set; } = 0;
    public uint ChatSpeechColorCustom { get; set; } = 0;


    /// <summary>
    /// Afficher le nom RP à la place du nom de personnage.
    ///
    /// Allumé, comme le nom RP sur les nameplates : livré éteint, ce réglage
    /// n'était trouvé par personne, et un nom RP renseigné restait invisible
    /// partout où on lit réellement du RP. Le nom de personnage reste lisible
    /// sur la fiche et dans l'infobulle, et le lien de joueur n'est pas touché :
    /// le clic droit vise toujours le vrai personnage.
    /// </summary>
    public bool ChatRpNames { get; set; } = true;

    // Canaux traités. Par défaut, « dire » et les messages privés seulement :
    // ce sont ceux où l'on joue. Les canaux de groupe et de compagnie libre
    // charrient de l'organisation et du contenu de jeu, qu'il n'y a aucune
    // raison de coloriser sans qu'on l'ait demandé.
    public bool ChatChannelSay         { get; set; } = true;
    public bool ChatChannelTell        { get; set; } = true;
    public bool ChatChannelShout       { get; set; } = false;
    public bool ChatChannelYell        { get; set; } = false;
    public bool ChatChannelParty       { get; set; } = false;
    public bool ChatChannelLinkshell   { get; set; } = false;
    public bool ChatChannelFreeCompany { get; set; } = false;
    public bool ChatChannelEmote       { get; set; } = false;

    // ─── Nouveautés ──────────────────────────────────────────────────────────

    /// <summary>
    /// Dernière version du plugin dont les nouveautés ont été affichées.
    /// null = jamais, ce qui vaut pour toute installation antérieure à cette
    /// fonctionnalité : la fenêtre s'ouvrira donc une fois après la mise à jour.
    /// </summary>
    public string? LastSeenVersion { get; set; }

    /// <summary>Ouvrir la fenêtre des nouveautés au premier lancement après une mise à jour.</summary>
    public bool AutoOpenWhatsNew { get; set; } = true;

    // ─── Ancien état, commun à tout le compte ────────────────────────────────
    //
    // Conservé pour la migration vers la version 4. Les nouveaux écrans passent
    // par les dictionnaires par personnage ci-dessous.

    public string? RpProfileLevel         { get; set; }
    public string? RpProfileApproachMode  { get; set; }
    public string? RpProfileLanguages     { get; set; }
    public string? RpProfileContactMode   { get; set; }
    public string? RpProfileSessionLength { get; set; }
    public string? RpProfileThemes        { get; set; }
    public bool    RpAvailabilityActive   { get; set; } = false;

    // ─── État par personnage ──────────────────────────────────────────────────
    //
    // Un rôliste joue plusieurs personnages : leur fiche et leur disponibilité
    // sont distinctes. La clé est « Nom@MondeId ».

    /// <summary>Cache de fiche, pour éviter un appel réseau à l'ouverture.</summary>
    public Dictionary<string, RpProfileCache> RpProfiles { get; set; } = [];

    /// <summary>Disponibilité effectivement publiée sur le site, par personnage.</summary>
    public Dictionary<string, bool> RpAvailability { get; set; } = [];

    /// <summary>
    /// Intention du joueur (« je veux être disponible »), par personnage.
    ///
    /// Distincte de la disponibilité publiée : celle-ci n'existe que tag « Jeu de
    /// rôle » actif. Sans cette mémoire, éteindre le tag effacerait le souhait du
    /// joueur, qui devrait se redéclarer disponible à chaque fois qu'il le
    /// rallume. L'intention survit au tag, la publication le suit.
    /// </summary>
    public Dictionary<string, bool> RpAvailabilityWanted { get; set; } = [];

    /// <summary>Clé d'un personnage dans les dictionnaires ci-dessus.</summary>
    public static string CharacterKey(string name, int worldId) => $"{name}@{worldId}";

    /// <summary>Fiche du personnage donné, ou null si elle n'est pas connue.</summary>
    public RpProfileCache? FindProfile(string? name, int? worldId)
    {
        if (string.IsNullOrEmpty(name) || worldId is not { } world) return null;
        return RpProfiles.TryGetValue(CharacterKey(name, world), out var profile) ? profile : null;
    }

    /// <summary>Disponibilité du personnage donné.</summary>
    public bool IsAvailable(string? name, int? worldId)
    {
        if (string.IsNullOrEmpty(name) || worldId is not { } world) return false;
        return RpAvailability.TryGetValue(CharacterKey(name, world), out var value) && value;
    }

    public void SetAvailable(string name, int worldId, bool available)
    {
        RpAvailability[CharacterKey(name, worldId)] = available;
        Save();
    }

    /// <summary>Le joueur souhaite-t-il être disponible avec ce personnage ?</summary>
    public bool IsAvailabilityWanted(string? name, int? worldId)
    {
        if (string.IsNullOrEmpty(name) || worldId is not { } world) return false;
        var key = CharacterKey(name, world);

        // Une configuration antérieure à l'intention n'a que la disponibilité
        // publiée : elle vaut alors intention, sans quoi la mise à jour ferait
        // sortir de la liste les personnages déjà déclarés disponibles.
        if (RpAvailabilityWanted.TryGetValue(key, out var wanted)) return wanted;
        return RpAvailability.TryGetValue(key, out var published) && published;
    }

    public void SetAvailabilityWanted(string name, int worldId, bool wanted)
    {
        RpAvailabilityWanted[CharacterKey(name, worldId)] = wanted;
        Save();
    }

    /// <summary>Proposer de se mettre indisponible à chaque reconnexion si disponible.</summary>
    public bool RpAskOnLogin { get; set; } = false;

    /// <summary>
    /// Reporte l'ancien état commun au compte sur chaque personnage lié.
    ///
    /// Sans cela, un joueur qui met le plugin à jour verrait sa fiche et sa
    /// disponibilité repartir de zéro. La migration reprend le raisonnement de
    /// la migration serveur : ce que l'on avait pour le compte valait pour tous
    /// ses personnages.
    /// </summary>
    public void MigrateToPerCharacter()
    {
        if (Version >= 4) return;

        var hadProfile = !string.IsNullOrEmpty(RpProfileLevel)
                         && !string.IsNullOrEmpty(RpProfileApproachMode);

        foreach (var character in CharacterTokens)
        {
            var key = CharacterKey(character.CharacterName, character.WorldId);

            if (hadProfile && !RpProfiles.ContainsKey(key))
            {
                RpProfiles[key] = new RpProfileCache
                {
                    RpLevel       = RpProfileLevel!,
                    ApproachMode  = RpProfileApproachMode!,
                    ContactMode   = RpProfileContactMode,
                    SessionLength = RpProfileSessionLength,
                    Languages     = RpProfileLanguages ?? "[\"fr\"]",
                    Themes        = RpProfileThemes,
                };
            }

            if (RpAvailabilityActive && !RpAvailability.ContainsKey(key))
                RpAvailability[key] = true;
        }

        Version = 4;
        Save();
    }

    /// <summary>
    /// Allume la mise en forme du chat sur les configurations existantes.
    ///
    /// Une valeur par défaut ne suffit pas ici : un fichier déjà enregistré
    /// porte <c>false</c> écrit noir sur blanc, et le repeindre en <c>true</c>
    /// dans la déclaration ne changerait rien pour ceux qui ont déjà lancé le
    /// plugin, c'est-à-dire tout le monde. D'où la version 5.
    ///
    /// Limite assumée : le fichier ne dit pas si le <c>false</c> vient d'un
    /// refus ou du défaut d'origine, et la migration rallume donc aussi les rares
    /// joueurs qui avaient éteint le module. Le module n'ayant vécu qu'une seule
    /// version, et n'ayant à peu près rien fait pendant celle-ci, ils sont au
    /// plus une poignée, et il leur reste un interrupteur bien visible.
    ///
    /// <c>ChatRpNames</c> n'était pas touché ici : à l'époque, il se cochait à la
    /// main ou pas du tout. La version 7 est revenue sur ce choix.
    /// </summary>
    public void MigrateChatDefaults()
    {
        if (Version >= 5) return;

        ChatFormatEnabled = true;
        ChatFormatEmote   = true;
        ChatFormatOoc     = true;
        ChatFormatSpeech  = true;

        Version = 5;
        Save();
    }

    /// <summary>
    /// Impose Ctrl sur les configurations qui n'avaient aucun modificateur.
    ///
    /// Comme en version 5, un défaut déclaré ne suffit pas : un fichier déjà
    /// enregistré porte <c>None</c> écrit noir sur blanc, et repeindre la
    /// déclaration ne changerait rien pour ceux qui jouent déjà, c'est-à-dire
    /// précisément ceux que l'infobulle dérange.
    ///
    /// Seul <c>None</c> est touché : qui a délibérément choisi Ctrl ou Alt a déjà
    /// réglé son affaire, et on ne lui reprend pas la main. La limite est la même
    /// qu'en version 5, le fichier ne distinguant pas un <c>None</c> choisi d'un
    /// <c>None</c> hérité du défaut d'origine.
    /// </summary>
    public void MigrateTooltipModifier()
    {
        if (Version >= 6) return;

        if (RpTooltipModifier == RpTooltipKey.None)
            RpTooltipModifier = RpTooltipKey.Ctrl;

        Version = 6;
        Save();
    }

    /// <summary>
    /// Allume l'affichage des noms RP sur les configurations existantes.
    ///
    /// Même raisonnement qu'en version 5 : un fichier déjà enregistré porte
    /// <c>false</c> écrit noir sur blanc, et repeindre la déclaration ne
    /// changerait rien pour ceux qui jouent déjà. Un nom RP renseigné dans une
    /// fiche publique doit se voir là où l'on joue, c'est-à-dire au-dessus du
    /// personnage et dans le chat.
    ///
    /// Limite assumée, la même qu'en version 5 : le fichier ne dit pas si le
    /// <c>false</c> vient d'un refus ou du défaut d'origine, et la migration
    /// rallume donc aussi les rares joueurs qui avaient éteint la substitution
    /// dans le chat. Les deux interrupteurs restent à portée dans les réglages.
    /// </summary>
    public void MigrateRpNameDefaults()
    {
        if (Version >= 7) return;

        ChatRpNames      = true;
        NameplateRpNames = true;

        Version = 7;
        Save();
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

namespace EorzeaEventsPlugin;

/// <summary>All user-facing strings for FR and EN.</summary>
internal sealed class Loc
{
    // ── Tabs ──────────────────────────────────────────────────────────────────
    public required string TabRp       { get; init; }
    public required string TabEvents   { get; init; }
    public required string TabEstabs   { get; init; }
    public required string TabDebug    { get; init; }
    public required string TabSettings { get; init; }

    // ── Common ────────────────────────────────────────────────────────────────
    public required string Loading    { get; init; }
    public required string Refresh    { get; init; }
    public required string ViewOnline { get; init; }
    public required string Open       { get; init; }
    public required string Ongoing    { get; init; }
    public required string Recurring    { get; init; }
    public required string Description  { get; init; }
    public required string Map          { get; init; }
    public required string Save       { get; init; }
    public required string Cancel     { get; init; }
    public required string Search     { get; init; }
    public required string Show       { get; init; }
    public required string Hide       { get; init; }
    public required string Ignore     { get; init; }
    public required string Auto       { get; init; }
    public required string Processing { get; init; }

    // ── RP Session tab ────────────────────────────────────────────────────────
    public required string RpNoSession         { get; init; }
    public required string RpSessionsActive    { get; init; }   // {0} session(s)
    public required string RpBeFirst           { get; init; }
    public required string RpInYourZone        { get; init; }   // ✦  Dans votre zone ({0})
    public required string RpOtherServers      { get; init; }
    public required string RpYourSessionActive { get; init; }
    public required string RpManageSession     { get; init; }
    public required string RpNewSession        { get; init; }
    public required string RpResume            { get; init; }
    public required string RpLastRefresh       { get; init; }   // {0} = secondes

    // ── Events tab ────────────────────────────────────────────────────────────
    public required string EventsNoEvents { get; init; }
    public required string EventsCount    { get; init; }   // {0} événement(s)
    public required string EventsOngoing  { get; init; }   // {0} en cours
    public required string EventsTotal    { get; init; }   // · {0} au total

    // Agenda : en-têtes de jour et filtres.
    public required string EventsToday       { get; init; }
    public required string EventsTomorrow    { get; init; }
    public required string EventsSearchHint  { get; init; }
    public required string EventsFilterAll   { get; init; }
    public required string EventsOfficial    { get; init; }
    public required string EventsCommunity   { get; init; }
    public required string EventsNoMatch     { get; init; }
    public required string EventsClearFilter { get; init; }

    // Voyage assisté par Lifestream.
    public required string TravelGo          { get; init; }
    public required string TravelBusy        { get; init; }
    public required string CfgTravel         { get; init; }
    public required string CfgTravelHint     { get; init; }
    public required string CfgTravelMissing  { get; init; }

    // ── Establishments tab ────────────────────────────────────────────────────
    public required string EstabSearchHint { get; init; }
    public required string EstabNoResults  { get; init; }
    public required string EstabCount      { get; init; }   // {0} établissement(s)
    public required string EstabDetail     { get; init; }
    public required string EstabOpenSite   { get; init; }
    public required string EstabDiscord    { get; init; }
    public required string EstabFeatured   { get; init; }
    public required string EstabSemiRp     { get; init; }
    public required string EstabApartment  { get; init; }
    public required string CfgCharactersHeader { get; init; }
    public required string CfgLinkPending      { get; init; }   // {0} = perso@monde
    public required string CfgLinkPendingHint  { get; init; }
    public required string CfgLinkReopen       { get; init; }
    public required string CfgLinkNeedLogin    { get; init; }
    public required string CfgLinkAgain        { get; init; }
    public required string CfgLinkCharacter    { get; init; }   // {0} = perso@monde
    public required string CfgLinkForget       { get; init; }
    public required string And             { get; init; }
    public required string RecurrenceDaily    { get; init; }
    public required string RecurrenceWeekly   { get; init; }
    public required string RecurrenceWeeklyOn { get; init; }   // {0} = jours
    public required string RecurrenceMonthly  { get; init; }

    /// <summary>Vrai pour le jeu de chaînes françaises. Sert au formatage des dates.</summary>
    public bool IsFrench => ReferenceEquals(this, Fr);

    /// <summary>
    /// Culture de formatage des dates, alignée sur la langue du plugin plutôt
    /// que sur celle du système : un joueur en plugin français doit lire
    /// « mercredi 5 août », quelle que soit la locale de sa machine.
    /// </summary>
    public System.Globalization.CultureInfo Culture => IsFrench ? FrCulture : EnCulture;

    private static readonly System.Globalization.CultureInfo FrCulture = new("fr-FR");
    private static readonly System.Globalization.CultureInfo EnCulture = new("en-GB");
    public required string EstabSyncshells { get; init; }
    public required string EstabPassword   { get; init; }
    public required string EstabReveal     { get; init; }
    public required string EstabCopied     { get; init; }
    public required Dictionary<string, string> DistrictLabels { get; init; }

    // ── Debug tab ─────────────────────────────────────────────────────────────
    public required string DebugCopy              { get; init; }
    public required string DebugCopied            { get; init; }
    public required string DebugUnavailable       { get; init; }
    public required string DebugSectionPlayer     { get; init; }
    public required string DebugSectionTerritory  { get; init; }
    public required string DebugSectionWorldPos   { get; init; }
    public required string DebugSectionMapPos     { get; init; }
    public required string DebugSectionHousing    { get; init; }
    public required string DebugSectionDerived    { get; init; }

    // ── My Session window ─────────────────────────────────────────────────────
    public required string MySessionTitle         { get; init; }
    public required string SessionCreate          { get; init; }
    public required string SessionActive          { get; init; }
    public required string FieldTitle             { get; init; }
    public required string FieldDesc              { get; init; }
    public required string FieldLocation          { get; init; }
    public required string FieldServer            { get; init; }
    public required string FieldCharName          { get; init; }
    public required string FieldDuration          { get; init; }
    public required string FieldWard              { get; init; }
    public required string FieldPlot              { get; init; }
    public required string FieldRoom              { get; init; }
    public required string FieldPosition          { get; init; }
    public required string FieldHousing           { get; init; }
    public required string HousingAnnex           { get; init; }
    public required string HousingWardPlot        { get; init; }   // {0}=ward {1}=plot
    public required string HousingWardRoom        { get; init; }   // {0}=ward {1}=room
    public required string HousingWard            { get; init; }   // {0}=ward
    public required string WorldUnknown           { get; init; }
    public required string ZoneUnknown            { get; init; }
    public required string BtnCreate              { get; init; }
    public required string BtnModify              { get; init; }
    public required string BtnEnd                 { get; init; }
    public required string BtnUpdatePos           { get; init; }
    public required string BtnExtend              { get; init; }
    public required string BtnConfigureNow        { get; init; }
    public required string StatusPosUpdated       { get; init; }
    public required string StatusCreating         { get; init; }
    public required string StatusUpdating         { get; init; }
    public required string StatusEnding           { get; init; }
    public required string StatusStarted          { get; init; }
    public required string StatusRecovered        { get; init; }
    public required string StatusUpdated          { get; init; }
    public required string StatusEnded            { get; init; }
    public required string StatusExtended         { get; init; }   // {0}h
    public required string ErrCreate              { get; init; }
    public required string ErrUpdate              { get; init; }
    public required string ErrExtend              { get; init; }
    public required string ErrTitleRequired       { get; init; }
    public required string ErrTokenMissing        { get; init; }
    public required string HintNoLocation         { get; init; }
    public required string HintNoServer           { get; init; }
    public required string ExpiresIn              { get; init; }
    public required string Hours                  { get; init; }
    public required string MySessionTokenMissingDesc  { get; init; }
    public required string MySessionTokenInvalidDesc  { get; init; }
    public required string AlertZoneChanged           { get; init; }
    public required string AlertRpTagRemoved          { get; init; }
    public required string AlertRpTagActivated        { get; init; }
    public required string AlertZoneChangedTitle      { get; init; }
    public required string AlertZoneChangedDesc       { get; init; }
    public required string AlertRpTagRemovedTitle     { get; init; }
    public required string AlertRpTagRemovedDesc      { get; init; }
    public required string AlertRpTagActivTitle       { get; init; }
    public required string AlertRpTagActivDesc        { get; init; }
    public required string AlertExpiryTitle           { get; init; }
    public required string AlertExpiryDesc            { get; init; }   // {0} = minutes
    public required string BtnStop                    { get; init; }
    public required string AlertActiveEventTitle      { get; init; }
    public required string AlertActiveEventDesc       { get; init; }   // {0} = eventTitle, {1} = estabName
    public required string AlertActiveRpTitle         { get; init; }
    public required string AlertActiveRpDesc          { get; init; }   // {0} = sessionTitle, {1} = authorName
    public required string AlertEventPromoTitle       { get; init; }
    public required string AlertEventPromoDesc        { get; init; }   // {0} = eventTitle, {1} = estabName
    public required string AlertEventPromoReason      { get; init; }   // {0} = motif serveur (reasonFr/reasonEn)
    public required string BtnCreateAnyway            { get; init; }

    // ── Setup window ──────────────────────────────────────────────────────────
    public required string SetupWelcomeL1        { get; init; }
    public required string SetupWelcomeL2        { get; init; }
    public required string SetupWelcomeL3        { get; init; }
    public required string SetupStart            { get; init; }
    public required string SetupStepTitle        { get; init; }
    public required string SetupStepDesc         { get; init; }
    public required string SetupMigrationTitle   { get; init; }
    public required string SetupMigrationDesc    { get; init; }
    public required string SetupMigrationMore    { get; init; }
    public required string SetupTokenLabel       { get; init; }
    public required string SetupTokenInvalid    { get; init; }
    public required string SetupErrPrefix       { get; init; }
    public required string SetupSkip            { get; init; }
    public required string SetupDoneTitle       { get; init; }
    public required string SetupDoneL1          { get; init; }
    public required string SetupDoneL2          { get; init; }
    public required string SetupDoneHint        { get; init; }
    public required string SetupOpenPlugin      { get; init; }

    // ── Config window ─────────────────────────────────────────────────────────
    public required string CfgTokenLabel        { get; init; }
    public required string CfgTokenOk           { get; init; }
    public required string CfgTokenMissing      { get; init; }
    public required string CfgTokenEdit         { get; init; }
    public required string CfgNotifHeader       { get; init; }
    public required string CfgNotifScreen       { get; init; }
    public required string CfgNotifScreenHint   { get; init; }
    public required string CfgNotifDalamud      { get; init; }
    public required string CfgNotifDalamudHint  { get; init; }
    public required string CfgNotifChat         { get; init; }
    public required string CfgNotifMyWorld      { get; init; }
    public required string CfgNotifNearby            { get; init; }
    public required string CfgNotifNearbyHint        { get; init; }
    public required string CfgNotifLanguageFilter     { get; init; }
    public required string CfgNotifLanguageFilterHint { get; init; }
    public required string CfgEventNotifHeader  { get; init; }
    public required string CfgEventNotifScreen  { get; init; }
    public required string CfgEventNotifChat    { get; init; }
    public required string CfgEventNotifHint    { get; init; }
    public required string CfgSessionHeader     { get; init; }
    public required string CfgSuggestOnTag      { get; init; }
    public required string CfgAlertZone         { get; init; }
    public required string CfgAlertTag          { get; init; }
    public required string CfgAlertExpiry       { get; init; }
    public required string CfgAutoRefreshPos    { get; init; }
    public required string CfgLangHeader        { get; init; }
    public required string CfgLangAuto          { get; init; }
    public required string CfgLangFr            { get; init; }
    public required string CfgLangEn            { get; init; }
    public required string CfgDtrHeader         { get; init; }
    public required string CfgDtrRp             { get; init; }
    public required string CfgDtrEvents         { get; init; }
    public required string CfgTest              { get; init; }

    // ── Token invalid / blocked screens ──────────────────────────────────────
    public required string TokenInvalidLine1    { get; init; }
    public required string TokenInvalidLine2    { get; init; }
    public required string TokenInvalidLine3    { get; init; }
    public required string TokenReconfigure     { get; init; }
    public required string BlockedHint          { get; init; }
    public required string BlockedOpenPluginPage { get; init; }

    // ── Notifications (toast / chat) ──────────────────────────────────────────
    public required string NotifTokenTitle      { get; init; }
    public required string NotifTokenContent    { get; init; }
    public required string NotifNewRpTitle      { get; init; }
    public required string NotifNearbyRp        { get; init; }   // {0} = title
    public required string NotifNewRpScreen     { get; init; }   // {0}=title {1}=loc {2}=server
    public required string NotifNewRpChat       { get; init; }   // {0}=title {1}=loc {2}=server
    public required string NotifEventStartScreen { get; init; }  // {0}=title {1}=establishment
    public required string NotifEventStartChat  { get; init; }   // {0}=details
    public required string DtrRpTooltip         { get; init; }
    public required string DtrEventsTooltip     { get; init; }
    public required string DtrRpAvailTooltip    { get; init; }
    public required string DtrRpAvailLabel      { get; init; }
    public required string DtrRpLabel           { get; init; }
    public required string DtrEventsLabel       { get; init; }
    public required string CfgDtrRpAvail        { get; init; }
    public required string PlayersOnline        { get; init; }   // {0} = count
    public required string MoreInfo             { get; init; }
    public required string EventsHideHint       { get; init; }
    public required string EventCancelled       { get; init; }

    // ── RP Availability & Profile ─────────────────────────────────────────────
    public required string RpAvailableDesc         { get; init; }
    public required string RpAvailableTitle        { get; init; }
    public required string RpAvailableEmpty        { get; init; }
    public required string RpAvailableInZone       { get; init; }   // {0} = count
    public required string RpAvailableEnable       { get; init; }
    public required string RpAvailableDisable      { get; init; }
    public required string RpAvailableDur30        { get; init; }
    public required string RpAvailableDur60        { get; init; }
    public required string RpAvailableDur120       { get; init; }
    public required string RpAvailableActiveStatus { get; init; }   // {0} = minutes restantes
    public required string RpAvailableNoToken      { get; init; }
    public required string RpAvailableNoCharacter  { get; init; }
    public required string RpAvailableFailed       { get; init; }
    public required string RpProfileSetup          { get; init; }
    public required string RpProfileWizardTitle    { get; init; }
    public required string RpProfileWizardIntro    { get; init; }
    public required string RpProfileTitle         { get; init; }
    public required string RpProfileNoCharacter   { get; init; }
    public required string RpProfileEditOnline    { get; init; }
    public required string RpProfileWebNoticeTitle { get; init; }
    public required string RpProfileWebNoticeBody  { get; init; }
    public required string RpAvailableEnableHint  { get; init; }
    public required string RpProfileHooks         { get; init; }
    public required string RpProfileHooksHint     { get; init; }
    public required string RpProfileHooksExample  { get; init; }
    public required string RpProfileCurrentQuest  { get; init; }
    public required string RpProfilePreferences   { get; init; }
    public required string RpProfileThemes        { get; init; }
    public required string RpProfileAvoidThemes   { get; init; }
    public required string RpProfileIdentity      { get; init; }
    public required string RpProfileRace          { get; init; }
    public required string RpProfileRaceOther     { get; init; }
    public required string RpProfileAge           { get; init; }
    public required string RpProfilePronouns      { get; init; }
    public required string RpProfileOrigin        { get; init; }
    public required string RpProfileOccupation    { get; init; }
    public required string RpProfileAppearance    { get; init; }
    public required string RpProfilePersonality   { get; init; }
    public required string RpProfileBackground    { get; init; }
    public required string RpProfileLimits        { get; init; }

    // Traits physiques, appartenances et mise en avant : éditables en jeu.
    public required string RpProfileTraits       { get; init; }
    public required string RpProfileTraitsHint   { get; init; }
    public required string RpProfileHeight       { get; init; }
    public required string RpProfileBuild        { get; init; }
    public required string RpProfileMarks        { get; init; }
    public required string RpProfileVoice        { get; init; }
    public required string RpProfileBelonging    { get; init; }
    public required string RpProfileFreeCompany  { get; init; }
    public required string RpProfileAllegiance   { get; init; }
    public required string RpProfileDeity        { get; init; }
    public required string RpProfileDeityNone    { get; init; }
    public required string RpProfileQuote        { get; init; }
    public required string RpProfileQuoteHint    { get; init; }

    // Relations : consultables en jeu, éditables sur le site.
    public required string RpProfileRelations     { get; init; }
    public required string RpProfileRelationAlly    { get; init; }
    public required string RpProfileRelationFriend  { get; init; }
    public required string RpProfileRelationFamily  { get; init; }
    public required string RpProfileRelationLover   { get; init; }
    public required string RpProfileRelationMentor  { get; init; }
    public required string RpProfileRelationStudent { get; init; }
    public required string RpProfileRelationRival   { get; init; }
    public required string RpProfileRelationEnemy   { get; init; }
    public required string RpProfileRelationOther   { get; init; }
    public required string RpProfileLevel          { get; init; }
    public required string RpProfileLevelBeginner  { get; init; }
    public required string RpProfileLevelCasual    { get; init; }
    public required string RpProfileLevelConfirmed { get; init; }
    public required string RpProfileApproach           { get; init; }
    public required string RpProfileApproachCome       { get; init; }
    public required string RpProfileApproachComeHint   { get; init; }
    public required string RpProfileApproachIGo        { get; init; }
    public required string RpProfileApproachIGoHint    { get; init; }
    public required string RpProfileApproachEither     { get; init; }
    public required string RpProfileApproachEitherHint { get; init; }
    public required string RpProfileLanguages      { get; init; }
    public required string RpProfileSaved          { get; init; }
    public required string RpProfileError          { get; init; }
    public required string RpProfileViewTitle      { get; init; }
    public required string CfgRpProfileHeader      { get; init; }
    public required string CfgRpIndicator          { get; init; }

    // ── Titre nameplate disponibilité ─────────────────────────────────────────
    public required string RpLoginPrompt            { get; init; }
    public required string RpLoginStay             { get; init; }
    public required string RpLoginDisable          { get; init; }
    public required string CfgRpAskOnLogin         { get; init; }

    // ── Titre nameplate disponibilité ─────────────────────────────────────────
    public required string RpNameplateBase          { get; init; }  // "Dispo RP"
    public required string RpNameplateTimide        { get; init; }  // "Timide"
    public required string RpNameplateExtraverti    { get; init; }  // masculin
    public required string RpNameplateExtravertie   { get; init; }  // féminin

    // ── Annonce one-shot ──────────────────────────────────────────────────────
    public required string AnnouncementTitle       { get; init; }
    public required string AnnouncementBadge       { get; init; }
    public required string AnnouncementBody        { get; init; }
    public required string AnnouncementConfigure   { get; init; }
    public required string AnnouncementLater       { get; init; }
    public required string AnnouncementIndicator   { get; init; }

    // ── Nouveautés ────────────────────────────────────────────────────────────
    public required string WhatsNewTitle       { get; init; }
    public required string WhatsNewClose       { get; init; }
    public required string WhatsNewEmpty       { get; init; }
    public required string WhatsNewUnseen      { get; init; }
    public required string CfgAboutHeader      { get; init; }
    public required string CfgWhatsNew         { get; init; }
    public required string CfgWhatsNewAuto     { get; init; }
    public required string CfgWhatsNewAutoHint { get; init; }

    // ── Fiche d'un autre joueur ───────────────────────────────────────────────
    public required string RpProfileNoProfile  { get; init; }
    public required string RpProfileNsfw       { get; init; }
    public required string RpProfileViewOnSite { get; init; }
    public required string MenuViewRpProfile   { get; init; }
    public required string RpProfileZoom       { get; init; }
    public required string RpProfileZoomClose  { get; init; }

    // ── Visibilité de la fiche ────────────────────────────────────────────────
    public required string RpProfileVisibility       { get; init; }
    public required string RpProfileVisWhere         { get; init; }
    public required string RpProfileVisWho           { get; init; }
    public required string RpProfileVisInGame        { get; init; }
    public required string RpProfileVisInGameHint    { get; init; }
    public required string RpProfileVisWebPage       { get; init; }
    public required string RpProfileVisWebPageHint   { get; init; }
    public required string RpProfileVisIndexable     { get; init; }
    public required string RpProfileVisIndexableHint { get; init; }
    public required string RpProfileVisAlwaysPublic  { get; init; }
    public required string RpProfileVisOwnerNote     { get; init; }  // {0} = libellé accordé
    public required string RpProfileAudiencePublic   { get; init; }
    public required string RpProfileAudienceOwner    { get; init; }  // masculin
    public required string RpProfileAudienceOwnerFem { get; init; }  // féminin
    public required string RpProfilePreview          { get; init; }
    public required string RpProfilePreviewTitle     { get; init; }
    public required string RpProfilePreviewHint      { get; init; }
    public required string RpProfilePreviewHidden    { get; init; }
    public required string RpProfileVisSaveFirst     { get; init; }
    public required string RpProfileDescription      { get; init; }

    // ── Autour de moi ─────────────────────────────────────────────────────────
    public required string TabAround         { get; init; }
    public required string AroundCount       { get; init; }   // {0} joueur(s)
    public required string AroundEmpty       { get; init; }
    public required string AroundNoMatch     { get; init; }
    public required string AroundSearchHint  { get; init; }
    public required string AroundMyWorldOnly { get; init; }

    // ── Static instances ──────────────────────────────────────────────────────

    public static readonly Loc Fr = new()
    {
        TabRp       = "RP Ouvert",
        TabEvents   = "Événements",
        TabEstabs   = "Lieux",
        TabDebug    = "Debug",
        TabSettings = "Paramètres",

        Loading    = "Chargement...",
        Refresh    = "Actualiser",
        ViewOnline = "Voir en ligne",
        Open       = "Ouvrir",
        MoreInfo   = "+ d'infos",
        Ongoing    = "EN COURS",
        Recurring    = "récurrent",
        Description  = "Description",
        Map          = "Carte",
        Save       = "Enregistrer",
        Cancel     = "Annuler",
        Search     = "Rechercher",
        Show       = "Afficher",
        Hide       = "Masquer",
        Ignore     = "Ignorer",
        Auto       = "Auto",
        Processing = "Traitement...",

        RpNoSession         = "Aucune session active en ce moment",
        RpSessionsActive    = "{0} session(s) en cours",
        RpBeFirst           = "Soyez le premier à en démarrer une !",
        RpInYourZone        = "Dans votre zone ({0})",
        RpOtherServers      = "Autres serveurs",
        RpYourSessionActive = "Votre session est en cours.",
        RpManageSession     = "Gérer ma session",
        RpNewSession        = "Nouvelle session de RP ouvert",
        RpLastRefresh       = "({0}s)",
        RpResume            = "Reprendre",

        EventsNoEvents = "Aucun événement dans les 14 prochains jours.",
        EventsCount    = "{0} événement(s)",
        EventsOngoing  = "{0} en cours",
        EventsTotal    = "· {0} événement(s) au total",
        EventsToday       = "Aujourd'hui",
        EventsTomorrow    = "Demain",
        EventsSearchHint  = "Rechercher un événement, un lieu…",
        EventsFilterAll   = "Tous",
        EventsOfficial    = "Officiels",
        EventsCommunity   = "Communauté",
        EventsNoMatch     = "Aucun événement ne correspond à ces filtres.",
        EventsClearFilter = "Réinitialiser",
        TravelGo         = "Y aller (Lifestream)",
        TravelBusy       = "Lifestream est déjà en route.",
        CfgTravel        = "Proposer le voyage via Lifestream",
        CfgTravelHint    = "Ajoute un bouton « Y aller » sur les événements et les fiches de lieu.",
        CfgTravelMissing = "Lifestream n'est pas installé : le bouton reste masqué.",
        EventsHideHint = "Pour ne plus voir un lieu ni recevoir ses notifications, masque-le depuis l'onglet Lieux.",
        EventCancelled = "Annulé pour aujourd'hui",

        EstabSearchHint = "Recherchez par nom, serveur ou quartier.",
        EstabNoResults  = "Aucun résultat.",
        EstabCount      = "{0} lieu(x)",
        EstabDetail     = "Fiche",
        EstabOpenSite   = "Voir le site",
        EstabDiscord    = "Discord",
        EstabFeatured  = "Mis en avant",
        EstabSemiRp    = "Semi-RP",
        EstabApartment = "Appartement",
        CfgCharactersHeader = "Personnages liés",
        CfgLinkPending      = "Couplage en cours pour {0}",
        CfgLinkPendingHint  = "Confirmez dans le navigateur : le plugin récupérera le jeton automatiquement.",
        CfgLinkReopen       = "Rouvrir la page",
        CfgLinkNeedLogin    = "Connectez-vous en jeu pour lier un personnage.",
        CfgLinkAgain        = "Relier",
        CfgLinkCharacter    = "Lier {0}",
        CfgLinkForget       = "Oublier ce personnage",
        And                = "et",
        RecurrenceDaily    = "chaque jour",
        RecurrenceWeekly   = "chaque semaine",
        RecurrenceWeeklyOn = "chaque {0}",
        RecurrenceMonthly  = "chaque mois",
        EstabSyncshells = "Syncshells",
        EstabPassword   = "MdP",
        EstabReveal     = "Révéler",
        EstabCopied     = "Copié !",
        DistrictLabels  = new()
        {
            ["brumee"]     = "Brumée",
            ["lavandiere"] = "Lavandière",
            ["coupe"]      = "La Coupe",
            ["shirogane"]  = "Shirogane",
            ["empyree"]    = "Empyrée",
        },
        DebugCopy             = "Copier le dump",
        DebugCopied           = "Dump copié dans le presse-papiers.",
        DebugUnavailable      = "Indisponible",
        DebugSectionPlayer    = "Joueur",
        DebugSectionTerritory = "Territoire / Carte",
        DebugSectionWorldPos  = "Position monde",
        DebugSectionMapPos    = "Coordonnées carte",
        DebugSectionHousing   = "Housing",
        DebugSectionDerived   = "Dérivés / Heuristiques",

        MySessionTitle        = "Ma session de RP ouvert",
        SessionCreate         = "Nouvelle session de RP ouvert",
        SessionActive         = "Session en cours",
        FieldTitle            = "Titre",
        FieldDesc             = "Description",
        FieldLocation         = "Zone",
        FieldServer           = "Serveur",
        FieldCharName         = "Nom du personnage",
        FieldDuration         = "Durée (heures)",
        FieldWard             = "Quartier",
        FieldPlot             = "Parcelle",
        FieldRoom             = "Appartement",
        FieldPosition         = "Position",
        FieldHousing          = "Logement",
        HousingAnnex          = "annexe",
        HousingWardPlot       = "Quartier {0}  —  Parcelle {1}",
        HousingWardRoom       = "Quartier {0}  —  Appartement {1}",
        HousingWard           = "Quartier {0}",
        WorldUnknown          = "Monde inconnu",
        ZoneUnknown           = "Zone inconnue",
        BtnCreate             = "Créer",
        BtnModify             = "Modifier",
        BtnEnd                = "Terminer la session",
        BtnUpdatePos          = "Mettre à jour la position",
        BtnExtend             = "Prolonger (+1h)",
        BtnConfigureNow       = "Configurer maintenant",
        StatusPosUpdated      = "Position mise à jour.",
        StatusCreating        = "Création en cours...",
        StatusUpdating        = "Mise à jour...",
        StatusEnding          = "Fin de session...",
        StatusStarted         = "Session démarrée !",
        StatusRecovered       = "Session existante récupérée.",
        StatusUpdated         = "Session mise à jour.",
        StatusEnded           = "Session terminée.",
        StatusExtended        = "Session prolongée de {0}h.",
        ErrCreate             = "Erreur lors de la création.",
        ErrUpdate             = "Erreur lors de la mise à jour.",
        ErrExtend             = "Erreur lors de la prolongation.",
        ErrTitleRequired      = "Le titre est obligatoire.",
        ErrTokenMissing       = "Token API non configuré.",
        HintNoLocation        = "Zone introuvable, remplissez manuellement.",
        HintNoServer          = "Serveur introuvable, remplissez manuellement.",
        ExpiresIn             = "Expire dans environ",
        Hours                 = "heure(s)",
        MySessionTokenMissingDesc = "Génère un token depuis ton profil pour accéder aux sessions RP.",
        MySessionTokenInvalidDesc = "Tu dois générer un nouveau token pour continuer à utiliser le plugin.",
        AlertZoneChanged      = "Vous avez changé de zone.",
        AlertRpTagRemoved     = "Vous avez retiré le tag RP.",
        AlertRpTagActivated   = "Tag RP activé.",
        AlertZoneChangedTitle = "⚠  Changement de zone détecté",
        AlertZoneChangedDesc  = "Voulez-vous mettre à jour votre emplacement ou terminer la session ?",
        AlertRpTagRemovedTitle = "⚠  Tag RP retiré",
        AlertRpTagRemovedDesc  = "Vous n'êtes plus en mode RP. Souhaitez-vous terminer la session ?",
        AlertRpTagActivTitle  = "Tag RP activé !",
        AlertRpTagActivDesc   = "Vous êtes en mode RP. Souhaitez-vous annoncer une session de RP ouvert ?",
        AlertExpiryTitle      = "Session bientôt expirée",
        AlertExpiryDesc       = "Votre session RP expire dans {0} minute(s). Souhaitez-vous la prolonger ?",
        BtnStop               = "Arrêter",
        AlertActiveEventTitle = "⚠  Événement en cours ici",
        AlertActiveEventDesc  = "« {0} » est déjà en cours chez {1}. Les participants reçoivent des notifications automatiques. Le RP ouvert est pour les sessions spontanées sans événement planifié.",
        AlertActiveRpTitle    = "⚠  RP ouvert déjà en cours ici",
        AlertActiveRpDesc     = "« {0} » par {1} est déjà en cours à cet emplacement. Rejoins-la si tu veux participer, ou crée la tienne quand même si c'est un RP distinct.",
        AlertEventPromoTitle  = "⛔  Session refusée",
        AlertEventPromoDesc   = "Cette session ferait doublon avec l'événement « {0} » déjà annoncé chez {1}. Le RP ouvert sert aux scènes spontanées, pas à promouvoir un événement planifié. Rejoins l'événement existant, ou contacte le staff si c'est une erreur.",
        AlertEventPromoReason = "Motif : {0}",
        BtnCreateAnyway       = "Créer quand même",

        SetupWelcomeL1     = "Ce plugin fonctionne de pair avec le site",
        SetupWelcomeL2     = "Il vous permet de gérer vos sessions de RP ouvert directement depuis FFXIV, sans quitter le jeu.",
        SetupWelcomeL3     = "Le couplage prend quelques secondes : il vous suffira de confirmer dans votre navigateur.",
        SetupStart            = "Commencer",
        SetupStepTitle        = "Étape 1 / 1 — Lier votre personnage",
        SetupStepDesc         = "Connectez-vous in-game sur le personnage à lier. Le plugin va lire son nom et son monde via Dalamud, puis ouvrir une page de confirmation dans votre navigateur. Cliquez « Confirmer » sur cette page et le couplage se fera automatiquement.",
        SetupMigrationTitle   = "Nouveau : tokens par personnage",
        SetupMigrationDesc    = "Le plugin supporte maintenant un token distinct par personnage. Liez votre personnage actuel pour en profiter. Votre ancien token continue de fonctionner en attendant.",
        SetupMigrationMore    = "Plus d'infos",
        SetupTokenLabel       = "Personnage détecté :",
        SetupTokenInvalid  = "Le lien avec ce personnage a expiré ou été révoqué.\nRelancez le couplage pour continuer.",
        SetupErrPrefix     = "Aucun personnage n'est connecté in-game pour le moment.",
        SetupSkip          = "Passer",
        SetupDoneTitle     = "Personnage lié !",
        SetupDoneL1        = "Votre personnage est lié. Vous pouvez maintenant créer",
        SetupDoneL2        = "des sessions RP directement depuis le jeu.",
        SetupDoneHint      = "Tapez /eorzea pour ouvrir le panneau, /eorzea link pour lier d'autres personnages.",
        SetupOpenPlugin    = "Ouvrir Eorzea Events",

        CfgTokenLabel       = "Token API :",
        CfgTokenOk          = "Configuré",
        CfgTokenMissing     = "Non configuré",
        CfgTokenEdit        = "Modifier",
        CfgNotifHeader      = "Nouvelles sessions de RP ouvert",
        CfgNotifScreen      = "Afficher une alerte au centre de l'écran",
        CfgNotifScreenHint  = "   Style natif FFXIV, comme les messages de bienvenue",
        CfgNotifDalamud     = "Afficher une bulle de notification",
        CfgNotifDalamudHint = "   Petite carte dans le coin supérieur droit",
        CfgNotifChat        = "Écrire un message dans le chat",
        CfgNotifMyWorld     = "Ignorer les sessions sur d'autres serveurs",
        CfgNotifNearby            = "Alerte prioritaire si la session est dans ma zone actuelle",
        CfgNotifNearbyHint        = "   Même serveur et même zone",
        CfgNotifLanguageFilter     = "Filtrer par langue",
        CfgNotifLanguageFilterHint = "   N'affiche que les RP dans la langue de l'interface du plugin.",
        CfgEventNotifHeader = "Événements en cours",
        CfgEventNotifScreen  = "Afficher une alerte au centre de l'écran",
        CfgEventNotifChat   = "Écrire aussi un message dans le chat",
        CfgEventNotifHint   = "   Seulement pour les événements communautaires visibles",
        CfgSessionHeader    = "Ma session de RP ouvert",
        CfgSuggestOnTag     = "Me proposer de démarrer une session quand j'active le tag RP",
        CfgAlertZone        = "Me prévenir si je change de zone ou effectue un TP",
        CfgAlertTag         = "Me prévenir si je retire le tag RP",
        CfgAlertExpiry      = "Me prévenir quand ma session arrive à expiration (15 min avant)",
        CfgAutoRefreshPos   = "Mettre à jour ma position automatiquement (toutes les 5 min)",
        CfgLangHeader       = "Langue",
        CfgLangAuto         = "Automatique (langue du jeu)",
        CfgLangFr           = "Français",
        CfgLangEn           = "English",
        CfgDtrHeader        = "Barre de statut",
        CfgDtrRp            = "Afficher le compteur de sessions RP (RP: N)",
        CfgDtrEvents        = "Afficher le compteur d'événements (Events: N)",
        CfgTest             = "Tester",

        TokenInvalidLine1 = "Token API invalide ou expiré.",
        TokenInvalidLine2 = "Tu dois en générer un nouveau pour continuer",
        TokenInvalidLine3 = "à utiliser Eorzea Events.",
        TokenReconfigure  = "Reconfigurer le token",
        BlockedHint       = "Tape /xlplugins en jeu pour ouvrir le gestionnaire de plugins.",
        BlockedOpenPluginPage = "Ouvrir la page du plugin",

        NotifTokenTitle   = "Token API expiré — Eorzea Events",
        NotifTokenContent = "Ton token API n'est plus valide. Génère-en un nouveau depuis ton profil.",
        NotifNewRpTitle   = "Nouvelle session de RP ouvert",
        NotifNearbyRp     = "RP ouvert dans votre zone !\n{0}",
        NotifNewRpScreen  = "Nouveau RP ouvert !\n{0} — {1} ({2})",
        NotifNewRpChat    = "Nouveau RP ouvert : {0} — {1} ({2})",
        NotifEventStartScreen = "Événement en cours !\n{0} — {1}",
        NotifEventStartChat  = "Événement en cours : {0}",
        DtrRpTooltip      = "Sessions RP ouvertes en cours\nCliquez pour ouvrir",
        DtrEventsTooltip  = "Événements en cours\nCliquez pour ouvrir",
        DtrRpAvailTooltip = "Disponibilité pour du RP spontané\nCliquez pour vous déclarer disponible ou non",
        DtrRpAvailLabel   = "Dispo RP",
        DtrRpLabel        = "RP",
        DtrEventsLabel    = "Événements",
        CfgDtrRpAvail     = "Afficher le statut de disponibilité RP",
        PlayersOnline     = "{0} joueur(s) en ligne",

        RpAvailableDesc         = "Signale aux autres rôlistes que tu es disponible pour du RP spontané. Un titre coloré apparaît sous ton nom sur les nameplates des joueurs avec le plugin : « Dispo RP - Timide » si tu préfères qu'on vienne vers toi, « Dispo RP - Avenant·e » si tu peux faire le premier pas.",
        RpAvailableTitle        = "Disponibles pour du RP spontané",
        RpAvailableEmpty        = "Personne de disponible dans cette zone",
        RpAvailableInZone       = "{0} disponible(s) pour du RP dans votre zone",
        RpAvailableEnable       = "Je suis disponible",
        RpAvailableDisable      = "Arrêter",
        RpAvailableDur30        = "30 min",
        RpAvailableDur60        = "1h",
        RpAvailableDur120       = "2h",
        RpAvailableActiveStatus = "Disponible pour du RP",
        RpAvailableNoToken      = "Liez ce personnage pour activer la disponibilité RP.",
        RpAvailableNoCharacter  = "Aucun personnage connecté : impossible de changer votre disponibilité.",
        RpAvailableFailed       = "La disponibilité n'a pas pu être enregistrée. Vérifiez votre connexion, puis réessayez.",
        RpProfileSetup          = "Configurer mon profil RP",
        RpProfileWizardTitle    = "Mon profil RP",
        RpProfileWizardIntro    = "Quelques questions rapides pour que les autres joueurs sachent à quoi s'attendre avant de t'approcher.",
        RpProfileTitle          = "Mon profil RP",
        RpProfileNoCharacter    = "Connectez-vous en jeu pour voir la fiche de votre personnage.",
        RpProfileEditOnline     = "Modifier sur le site",
        RpProfileWebNoticeTitle = "La fiche complète se remplit sur le site",
        RpProfileWebNoticeBody  = "En jeu se règle ce qui change souvent en jouant : disponibilité, accroches, traits physiques, appartenances, préférences et visibilité.\n\nLe portrait, l'identité (race, âge, pronoms, origine, métier), les thèmes recherchés et évités, les relations, l'apparence, la personnalité, l'histoire et les limites ne se modifient que sur le site. Ils s'affichent ici en lecture seule.",
        RpAvailableEnableHint   = "Les autres rôlistes vous voient dans la liste des joueurs disponibles.",
        RpProfileHooks          = "Accroches",
        RpProfileHooksHint      = "Ce qui donne envie de venir vous parler.",
        RpProfileHooksExample   = "Tenancier de la taverne des Deux Lunes",
        RpProfileCurrentQuest   = "En ce moment",
        RpProfilePreferences    = "Préférences",
        RpProfileThemes         = "Thèmes recherchés",
        RpProfileAvoidThemes    = "Thèmes évités",
        RpProfileIdentity       = "Identité",
        RpProfileRace           = "Race",
        RpProfileRaceOther      = "Autre",
        RpProfileAge            = "Âge",
        RpProfilePronouns       = "Pronoms",
        RpProfileOrigin         = "Origine",
        RpProfileOccupation     = "Occupation",
        RpProfileAppearance     = "Apparence",
        RpProfilePersonality    = "Personnalité",
        RpProfileBackground     = "Histoire",
        RpProfileLimits         = "Limites",
        RpProfileTraits         = "Traits physiques",
        RpProfileTraitsHint     = "Des repères rapides, en plus de l'apparence rédigée.",
        RpProfileHeight         = "Taille",
        RpProfileBuild          = "Corpulence",
        RpProfileMarks          = "Signes distinctifs",
        RpProfileVoice          = "Voix",
        RpProfileBelonging      = "Appartenances",
        RpProfileFreeCompany    = "Compagnie libre",
        RpProfileAllegiance     = "Allégeance",
        RpProfileDeity          = "Divinité",
        RpProfileDeityNone      = "Non précisé",
        RpProfileQuote          = "Citation",
        RpProfileQuoteHint      = "Une réplique qui résume le personnage.",
        RpProfileRelations      = "Relations",
        RpProfileRelationAlly    = "Allié",
        RpProfileRelationFriend  = "Ami",
        RpProfileRelationFamily  = "Famille",
        RpProfileRelationLover   = "Amour",
        RpProfileRelationMentor  = "Mentor",
        RpProfileRelationStudent = "Élève",
        RpProfileRelationRival   = "Rival",
        RpProfileRelationEnemy   = "Ennemi",
        RpProfileRelationOther   = "Autre",
        RpProfileLevel          = "Niveau de RP",
        RpProfileLevelBeginner  = "Débutant — Je découvre le RP",
        RpProfileLevelCasual    = "Casual — Je RP de temps en temps",
        RpProfileLevelConfirmed = "Confirmé — Rôliste expérimenté",
        RpProfileApproach           = "Mode d'approche",
        RpProfileApproachCome       = "Venez vers moi",
        RpProfileApproachComeHint   = "Je suis timide — faites le premier pas, je ne mordrai pas !",
        RpProfileApproachIGo        = "Je peux approcher",
        RpProfileApproachIGoHint    = "Je n'hésite pas à initier le contact si je sens une compatibilité.",
        RpProfileApproachEither     = "Indifférent",
        RpProfileApproachEitherHint = "Que vous veniez à moi ou l'inverse, je m'adapte.",
        RpProfileLanguages      = "Langue(s) de RP",
        RpProfileSaved          = "Profil enregistré !",
        RpProfileError          = "Erreur lors de l'enregistrement.",
        RpProfileViewTitle      = "Profil RP",
        CfgRpProfileHeader      = "Profil RP & Disponibilité",
        CfgRpIndicator          = "Afficher le marqueur sur les nameplates des joueurs disponibles",

        RpLoginPrompt           = "Tu étais disponible pour du RP lors de ta dernière session.",
        RpLoginStay             = "Rester disponible",
        RpLoginDisable          = "Me mettre indisponible",
        CfgRpAskOnLogin         = "Me demander à la reconnexion si je suis disponible",

        RpNameplateBase         = "Dispo RP",
        RpNameplateTimide       = "Timide",
        RpNameplateExtraverti   = "Avenant",
        RpNameplateExtravertie  = "Avenante",

        AnnouncementTitle       = "Nouveau — Profil RP & Disponibilité",
        AnnouncementBadge       = "Mise à jour",
        AnnouncementBody        = "Tu peux maintenant indiquer que tu es disponible pour du RP spontané.\n\nLes autres joueurs verront un losange à droite de ton nom dans le jeu, et pourront voir ton profil (niveau, mode d'approche, langue) avant de t'aborder.\n\nConfigure ton profil en quelques secondes — tu pourras le modifier à tout moment dans les paramètres.",
        AnnouncementConfigure   = "Configurer mon profil RP",
        AnnouncementLater       = "Plus tard",
        AnnouncementIndicator   = "L'indicateur sur les nameplates peut être désactivé dans Paramètres.",

        WhatsNewTitle       = "Nouveautés Eorzea Events",
        WhatsNewClose       = "Compris",
        WhatsNewEmpty       = "Aucune note pour cette version.",
        WhatsNewUnseen      = "nouveau",
        CfgAboutHeader      = "À propos",
        CfgWhatsNew         = "Voir les nouveautés",
        CfgWhatsNewAuto     = "Afficher les nouveautés après une mise à jour",
        CfgWhatsNewAutoHint = "La fenêtre s'ouvre une seule fois, au premier lancement suivant l'installation d'une nouvelle version.",

        RpProfileNoProfile  = "Ce joueur n'a pas encore rempli sa fiche.",
        RpProfileNsfw       = "Contenu sensible",
        RpProfileViewOnSite = "Voir sur le site",
        MenuViewRpProfile   = "Voir la fiche RP",
        RpProfileZoom       = "Cliquer pour agrandir le portrait",
        RpProfileZoomClose  = "Clic ou Échap pour fermer",

        RpProfileVisibility       = "Visibilité",
        RpProfileVisWhere         = "Où ma fiche apparaît",
        RpProfileVisWho           = "Qui voit quoi",
        RpProfileVisInGame        = "Visible en jeu",
        RpProfileVisInGameHint    = "Dans la liste des joueurs disponibles et par clic droit. N'ouvre aucune page sur le site.",
        RpProfileVisWebPage       = "Page web partageable",
        RpProfileVisWebPageHint   = "Crée une adresse que tu peux transmettre. N'inscrit pas ta fiche dans les moteurs de recherche.",
        RpProfileVisIndexable     = "Référencée par les moteurs",
        RpProfileVisIndexableHint = "Ta fiche peut apparaître dans les résultats de recherche. Une fois indexée, elle peut y rester un moment.",
        RpProfileVisAlwaysPublic  = "Niveau, mode d'approche, langues, thèmes, citation et disponibilité restent visibles dès que ta fiche est visible en jeu : ce sont eux qui alimentent le marqueur sur les plaques de nom.",
        RpProfileVisOwnerNote     = "« {0} » veut dire que la section n'est jamais envoyée aux autres joueurs. Ton texte reste enregistré sur le site.",
        RpProfileAudiencePublic   = "Tout le monde",
        RpProfileAudienceOwner    = "Moi seul",
        RpProfileAudienceOwnerFem = "Moi seule",
        RpProfilePreview          = "Aperçu de mon profil",
        RpProfilePreviewTitle     = "Aperçu de ma fiche",
        RpProfilePreviewHint      = "Voici exactement ce que les autres joueurs voient. Les sections réservées ne sont pas envoyées par le serveur, elles sont donc absentes d'ici aussi.",
        RpProfilePreviewHidden    = "Ta fiche n'est pas visible en jeu. Coche « Visible en jeu » pour que les autres puissent la consulter.",
        RpProfileVisSaveFirst     = "Enregistre d'abord : l'aperçu montre ce que le serveur a reçu.",
        RpProfileDescription      = "Description",

        TabAround         = "Autour de moi",
        AroundCount       = "{0} joueur(s) disponible(s)",
        AroundEmpty       = "Personne de disponible pour le moment",
        AroundNoMatch     = "Aucun joueur ne correspond",
        AroundSearchHint  = "Rechercher un personnage",
        AroundMyWorldOnly = "Mon monde uniquement",
    };

    public static readonly Loc En = new()
    {
        TabRp       = "Open RP",
        TabEvents   = "Events",
        TabEstabs   = "Venues",
        TabDebug    = "Debug",
        TabSettings = "Settings",

        Loading    = "Loading...",
        Refresh    = "Refresh",
        ViewOnline = "View online",
        Open       = "Open",
        MoreInfo   = "More info",
        Ongoing    = "ONGOING",
        Recurring    = "recurring",
        Description  = "Description",
        Map          = "Map",
        Save       = "Save",
        Cancel     = "Cancel",
        Search     = "Search",
        Show       = "Show",
        Hide       = "Hide",
        Ignore     = "Dismiss",
        Auto       = "Auto",
        Processing = "Processing...",

        RpNoSession         = "No active sessions right now",
        RpSessionsActive    = "{0} active session(s)",
        RpBeFirst           = "Be the first to start one!",
        RpInYourZone        = "In your zone ({0})",
        RpOtherServers      = "Other servers",
        RpYourSessionActive = "Your session is active.",
        RpManageSession     = "Manage my session",
        RpNewSession        = "New open RP session",
        RpLastRefresh       = "({0}s)",
        RpResume            = "Resume",

        EventsNoEvents = "No events in the next 14 days.",
        EventsCount    = "{0} event(s)",
        EventsOngoing  = "{0} ongoing",
        EventsTotal    = "· {0} event(s) total",
        EventsToday       = "Today",
        EventsTomorrow    = "Tomorrow",
        EventsSearchHint  = "Search an event, a venue…",
        EventsFilterAll   = "All",
        EventsOfficial    = "Official",
        EventsCommunity   = "Community",
        EventsNoMatch     = "No event matches these filters.",
        EventsClearFilter = "Reset",
        TravelGo         = "Travel there (Lifestream)",
        TravelBusy       = "Lifestream is already travelling.",
        CfgTravel        = "Offer travel through Lifestream",
        CfgTravelHint    = "Adds a “Travel there” button on events and venue pages.",
        CfgTravelMissing = "Lifestream is not installed: the button stays hidden.",
        EventsHideHint = "To stop seeing a venue and its event notifications, hide it from the Venues tab.",
        EventCancelled = "Cancelled for today",

        EstabSearchHint = "Search by name, server or ward.",
        EstabNoResults  = "No results found.",
        EstabCount      = "{0} venue(s)",
        EstabDetail     = "Details",
        EstabOpenSite   = "Visit website",
        EstabDiscord    = "Discord",
        EstabFeatured  = "Featured",
        EstabSemiRp    = "Semi-RP",
        EstabApartment = "Apartment",
        CfgCharactersHeader = "Linked characters",
        CfgLinkPending      = "Linking {0}",
        CfgLinkPendingHint  = "Confirm in your browser: the plugin will pick up the token automatically.",
        CfgLinkReopen       = "Reopen page",
        CfgLinkNeedLogin    = "Log in to the game to link a character.",
        CfgLinkAgain        = "Relink",
        CfgLinkCharacter    = "Link {0}",
        CfgLinkForget       = "Forget this character",
        And                = "and",
        RecurrenceDaily    = "every day",
        RecurrenceWeekly   = "every week",
        RecurrenceWeeklyOn = "every {0}",
        RecurrenceMonthly  = "every month",
        EstabSyncshells = "Syncshells",
        EstabPassword   = "Pwd",
        EstabReveal     = "Reveal",
        EstabCopied     = "Copied!",
        DistrictLabels  = new()
        {
            ["brumee"]     = "The Mist",
            ["lavandiere"] = "The Lavender Beds",
            ["coupe"]      = "The Goblet",
            ["shirogane"]  = "Shirogane",
            ["empyree"]    = "The Empyrean",
        },
        DebugCopy             = "Copy dump",
        DebugCopied           = "Dump copied to clipboard.",
        DebugUnavailable      = "Unavailable",
        DebugSectionPlayer    = "Player",
        DebugSectionTerritory = "Territory / Map",
        DebugSectionWorldPos  = "World Position",
        DebugSectionMapPos    = "Map Coordinates",
        DebugSectionHousing   = "Housing",
        DebugSectionDerived   = "Derived / Guesses",

        MySessionTitle        = "My Open RP Session",
        SessionCreate         = "New open RP session",
        SessionActive         = "Active session",
        FieldTitle            = "Title",
        FieldDesc             = "Description",
        FieldLocation         = "Zone",
        FieldServer           = "Server",
        FieldCharName         = "Character name",
        FieldDuration         = "Duration (hours)",
        FieldWard             = "Ward",
        FieldPlot             = "Plot",
        FieldRoom             = "Room",
        FieldPosition         = "Position",
        FieldHousing          = "Housing",
        HousingAnnex          = "annex",
        HousingWardPlot       = "Ward {0}  —  Plot {1}",
        HousingWardRoom       = "Ward {0}  —  Room {1}",
        HousingWard           = "Ward {0}",
        WorldUnknown          = "Unknown world",
        ZoneUnknown           = "Unknown zone",
        BtnCreate             = "Create",
        BtnModify             = "Edit",
        BtnEnd                = "End session",
        BtnUpdatePos          = "Update position",
        BtnExtend             = "Extend (+1h)",
        BtnConfigureNow       = "Configure now",
        StatusPosUpdated      = "Position updated.",
        StatusCreating        = "Creating...",
        StatusUpdating        = "Updating...",
        StatusEnding          = "Ending session...",
        StatusStarted         = "Session started!",
        StatusRecovered       = "Existing session recovered.",
        StatusUpdated         = "Session updated.",
        StatusEnded           = "Session ended.",
        StatusExtended        = "Session extended by {0}h.",
        ErrCreate             = "Error while creating session.",
        ErrUpdate             = "Error while updating.",
        ErrExtend             = "Error extending session.",
        ErrTitleRequired      = "Title is required.",
        ErrTokenMissing       = "API token not configured.",
        HintNoLocation        = "Zone not found, please fill in manually.",
        HintNoServer          = "Server not found, please fill in manually.",
        ExpiresIn             = "Expires in about",
        Hours                 = "hour(s)",
        MySessionTokenMissingDesc = "Generate a token from your profile to access RP sessions.",
        MySessionTokenInvalidDesc = "You need to generate a new token to continue using the plugin.",
        AlertZoneChanged      = "You changed zone.",
        AlertRpTagRemoved     = "You removed the RP tag.",
        AlertRpTagActivated   = "RP tag activated.",
        AlertZoneChangedTitle = "⚠  Zone change detected",
        AlertZoneChangedDesc  = "Do you want to update your location or end the session?",
        AlertRpTagRemovedTitle = "⚠  RP tag removed",
        AlertRpTagRemovedDesc  = "You're no longer in RP mode. Do you want to end the session?",
        AlertRpTagActivTitle  = "RP tag activated!",
        AlertRpTagActivDesc   = "You're in RP mode. Do you want to announce an open RP session?",
        AlertExpiryTitle      = "Session expiring soon",
        AlertExpiryDesc       = "Your RP session expires in {0} minute(s). Do you want to extend it?",
        BtnStop               = "Stop",
        AlertActiveEventTitle = "⚠  Event in progress here",
        AlertActiveEventDesc  = "\"{0}\" is running at {1}. Attendees receive automatic notifications. Open RP is for spontaneous sessions without a scheduled event.",
        AlertActiveRpTitle    = "⚠  Open RP already in progress here",
        AlertActiveRpDesc     = "\"{0}\" by {1} is already running at this location. Join it if you want to take part, or create yours anyway if it's a distinct RP.",
        AlertEventPromoTitle  = "⛔  Session blocked",
        AlertEventPromoDesc   = "This session would duplicate the event \"{0}\" already announced at {1}. Open RP is for spontaneous scenes, not for promoting a planned event. Join the existing event, or contact staff if this is a mistake.",
        AlertEventPromoReason = "Reason: {0}",
        BtnCreateAnyway       = "Create anyway",

        SetupWelcomeL1     = "This plugin works alongside the website",
        SetupWelcomeL2     = "It lets you manage your open RP sessions directly from FFXIV, without leaving the game.",
        SetupWelcomeL3     = "Linking takes a few seconds: just confirm in your browser.",
        SetupStart            = "Get started",
        SetupStepTitle        = "Step 1 / 1 — Link your character",
        SetupStepDesc         = "Log in-game on the character you want to link. The plugin reads its name and world via Dalamud, then opens a confirmation page in your browser. Click \"Confirm\" on that page and the link completes automatically.",
        SetupMigrationTitle   = "New: per-character tokens",
        SetupMigrationDesc    = "The plugin now supports one token per character. Link your current character to use it. Your legacy token keeps working in the meantime.",
        SetupMigrationMore    = "Learn more",
        SetupTokenLabel       = "Detected character:",
        SetupTokenInvalid  = "The link with this character has expired or was revoked.\nStart the linking process again to continue.",
        SetupErrPrefix     = "No character is currently logged in-game.",
        SetupSkip          = "Skip",
        SetupDoneTitle     = "Character linked!",
        SetupDoneL1        = "Your character is linked. You can now create",
        SetupDoneL2        = "RP sessions directly from the game.",
        SetupDoneHint      = "Type /eorzea to open the panel, /eorzea link to link more characters.",
        SetupOpenPlugin    = "Open Eorzea Events",

        CfgTokenLabel       = "API Token:",
        CfgTokenOk          = "Configured",
        CfgTokenMissing     = "Not configured",
        CfgTokenEdit        = "Edit",
        CfgNotifHeader      = "New open RP sessions",
        CfgNotifScreen      = "Show an alert in the center of the screen",
        CfgNotifScreenHint  = "   Native FFXIV style, like welcome messages",
        CfgNotifDalamud     = "Show a notification bubble",
        CfgNotifDalamudHint = "   Small card in the top-right corner",
        CfgNotifChat        = "Print a message in the chat",
        CfgNotifMyWorld     = "Ignore sessions on other servers",
        CfgNotifNearby            = "Priority alert if the session is in my current zone",
        CfgNotifNearbyHint        = "   Same server and same zone",
        CfgNotifLanguageFilter     = "Filter by language",
        CfgNotifLanguageFilterHint = "   Only shows RPs in the plugin interface language.",
        CfgEventNotifHeader = "Live events",
        CfgEventNotifScreen  = "Show an alert in the middle of the screen",
        CfgEventNotifChat   = "Also print a chat message",
        CfgEventNotifHint   = "   Only for visible community events",
        CfgSessionHeader    = "My open RP session",
        CfgSuggestOnTag     = "Suggest starting a session when I enable the RP tag",
        CfgAlertZone        = "Warn me if I change zone or teleport",
        CfgAlertTag         = "Warn me if I remove the RP tag",
        CfgAlertExpiry      = "Warn me when my session is about to expire (15 min before)",
        CfgAutoRefreshPos   = "Update my position automatically (every 5 min)",
        CfgLangHeader       = "Language",
        CfgLangAuto         = "Auto (game language)",
        CfgLangFr           = "Francais",
        CfgLangEn           = "English",
        CfgDtrHeader        = "Server info bar",
        CfgDtrRp            = "Show RP session counter (RP: N)",
        CfgDtrEvents        = "Show event counter (Events: N)",
        CfgTest             = "Test",

        TokenInvalidLine1 = "API token invalid or expired.",
        TokenInvalidLine2 = "You need to generate a new one to continue",
        TokenInvalidLine3 = "using Eorzea Events.",
        TokenReconfigure  = "Reconfigure token",
        BlockedHint       = "Type /xlplugins in-game to open the plugin manager.",
        BlockedOpenPluginPage = "Open plugin page",

        NotifTokenTitle   = "API token expired — Eorzea Events",
        NotifTokenContent = "Your API token is no longer valid. Generate a new one from your profile.",
        NotifNewRpTitle   = "New Open RP Session",
        NotifNearbyRp     = "Open RP in your zone!\n{0}",
        NotifNewRpScreen  = "New open RP!\n{0} — {1} ({2})",
        NotifNewRpChat    = "New open RP: {0} — {1} ({2})",
        NotifEventStartScreen = "Event is live!\n{0} — {1}",
        NotifEventStartChat  = "Event is live: {0}",
        DtrRpTooltip      = "Active open RP sessions\nClick to open",
        DtrEventsTooltip  = "Ongoing events\nClick to open",
        DtrRpAvailTooltip = "Availability for spontaneous RP\nClick to mark yourself available or not",
        DtrRpAvailLabel   = "RP avail.",
        DtrRpLabel        = "RP",
        DtrEventsLabel    = "Events",
        CfgDtrRpAvail     = "Show RP availability status",
        PlayersOnline     = "{0} player(s) online",

        RpAvailableDesc         = "Signal to other roleplayers that you're available for spontaneous RP. A colored title appears below your name on nameplates for players with the plugin: \"RP Avail - Shy\" if you'd rather others come to you, \"RP Avail - Friendly\" if you can make the first move.",
        RpAvailableTitle        = "Available for Spontaneous RP",
        RpAvailableEmpty        = "No one available in this zone",
        RpAvailableInZone       = "{0} available for RP in your zone",
        RpAvailableEnable       = "I'm available",
        RpAvailableDisable      = "Stop",
        RpAvailableDur30        = "30 min",
        RpAvailableDur60        = "1h",
        RpAvailableDur120       = "2h",
        RpAvailableActiveStatus = "Available for RP",
        RpAvailableNoToken      = "Link this character to enable RP availability.",
        RpAvailableNoCharacter  = "No character logged in: your availability can't be changed.",
        RpAvailableFailed       = "Your availability could not be saved. Check your connection and try again.",
        RpProfileSetup          = "Set up my RP profile",
        RpProfileWizardTitle    = "My RP Profile",
        RpProfileWizardIntro    = "A few quick questions so other players know what to expect before approaching you.",
        RpProfileTitle          = "My RP profile",
        RpProfileNoCharacter    = "Log in to the game to see your character's profile.",
        RpProfileEditOnline     = "Edit on the website",
        RpProfileWebNoticeTitle = "The full profile is filled in on the website",
        RpProfileWebNoticeBody  = "In game you set what changes often while playing: availability, hooks, physical traits, allegiances, preferences and visibility.\n\nThe portrait, identity (race, age, pronouns, origin, occupation), sought and avoided themes, relationships, appearance, personality, background and limits can only be edited on the website. They are shown here read-only.",
        RpAvailableEnableHint   = "Other roleplayers will see you in the available players list.",
        RpProfileHooks          = "Hooks",
        RpProfileHooksHint      = "What makes people want to come and talk to you.",
        RpProfileHooksExample   = "Keeper of the Two Moons tavern",
        RpProfileCurrentQuest   = "Right now",
        RpProfilePreferences    = "Preferences",
        RpProfileThemes         = "Themes sought",
        RpProfileAvoidThemes    = "Themes avoided",
        RpProfileIdentity       = "Identity",
        RpProfileRace           = "Race",
        RpProfileRaceOther      = "Other",
        RpProfileAge            = "Age",
        RpProfilePronouns       = "Pronouns",
        RpProfileOrigin         = "Origin",
        RpProfileOccupation     = "Occupation",
        RpProfileAppearance     = "Appearance",
        RpProfilePersonality    = "Personality",
        RpProfileBackground     = "Background",
        RpProfileLimits         = "Limits",
        RpProfileTraits         = "Physical traits",
        RpProfileTraitsHint     = "Quick cues, alongside the written appearance.",
        RpProfileHeight         = "Height",
        RpProfileBuild          = "Build",
        RpProfileMarks          = "Distinguishing marks",
        RpProfileVoice          = "Voice",
        RpProfileBelonging      = "Affiliations",
        RpProfileFreeCompany    = "Free company",
        RpProfileAllegiance     = "Allegiance",
        RpProfileDeity          = "Deity",
        RpProfileDeityNone      = "Unspecified",
        RpProfileQuote          = "Quote",
        RpProfileQuoteHint      = "A line that sums the character up.",
        RpProfileRelations      = "Relationships",
        RpProfileRelationAlly    = "Ally",
        RpProfileRelationFriend  = "Friend",
        RpProfileRelationFamily  = "Family",
        RpProfileRelationLover   = "Lover",
        RpProfileRelationMentor  = "Mentor",
        RpProfileRelationStudent = "Student",
        RpProfileRelationRival   = "Rival",
        RpProfileRelationEnemy   = "Enemy",
        RpProfileRelationOther   = "Other",
        RpProfileLevel          = "RP Level",
        RpProfileLevelBeginner  = "Beginner — New to RP",
        RpProfileLevelCasual    = "Casual — I RP occasionally",
        RpProfileLevelConfirmed = "Experienced — Seasoned roleplayer",
        RpProfileApproach           = "Approach style",
        RpProfileApproachCome       = "Come to me",
        RpProfileApproachComeHint   = "I'm a bit shy — feel free to make the first move, I won't bite!",
        RpProfileApproachIGo        = "I can approach",
        RpProfileApproachIGoHint    = "I don't mind initiating contact when I feel like we'd click.",
        RpProfileApproachEither     = "Either way",
        RpProfileApproachEitherHint = "Whether you come to me or I come to you, it works for me.",
        RpProfileLanguages      = "RP Language(s)",
        RpProfileSaved          = "Profile saved!",
        RpProfileError          = "Error saving profile.",
        RpProfileViewTitle      = "RP Profile",
        CfgRpProfileHeader      = "RP Profile & Availability",
        CfgRpIndicator          = "Show the marker on nameplates of available players",

        RpLoginPrompt           = "You were available for RP in your last session.",
        RpLoginStay             = "Stay available",
        RpLoginDisable          = "Set myself unavailable",
        CfgRpAskOnLogin         = "Ask me on login if I'm available for RP",

        RpNameplateBase         = "RP Avail",
        RpNameplateTimide       = "Shy",
        RpNameplateExtraverti   = "Friendly",
        RpNameplateExtravertie  = "Friendly",

        AnnouncementTitle       = "New — RP Profile & Availability",
        AnnouncementBadge       = "Update",
        AnnouncementBody        = "You can now signal that you're available for spontaneous RP.\n\nOther players will see a diamond next to your name in-game and can view your profile (level, approach style, language) before approaching you.\n\nSet up your profile in a few seconds — you can always change it later in Settings.",
        AnnouncementConfigure   = "Set up my RP profile",
        AnnouncementLater       = "Later",
        AnnouncementIndicator   = "The nameplate indicator can be disabled in Settings.",

        WhatsNewTitle       = "What's new in Eorzea Events",
        WhatsNewClose       = "Got it",
        WhatsNewEmpty       = "No notes for this version.",
        WhatsNewUnseen      = "new",
        CfgAboutHeader      = "About",
        CfgWhatsNew         = "View what's new",
        CfgWhatsNewAuto     = "Show what's new after an update",
        CfgWhatsNewAutoHint = "The window opens once, the first time you launch after a new version is installed.",

        RpProfileNoProfile  = "This player hasn't filled in their profile yet.",
        RpProfileNsfw       = "Sensitive content",
        RpProfileViewOnSite = "View on the website",
        MenuViewRpProfile   = "View RP profile",
        RpProfileZoom       = "Click to enlarge the portrait",
        RpProfileZoomClose  = "Click or press Esc to close",

        RpProfileVisibility       = "Visibility",
        RpProfileVisWhere         = "Where my profile appears",
        RpProfileVisWho           = "Who sees what",
        RpProfileVisInGame        = "Visible in game",
        RpProfileVisInGameHint    = "In the list of available players and on right-click. Does not create any page on the website.",
        RpProfileVisWebPage       = "Shareable web page",
        RpProfileVisWebPageHint   = "Creates an address you can pass around. Does not list your profile in search engines.",
        RpProfileVisIndexable     = "Listed in search engines",
        RpProfileVisIndexableHint = "Your profile may appear in search results. Once indexed, it can linger there for a while.",
        RpProfileVisAlwaysPublic  = "Level, approach style, languages, themes, quote and availability stay visible as soon as your profile is visible in game: they are what drives the nameplate marker.",
        RpProfileVisOwnerNote     = "\"{0}\" means the section is never sent to other players. Your text stays saved on the site.",
        RpProfileAudiencePublic   = "Everyone",
        // L'anglais ne s'accorde pas : les deux formes sont identiques, mais la
        // clé existe pour que l'appelant n'ait pas à savoir dans quelle langue il est.
        RpProfileAudienceOwner    = "Only me",
        RpProfileAudienceOwnerFem = "Only me",
        RpProfilePreview          = "Preview my profile",
        RpProfilePreviewTitle     = "Preview of my profile",
        RpProfilePreviewHint      = "This is exactly what other players see. Reserved sections are not sent by the server, so they are missing here too.",
        RpProfilePreviewHidden    = "Your profile is not visible in game. Tick \"Visible in game\" so others can view it.",
        RpProfileVisSaveFirst     = "Save first: the preview shows what the server received.",
        RpProfileDescription      = "Description",

        TabAround         = "Around me",
        AroundCount       = "{0} player(s) available",
        AroundEmpty       = "Nobody available right now",
        AroundNoMatch     = "No player matches",
        AroundSearchHint  = "Search for a character",
        AroundMyWorldOnly = "My world only",
    };
}

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
    public required string LoadFailed { get; init; }
    public required string SaveFailed { get; init; }
    public required string OnSiteCount { get; init; }   // {0} = joueurs présents
    public required string DeclaredPosition     { get; init; }
    public required string DeclaredPositionHint { get; init; }
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
    public required string DtrRpAvailPausedLabel { get; init; }
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
    public required string RpAvailableNeedsRpTag   { get; init; }
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

    // ── Coup d'œil ────────────────────────────────────────────────────────────
    //
    // Cinq détails que l'on remarque au premier regard. Le nom de chaque icône
    // est traduit : le sélecteur ImGui n'affiche pas de pictogramme dans sa
    // liste déroulante, seul le libellé permet donc de choisir.
    public required string RpProfileGlance         { get; init; }
    public required string RpProfileGallery        { get; init; }
    public required string RpProfileGlanceHint     { get; init; }
    public required string RpProfileGlanceBody     { get; init; }
    public required string RpProfileGlanceActive   { get; init; }
    public required string RpProfileGlanceExample  { get; init; }
    public required string RpProfileGlanceEmpty    { get; init; }
    public required string RpProfileGlanceAdd      { get; init; }
    public required string RpProfileGlanceSlot     { get; init; }  // {0} = rang
    public required string RpProfileGlanceRemove   { get; init; }
    public required string RpProfileGlanceRemoveArm { get; init; }
    public required Dictionary<string, string> RpGlanceIconLabels { get; init; }

    // ── Instant présent ───────────────────────────────────────────────────────
    //
    // Le statut du moment et l'intrigue en cours se ressemblent assez pour être
    // confondus : l'un se change au fil de la soirée, l'autre est l'arc de fond
    // du personnage. Les libellés doivent les distinguer, sans quoi le joueur
    // écrit son humeur dans le champ qui ne s'efface jamais.
    public required string RpProfileStatus           { get; init; }
    public required string RpProfileStatusHint       { get; init; }
    public required string RpProfileCurrently        { get; init; }
    public required string RpProfileCurrentlyExample { get; init; }
    public required string RpProfileIcState          { get; init; }
    public required string RpProfileIcStateIc        { get; init; }
    public required string RpProfileIcStateOoc       { get; init; }
    public required string RpProfileIcStateHint      { get; init; }
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
    public required string RpProfileContact        { get; init; }
    public required string RpProfileContactDirect  { get; init; }
    public required string RpProfileContactTell    { get; init; }
    public required string RpProfileContactEither  { get; init; }
    public required string RpProfileLengths        { get; init; }
    public required string RpProfileLengthShort    { get; init; }
    public required string RpProfileLengthMedium   { get; init; }
    public required string RpProfileLengthLong     { get; init; }
    public required string RpProfileThemeSong      { get; init; }
    public required string RpProfileExternalLink   { get; init; }
    public required string RpProfileLinks          { get; init; }

    // Thèmes et races : traduits, et non figés dans le code. Ils l'étaient au
    // motif qu'ils seraient « identiques dans les deux langues », ce qui est
    // faux pour huit thèmes sur douze et pour deux races.
    public required Dictionary<string, string> RpThemeLabels { get; init; }
    public required Dictionary<string, string> RpRaceLabels  { get; init; }
    public required string RpProfileOpenLink       { get; init; }
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

    // ─── Codes de sync ────────────────────────────────────────────────────────
    public required string RpProfileSyncshells     { get; init; }
    public required string RpProfileSyncshellsHint { get; init; }
    public required string RpProfileSyncshellOther { get; init; }
    public required string RpProfileSyncshellName  { get; init; }
    public required string RpProfileSyncshellId    { get; init; }
    public required string RpProfileSyncCopy       { get; init; }

    public required string RpProfileQuote        { get; init; }
    /// Étiquette de la ligne de disponibilité dans l'entête de la fiche. Le
    /// texte seul ne se comprenait pas : « Le soir et les weekends » posé sous
    /// une citation ne dit pas de quoi il parle, là où le site le nomme.
    public required string RpProfileAvailabilityLabel { get; init; }
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
    public required string CfgRpNameplateNames     { get; init; }
    public required string CfgRpNameplateNamesHint { get; init; }

    // ── Onglets des réglages ──────────────────────────────────────────────────
    public required string CfgTabCharacters         { get; init; }
    public required string CfgTabChat               { get; init; }
    public required string CfgTabRp                 { get; init; }
    public required string CfgTabNotifications      { get; init; }
    public required string CfgTabMisc               { get; init; }

    // ── Infobulle de ciblage ──────────────────────────────────────────────────
    public required string CfgRpTooltipCard         { get; init; }
    public required string CfgRpTooltip             { get; init; }
    public required string CfgRpTooltipHint         { get; init; }
    public required string CfgRpTooltipHover        { get; init; }
    public required string CfgRpTooltipHoverHint    { get; init; }
    public required string CfgRpTooltipModifier     { get; init; }
    public required string CfgRpTooltipModifierHint { get; init; }
    public required string CfgRpTooltipModNone      { get; init; }
    public required string CfgRpTooltipModCtrl      { get; init; }
    public required string CfgRpTooltipModAlt       { get; init; }

    // ── Facilités de discussion ───────────────────────────────────────────────
    public required string CfgChatHeader          { get; init; }
    public required string CfgChatEnabled         { get; init; }
    public required string CfgChatEnabledHint     { get; init; }
    public required string CfgChatOn              { get; init; }
    public required string CfgChatOff             { get; init; }
    public required string CfgChatEmote           { get; init; }
    public required string CfgChatEmoteHint       { get; init; }
    public required string CfgChatEmoteStyle      { get; init; }
    public required string CfgChatEmoteStyleStars { get; init; }
    public required string CfgChatEmoteStyleAngle { get; init; }
    public required string CfgChatEmoteStyleBoth  { get; init; }
    public required string CfgChatOoc             { get; init; }
    public required string CfgChatOocHint         { get; init; }
    public required string CfgChatSpeech          { get; init; }
    public required string CfgChatSpeechHint      { get; init; }
    public required string CfgChatColor           { get; init; }
    public required string CfgChatColorDefault    { get; init; }
    public required string CfgChatColorCustom     { get; init; }
    public required string CfgChatColorPicked     { get; init; }
    public required string CfgChatColorRendered   { get; init; }
    public required string CfgChatColorHint       { get; init; }
    public required string CfgChatRpNameAccent    { get; init; }
    public required string CfgChatChannels        { get; init; }
    public required string CfgChatChannelsHint    { get; init; }
    public required string CfgChatChanSay         { get; init; }
    public required string CfgChatChanTell        { get; init; }
    public required string CfgChatChanShout       { get; init; }
    public required string CfgChatChanYell        { get; init; }
    public required string CfgChatChanParty       { get; init; }
    public required string CfgChatChanLinkshell   { get; init; }
    public required string CfgChatChanFreeCompany { get; init; }
    public required string CfgChatChanEmote       { get; init; }
    public required string CfgChatRpNames         { get; init; }
    public required string CfgChatRpNamesHint     { get; init; }
    public required string CfgChatTokens          { get; init; }
    public required string CfgChatTokensHint      { get; init; }
    public required string ChatTokensUsage        { get; init; }
    public required string ChatTokensCopied       { get; init; }   // {0} = texte substitué
    public required string CfgRpNsfwShow            { get; init; }
    public required string CfgRpNsfwShowHint        { get; init; }
    public required string RpTooltipNsfwHidden      { get; init; }

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
    public required string RpProfileNsfwWithheld     { get; init; }
    public required string RpProfileNsfwWithheldHint { get; init; }
    public required string RpProfileNsfwWithheldCta  { get; init; }
    public required string RpProfileViewOnSite { get; init; }
    public required string MenuViewRpProfile   { get; init; }
    public required string RpFriendAdd         { get; init; }
    public required string RpFriendAddHint     { get; init; }
    public required string RpFriendAdded       { get; init; }
    public required string RpFriendRemoved     { get; init; }
    public required string RpFriendAddFailed   { get; init; }
    public required string RpFriendAddNotFound { get; init; }
    public required string RpFriendAddLimit    { get; init; }
    public required string RpFriendNoToken     { get; init; }
    public required string RpFriendChip        { get; init; }
    public required string RpFriendMutual      { get; init; }
    public required string RpFriendRenamed     { get; init; }
    public required string RpFriendNote        { get; init; }
    public required string RpFriendRemove      { get; init; }
    public required string RpFriendRemoveArm   { get; init; }
    public required string TabFriends          { get; init; }
    public required string RpFriendsTitle      { get; init; }
    public required string RpFriendsNoticeBody { get; init; }
    public required string RpFriendsEmpty      { get; init; }
    public required string RpFriendsNoToken    { get; init; }
    public required string RpFriendsGoVisibility   { get; init; }
    public required string RpProfileAudienceFriend { get; init; }
    public required string RpProfileVisFriendNote  { get; init; }
    public required string RpProfilePresetFriends  { get; init; }
    public required string RpProfilePresetFriendsHint { get; init; }
    public required string RpProfilePreviewAsPublic   { get; init; }
    public required string RpProfilePreviewAsFriend   { get; init; }
    public required string RpProfileZoom       { get; init; }
    public required string RpProfileZoomClose  { get; init; }

    // ── Visibilité de la fiche ────────────────────────────────────────────────
    public required string RpProfileVisibility       { get; init; }
    public required string RpProfileVisWhere         { get; init; }
    public required string RpProfileVisWho           { get; init; }
    public required string RpProfileVisInGame        { get; init; }
    public required string RpProfileVisInGameHint    { get; init; }
    public required string RpProfileVisIndexable     { get; init; }
    public required string RpProfileVisIndexableHint { get; init; }
    public required string RpProfileVisAlwaysPublic  { get; init; }
    // ── Fiche en onglets ──────────────────────────────────────────────────────
    //
    // Cinq libellés courts : ils tiennent sur une barre, et l'icône porte déjà
    // la moitié du sens.
    public required string RpProfileTabOverview  { get; init; }
    public required string RpProfileTabCharacter { get; init; }
    public required string RpProfileTabPlay      { get; init; }
    public required string RpProfileTabLinks     { get; init; }
    public required string RpProfileTabPrivacy   { get; init; }
    public required string RpProfileTabUnsaved   { get; init; }
    public required string CfgRpProfileTabs      { get; init; }
    public required string CfgRpProfileTabsHint  { get; init; }

    public required string RpProfileAutoSaved        { get; init; }
    public required string RpProfileAutoSaving       { get; init; }
    public required string RpProfileVisOwnerNote     { get; init; }  // {0} = libellé accordé
    public required string RpProfileAudiencePublic   { get; init; }
    public required string RpProfileAudienceOwner    { get; init; }  // masculin
    public required string RpProfileAudienceOwnerFem { get; init; }  // féminin
    public required string RpProfilePreview          { get; init; }
    public required string RpProfilePreviewTitle     { get; init; }
    public required string RpProfilePreviewHint      { get; init; }
    public required string RpProfilePreviewHidden    { get; init; }
    public required string RpProfileVisSaveFirst     { get; init; }
    public required string RpProfileRefreshHint      { get; init; }
    public required string RpProfileRefreshSaveFirst { get; init; }
    public required string RpProfileDescription      { get; init; }

    // ── Statut d'équipe ───────────────────────────────────────────────────────
    public required string RpProfileStaffModerator   { get; init; }
    public required string RpProfileStaffAdmin       { get; init; }
    public required string RpProfileStaffTitle       { get; init; }
    public required string RpProfileStaffBadge       { get; init; }
    public required string RpProfileStaffBadgeHint   { get; init; }

    // ── Habillage réservé aux membres ─────────────────────────────────────────
    // Un libellé par clé de FrameKeys et TitleAnimKeys (RpProfilePage). Ces
    // propriétés étant « required », ajouter un style sans son libellé ne compile
    // pas : c'est le seul garde-fou entre le vocabulaire du serveur et le plugin.
    public required string RpProfileFrameCorners     { get; init; }
    public required string RpProfileFrameRipple      { get; init; }
    public required string RpProfileFrameDuo         { get; init; }
    public required string RpProfileTitleAnimHalo    { get; init; }
    public required string RpProfileTitleAnimDuotone { get; init; }
    public required string RpProfileTitleAnimWave    { get; init; }
    public required string RpProfileTitleAnimNeon    { get; init; }

    // ── Autour de moi ─────────────────────────────────────────────────────────
    public required string TabAround         { get; init; }
    public required string AroundCount       { get; init; }   // {0} joueur(s)
    public required string AroundRpTaggedCount { get; init; }   // {0} joueur(s)
    public required string AroundRpTaggedHint  { get; init; }
    public required string AroundRpTaggedChip  { get; init; }
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
        LoadFailed = "Le chargement a échoué. Le site est peut-être injoignable.",
        SaveFailed = "L'enregistrement a échoué. Rien n'a été modifié sur le site.",
        OnSiteCount = "{0} sur place",
        DeclaredPosition     = "Position déclarée",
        DeclaredPositionHint = "Session annoncée depuis le site : l'emplacement est décrit par son auteur, il n'a pas été relevé en jeu. Hors logement, personne ne peut être détecté sur place.",
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
        DtrRpAvailTooltip = "Disponibilité pour du RP spontané\nElle n'est publiée que si le tag « Jeu de rôle » du jeu est actif (commande /jdr)\nCliquez pour vous déclarer disponible ou non",
        DtrRpAvailLabel   = "Dispo RP",
        DtrRpAvailPausedLabel = "Dispo RP (tag éteint)",
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
        RpAvailableNeedsRpTag   = "Votre disponibilité est retenue : elle sera publiée dès que le tag « Jeu de rôle » du jeu sera actif (commande /jdr).",
        RpProfileSetup          = "Configurer mon profil RP",
        RpProfileWizardTitle    = "Mon profil RP",
        RpProfileWizardIntro    = "Quelques questions rapides pour que les autres joueurs sachent à quoi s'attendre avant de t'approcher.",
        RpProfileTitle          = "Mon profil RP",
        RpProfileNoCharacter    = "Connectez-vous en jeu pour voir la fiche de votre personnage.",
        RpProfileEditOnline     = "Modifier sur le site",
        RpProfileWebNoticeTitle = "La fiche complète se remplit sur le site",
        RpProfileWebNoticeBody  = "En jeu se règle ce qui change souvent en jouant : disponibilité, accroches, traits physiques, appartenances, préférences et visibilité.\n\nLe portrait, la bannière, la couleur d'accent, l'identité (race, âge, pronoms, origine, métier), les thèmes recherchés et évités, les relations, l'apparence, la personnalité, l'histoire et les limites ne se modifient que sur le site. Ils s'affichent ici en lecture seule.",
        RpAvailableEnableHint   = "Les autres rôlistes vous voient dans la liste des joueurs disponibles.",
        RpProfileHooks          = "Accroches",
        RpProfileHooksHint      = "Ce qui donne envie de venir vous parler.",
        RpProfileHooksExample   = "Tenancier de la taverne des Deux Lunes",
        // « En ce moment » désigne désormais le statut du moment : l'intrigue
        // reprend le libellé du site, qui la nomme correctement.
        RpProfileCurrentQuest   = "Intrigue en cours",

        RpProfileGlance         = "Coup d'œil",
        RpProfileGallery        = "Galerie",
        RpProfileGlanceHint     = "Jusqu'à cinq détails que l'on remarque au premier regard, avant même de vous parler.",
        RpProfileGlanceBody     = "Description",
        RpProfileGlanceActive   = "Affiché",
        RpProfileGlanceExample  = "Une cicatrice en travers de la joue",
        RpProfileGlanceEmpty    = "Aucun détail pour l'instant.",
        RpProfileGlanceAdd      = "Ajouter un détail",
        RpProfileGlanceSlot     = "Détail {0}",
        RpProfileGlanceRemove   = "Retirer",
        RpProfileGlanceRemoveArm = "Confirmer le retrait",
        RpGlanceIconLabels = new()
        {
            ["sword"] = "Épée", ["shield"] = "Bouclier", ["book"] = "Livre",
            ["scroll"] = "Parchemin", ["flask"] = "Fiole", ["music"] = "Musique",
            ["heart"] = "Cœur", ["star"] = "Étoile", ["coin"] = "Pièce",
            ["hammer"] = "Marteau", ["leaf"] = "Feuille", ["flame"] = "Flamme",
            ["moon"] = "Lune", ["sun"] = "Soleil", ["eye"] = "Œil",
            ["mask"] = "Masque", ["crown"] = "Couronne", ["anchor"] = "Ancre",
            ["feather"] = "Plume", ["key"] = "Clé", ["skull"] = "Crâne",
            ["cup"] = "Coupe", ["map"] = "Carte", ["paw"] = "Patte",
        },

        RpProfileStatus           = "En ce moment",
        RpProfileStatusHint       = "Ce que fait votre personnage là, maintenant. Les autres le voient dans la liste et sur votre fiche.",
        RpProfileCurrently        = "Statut du moment",
        RpProfileCurrentlyExample = "Accoudé au bar du Quicksand, cherche une oreille attentive",
        RpProfileIcState          = "État de jeu",
        RpProfileIcStateIc        = "En RP",
        RpProfileIcStateOoc       = "Hors RP",
        RpProfileIcStateHint      = "Suit le tag « Jeu de rôle » du jeu (commande /jdr).",

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
        RpProfileContact        = "Prise de contact",
        RpProfileContactDirect  = "Direct",
        RpProfileContactTell    = "Un /tell d'abord",
        RpProfileContactEither  = "Peu importe",
        RpProfileLengths        = "Durée des scènes",
        RpProfileLengthShort    = "Courtes",
        RpProfileLengthMedium   = "Moyennes",
        RpProfileLengthLong     = "Longues",
        RpProfileThemeSong      = "Thème musical",
        RpProfileExternalLink   = "En savoir plus",
        RpProfileLinks          = "Liens",
        RpThemeLabels = new()
        {
            ["tavern"] = "Taverne", ["adventure"] = "Aventure", ["drama"] = "Drame",
            ["romance"] = "Romance", ["lore"] = "Lore-friendly", ["dark"] = "Thèmes sombres",
            ["mystery"] = "Mystère", ["intrigue"] = "Intrigue", ["combat"] = "Combat",
            ["craft"] = "Artisanat", ["slice_of_life"] = "Tranche de vie",
            ["politics"] = "Politique",
        },
        RpRaceLabels = new()
        {
            ["hyur"] = "Hyur", ["elezen"] = "Elézen", ["lalafell"] = "Lalafell",
            ["miqote"] = "Miqo'te", ["roegadyn"] = "Roegadyn", ["aura"] = "Au Ra",
            ["hrothgar"] = "Hrothgar", ["viera"] = "Viéra", ["other"] = "Autre",
        },
        RpProfileOpenLink       = "Ouvrir",
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

        RpProfileSyncshells     = "Codes de sync",
        RpProfileSyncshellsHint = "Vos identifiants Snowcloak, Umbra… Qui les voit se règle plus bas, dans Visibilité.",
        RpProfileSyncshellOther = "Autre",
        RpProfileSyncshellName  = "Nom du service",
        RpProfileSyncshellId    = "Identifiant",
        RpProfileSyncCopy       = "Copier",

        RpProfileQuote          = "Citation",
        RpProfileAvailabilityLabel = "Disponibilité",
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
        CfgRpNameplateNames     = "Afficher les noms RP sur les nameplates",
        CfgRpNameplateNamesHint = "Le nom du personnage est remplacé par son nom RP au-dessus de sa tête. Concerne tous les joueurs dont la fiche est publique et le tag « Jeu de rôle » allumé, pas seulement ceux qui se sont déclarés disponibles. Le clic droit vise toujours le vrai personnage.",

        CfgTabCharacters         = "Personnages",
        CfgTabChat               = "Discussion",
        CfgTabRp                 = "Rôleplay",
        CfgTabNotifications      = "Notifications",
        CfgTabMisc               = "Divers",

        CfgRpTooltipCard         = "Infobulle de survol",
        CfgRpTooltip             = "Afficher une infobulle sur les joueurs disponibles",
        CfgRpTooltipHint         = "Nom RP, état de jeu et coup d'œil apparaissent près du curseur. Rien n'est demandé au serveur : seuls les joueurs déjà déclarés disponibles s'y affichent.",
        CfgRpTooltipHover        = "Afficher aussi au simple survol",
        CfgRpTooltipHoverHint    = "Décoché, l'infobulle ne s'affiche que sur la cible sélectionnée.",
        CfgRpTooltipModifier     = "Touche à maintenir",
        CfgRpTooltipModifierHint = "L'infobulle reste masquée tant que la touche n'est pas enfoncée.",
        CfgRpTooltipModNone      = "Aucune",
        CfgRpTooltipModCtrl      = "Ctrl",
        CfgRpTooltipModAlt       = "Alt",

        CfgChatHeader          = "Discussion",
        CfgChatEnabled         = "Paramètres du chat",
        CfgChatEnabledHint     = "Les emotes, le hors jeu et le discours sont recolorés à l'affichage. Rien n'est modifié à l'envoi : vos interlocuteurs reçoivent exactement ce que vous avez tapé, plugin ou pas.",
        CfgChatOn              = "Actif : les réglages ci-dessous s'appliquent à votre chat.",
        CfgChatOff             = "Éteint : rien de ce qui suit n'a d'effet. Cocher une case rallume la mise en forme.",
        CfgChatEmote           = "Colorer les emotes",
        CfgChatEmoteHint       = "Par exemple : *ouvre la porte*",
        CfgChatEmoteStyle      = "Délimiteurs reconnus",
        CfgChatEmoteStyleStars = "Astérisques : *texte* et **texte**",
        CfgChatEmoteStyleAngle = "Chevrons : <texte>",
        CfgChatEmoteStyleBoth  = "Les deux",
        CfgChatOoc             = "Atténuer le hors jeu",
        CfgChatOocHint         = "Par exemple : (je reviens dans deux minutes)",
        CfgChatSpeech          = "Mettre en évidence le discours",
        CfgChatSpeechHint      = "Entre guillemets : « bonsoir » ou \"bonsoir\"",
        CfgChatColor           = "Couleur",
        CfgChatColorDefault    = "Couleur du plugin, ramenée à la palette du jeu",
        CfgChatColorCustom     = "Couleur personnalisée…",
        CfgChatColorPicked     = "Couleur choisie",
        CfgChatColorRendered   = "Telle qu'elle sortira dans le chat",
        CfgChatColorHint       = "Le chat du jeu n'affiche que les couleurs de sa palette. La teinte choisie est ramenée à la plus proche, montrée à droite.",
        CfgChatRpNameAccent    = "La couleur est celle de la fiche du joueur, ramenée à la teinte la plus proche que le chat sache afficher.",
        CfgChatChannels        = "Canaux traités",
        CfgChatChannelsHint    = "Limité à « dire » et aux messages privés par défaut : ailleurs, le chat charrie surtout du contenu de jeu.",
        CfgChatChanSay         = "Dire",
        CfgChatChanTell        = "Messages privés",
        CfgChatChanShout       = "Crier",
        CfgChatChanYell        = "Hurler",
        CfgChatChanParty       = "Groupe et alliance",
        CfgChatChanLinkshell   = "Linkshells",
        CfgChatChanFreeCompany = "Compagnie libre",
        CfgChatChanEmote       = "Emotes du jeu",
        CfgChatRpNames         = "Afficher les noms RP",
        CfgChatRpNamesHint     = "Le nom du personnage est remplacé par son nom RP, dans la couleur de sa fiche. Seuls les joueurs déjà déclarés disponibles sont concernés : rien n'est demandé au serveur à la réception d'un message.",
        CfgChatTokens          = "Jetons de saisie",
        CfgChatTokensHint      = "/eorzea rp <texte> remplace %xt par le nom RP de votre cible et %xp par le vôtre (%xtf, %xtl, %xpf, %xpl pour le prénom ou le nom seuls), puis copie le résultat dans le presse-papiers. Rien n'est envoyé à votre place.",
        ChatTokensUsage        = "Utilisation : /eorzea rp <texte>. %xt = nom RP de la cible, %xp = le vôtre, %xtf et %xtl pour son prénom et son nom, %xpf et %xpl pour les vôtres.",
        ChatTokensCopied       = "Copié dans le presse-papiers : {0}",
        CfgRpNsfwShow            = "Afficher le contenu des fiches marquées sensibles",
        CfgRpNsfwShowHint        = "Décoché, l'infobulle signale le marquage mais masque le coup d'œil et le statut du moment. L'ouverture d'une fiche entière dépend, elle, de ton compte sur le site.",
        RpTooltipNsfwHidden      = "Contenu masqué par vos réglages.",

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
        RpProfileNsfwWithheld     = "Fiche marquée contenu sensible",
        RpProfileNsfwWithheldHint = "Son auteur l'a marquée comme réservée aux adultes. Le site ne l'envoie qu'aux comptes qui ont accepté ce type de contenu.",
        RpProfileNsfwWithheldCta  = "Régler sur le site",
        RpProfileViewOnSite = "Voir sur le site",
        MenuViewRpProfile   = "Voir la fiche RP",
        RpFriendAdd         = "Ajouter comme ami RP",
        RpFriendAddHint     = "Lui ouvre les sections de VOTRE fiche réservées aux amis. Ne vous donne rien sur la sienne, et il n'en sera pas informé.",
        RpFriendAdded       = "[Eorzea Events] {0} peut désormais voir les sections de votre fiche réservées à vos amis.",
        RpFriendRemoved     = "[Eorzea Events] {0} ne voit plus les sections réservées à vos amis.",
        RpFriendAddFailed   = "La liste d'amis n'a pas pu être modifiée.",
        RpFriendAddNotFound = "Ce personnage n'a pas de fiche visible en jeu : il n'y a rien à ouvrir pour lui.",
        RpFriendAddLimit    = "Votre liste d'amis est pleine. Retirez quelqu'un avant d'ajouter.",
        RpFriendNoToken     = "Liez ce personnage depuis les réglages pour gérer ses amis RP.",
        RpFriendChip        = "Ami",
        RpFriendMutual      = "Vous a ajouté aussi",
        RpFriendRenamed     = "ajouté sous {0}",
        RpFriendNote        = "Note privée",
        RpFriendRemove      = "Retirer",
        RpFriendRemoveArm   = "Confirmer le retrait",
        TabFriends          = "Amis RP",
        RpFriendsTitle      = "Qui voit vos sections « amis »",
        RpFriendsNoticeBody = "Cette liste ouvre l'accès aux sections de VOTRE fiche réglées sur « Mes amis RP ». Elle ne vous donne accès à rien, et personne n'est prévenu d'y figurer.",
        RpFriendsEmpty      = "Aucun ami pour l'instant. Clic droit sur un joueur en jeu, puis « Ajouter comme ami RP ».",
        RpFriendsNoToken    = "Liez ce personnage pour gérer ses amis RP.",
        RpFriendsGoVisibility   = "Régler mes sections",
        RpProfileAudienceFriend = "Mes amis RP",
        RpProfileVisFriendNote  = "« Mes amis RP » ne s'applique qu'en jeu : ces sections n'apparaissent ni dans la liste des joueurs disponibles, ni sur la page web. La liste se gère dans l'onglet Amis RP.",
        RpProfilePresetFriends  = "Ouvrir à mes amis",
        RpProfilePresetFriendsHint = "Passe toutes les sections réservées à « Mes amis RP ».",
        RpProfilePreviewAsPublic   = "Vu par tous",
        RpProfilePreviewAsFriend   = "Vu par un ami",
        RpProfileZoom       = "Cliquer pour agrandir le portrait",
        RpProfileZoomClose  = "Clic ou Échap pour fermer",

        RpProfileVisibility       = "Visibilité",
        RpProfileVisWhere         = "Où ma fiche apparaît",
        RpProfileVisWho           = "Qui voit quoi",
        RpProfileVisInGame        = "Visible par les autres",
        RpProfileVisInGameHint    = "Ta fiche est consultable en jeu par les autres joueurs (liste des joueurs disponibles, clic droit) et elle a une adresse partageable que tu peux transmettre. Elle n'est pour autant ni listée dans l'annuaire du site ni inscrite dans les moteurs de recherche.",
        RpProfileVisIndexable     = "Listée sur le site et dans les moteurs",
        RpProfileVisIndexableHint = "Ta fiche est publiée dans l'annuaire des personnages du site, où n'importe qui peut la parcourir et la filtrer, et elle peut apparaître dans les résultats des moteurs de recherche. Une fois indexée, elle peut y rester un moment même après avoir décoché cette case.",
        RpProfileVisAlwaysPublic  = "Niveau, mode d'approche, langues, thèmes, citation et disponibilité restent visibles dès que ta fiche est visible par les autres : ce sont eux qui alimentent le marqueur sur les plaques de nom. La bannière et la couleur d'accent restent visibles également : c'est du décor. Le portrait, lui, suit le réglage de la section Identité.",
        RpProfileTabOverview  = "Aperçu",
        RpProfileTabCharacter = "Personnage",
        RpProfileTabPlay      = "Jeu",
        RpProfileTabLinks     = "Liens & Sync",
        RpProfileTabPrivacy   = "Confidentialité",
        RpProfileTabUnsaved   = "Modifications non enregistrées",
        CfgRpProfileTabs      = "Fiche RP en onglets",
        CfgRpProfileTabsHint  = "Découpe la fiche en cinq onglets au lieu d'une longue page à dérouler.",

        RpProfileAutoSaved        = "Enregistré.",
        RpProfileAutoSaving       = "Enregistrement...",
        RpProfileVisOwnerNote     = "« {0} » veut dire que la section n'est jamais envoyée aux autres joueurs. Ton texte reste enregistré sur le site.",
        RpProfileAudiencePublic   = "Tout le monde",
        RpProfileAudienceOwner    = "Moi seul",
        RpProfileAudienceOwnerFem = "Moi seule",
        RpProfilePreview          = "Aperçu de mon profil",
        RpProfilePreviewTitle     = "Aperçu de ma fiche",
        RpProfilePreviewHint      = "Voici exactement ce que les autres joueurs voient. Les sections réservées ne sont pas envoyées par le serveur, elles sont donc absentes d'ici aussi.",
        RpProfilePreviewHidden    = "Ta fiche n'est pas visible en jeu. Coche « Visible en jeu » pour que les autres puissent la consulter.",
        RpProfileVisSaveFirst     = "Enregistre d'abord : l'aperçu montre ce que le serveur a reçu.",
        RpProfileRefreshHint      = "Recharge ta fiche depuis le site, pour voir ici les modifications que tu viens d'y faire.",
        RpProfileRefreshSaveFirst = "Enregistre d'abord : recharger remplacerait ce que tu es en train de saisir.",
        RpProfileDescription      = "Description",

        RpProfileStaffModerator   = "Modération Eorzea Events",
        RpProfileStaffAdmin       = "Équipe Eorzea Events",
        RpProfileStaffTitle       = "Membre de l'équipe du site Eorzea Events",
        RpProfileStaffBadge       = "Afficher mon statut d'équipe",
        RpProfileStaffBadgeHint   = "Une pastille signale aux autres joueurs que tu fais partie de l'équipe. Utile pour savoir à qui s'adresser en jeu.",

        RpProfileFrameCorners     = "Équerres aux quatre coins, sans mouvement",
        RpProfileFrameRipple      = "Onde qui s'écarte du cadre",
        RpProfileFrameDuo         = "Filet bicolore fixe",
        RpProfileTitleAnimHalo    = "Halo lumineux autour du texte",
        RpProfileTitleAnimDuotone = "Dégradé bicolore, sans mouvement",
        RpProfileTitleAnimWave    = "Lettres qui ondulent",
        RpProfileTitleAnimNeon    = "Vacillement néon",

        TabAround         = "Autour de moi",
        AroundCount       = "{0} joueur(s) disponible(s)",
        AroundRpTaggedCount = "{0} joueur(s) en jeu de rôle",
        AroundRpTaggedHint  = "Ces joueurs ont le tag Jeu de rôle actif et une fiche visible. Ils ne se sont pas déclarés disponibles : à aborder avec le tact qu'on aurait en jeu.",
        AroundRpTaggedChip  = "Tag JDR",
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
        LoadFailed = "Loading failed. The website may be unreachable.",
        SaveFailed = "Saving failed. Nothing was changed on the website.",
        OnSiteCount = "{0} on site",
        DeclaredPosition     = "Declared location",
        DeclaredPositionHint = "Session announced from the website: the location is described by its author, it was not read in-game. Outside housing, nobody can be detected on site.",
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
        DtrRpAvailTooltip = "Availability for spontaneous RP\nIt is only published while the game's \"Role-playing\" tag is on (/roleplaying command)\nClick to mark yourself available or not",
        DtrRpAvailLabel   = "RP avail.",
        DtrRpAvailPausedLabel = "RP avail. (tag off)",
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
        RpAvailableNeedsRpTag   = "Your availability is on hold: it will be published as soon as the game's \"Role-playing\" tag is on (/roleplaying command).",
        RpProfileSetup          = "Set up my RP profile",
        RpProfileWizardTitle    = "My RP Profile",
        RpProfileWizardIntro    = "A few quick questions so other players know what to expect before approaching you.",
        RpProfileTitle          = "My RP profile",
        RpProfileNoCharacter    = "Log in to the game to see your character's profile.",
        RpProfileEditOnline     = "Edit on the website",
        RpProfileWebNoticeTitle = "The full profile is filled in on the website",
        RpProfileWebNoticeBody  = "In game you set what changes often while playing: availability, hooks, physical traits, allegiances, preferences and visibility.\n\nThe portrait, banner, accent colour, identity (race, age, pronouns, origin, occupation), sought and avoided themes, relationships, appearance, personality, background and limits can only be edited on the website. They are shown here read-only.",
        RpAvailableEnableHint   = "Other roleplayers will see you in the available players list.",
        RpProfileHooks          = "Hooks",
        RpProfileHooksHint      = "What makes people want to come and talk to you.",
        RpProfileHooksExample   = "Keeper of the Two Moons tavern",
        // « Right now » désigne désormais le statut du moment : l'intrigue
        // reprend le libellé du site, qui la nomme correctement.
        RpProfileCurrentQuest   = "Current storyline",

        RpProfileGlance         = "At a glance",
        RpProfileGallery        = "Gallery",
        RpProfileGlanceHint     = "Up to five details people notice at first sight, before they even talk to you.",
        RpProfileGlanceBody     = "Description",
        RpProfileGlanceActive   = "Shown",
        RpProfileGlanceExample  = "A scar across the cheek",
        RpProfileGlanceEmpty    = "No detail yet.",
        RpProfileGlanceAdd      = "Add a detail",
        RpProfileGlanceSlot     = "Detail {0}",
        RpProfileGlanceRemove   = "Remove",
        RpProfileGlanceRemoveArm = "Confirm removal",
        RpGlanceIconLabels = new()
        {
            ["sword"] = "Sword", ["shield"] = "Shield", ["book"] = "Book",
            ["scroll"] = "Scroll", ["flask"] = "Flask", ["music"] = "Music",
            ["heart"] = "Heart", ["star"] = "Star", ["coin"] = "Coin",
            ["hammer"] = "Hammer", ["leaf"] = "Leaf", ["flame"] = "Flame",
            ["moon"] = "Moon", ["sun"] = "Sun", ["eye"] = "Eye",
            ["mask"] = "Mask", ["crown"] = "Crown", ["anchor"] = "Anchor",
            ["feather"] = "Feather", ["key"] = "Key", ["skull"] = "Skull",
            ["cup"] = "Cup", ["map"] = "Map", ["paw"] = "Paw",
        },

        RpProfileStatus           = "Right now",
        RpProfileStatusHint       = "What your character is doing at this very moment. Others see it in the list and on your profile.",
        RpProfileCurrently        = "Current status",
        RpProfileCurrentlyExample = "Leaning on the Quicksand bar, looking for a friendly ear",
        RpProfileIcState          = "Play state",
        RpProfileIcStateIc        = "In character",
        RpProfileIcStateOoc       = "Out of character",
        RpProfileIcStateHint      = "Follows the game's \"Role-playing\" tag (/roleplaying command).",

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
        RpProfileContact        = "Getting in touch",
        RpProfileContactDirect  = "Direct",
        RpProfileContactTell    = "A /tell first",
        RpProfileContactEither  = "No preference",
        RpProfileLengths        = "Scene length",
        RpProfileLengthShort    = "Short",
        RpProfileLengthMedium   = "Medium",
        RpProfileLengthLong     = "Long",
        RpProfileThemeSong      = "Theme song",
        RpProfileExternalLink   = "Learn more",
        RpProfileLinks          = "Links",
        RpThemeLabels = new()
        {
            ["tavern"] = "Tavern", ["adventure"] = "Adventure", ["drama"] = "Drama",
            ["romance"] = "Romance", ["lore"] = "Lore-friendly", ["dark"] = "Dark themes",
            ["mystery"] = "Mystery", ["intrigue"] = "Intrigue", ["combat"] = "Combat",
            ["craft"] = "Crafting", ["slice_of_life"] = "Slice of life",
            ["politics"] = "Politics",
        },
        RpRaceLabels = new()
        {
            ["hyur"] = "Hyur", ["elezen"] = "Elezen", ["lalafell"] = "Lalafell",
            ["miqote"] = "Miqo'te", ["roegadyn"] = "Roegadyn", ["aura"] = "Au Ra",
            ["hrothgar"] = "Hrothgar", ["viera"] = "Viera", ["other"] = "Other",
        },
        RpProfileOpenLink       = "Open",
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

        RpProfileSyncshells     = "Sync codes",
        RpProfileSyncshellsHint = "Your Snowcloak, Umbra… IDs. Who sees them is set below, under Visibility.",
        RpProfileSyncshellOther = "Other",
        RpProfileSyncshellName  = "Service name",
        RpProfileSyncshellId    = "ID",
        RpProfileSyncCopy       = "Copy",

        RpProfileQuote          = "Quote",
        RpProfileAvailabilityLabel = "Availability",
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
        CfgRpNameplateNames     = "Show RP names on nameplates",
        CfgRpNameplateNamesHint = "The character name is replaced by their RP name above their head. This covers every player with a public profile and the Roleplaying tag on, not just those flagged as available. Right-click still targets the real character.",

        CfgTabCharacters         = "Characters",
        CfgTabChat               = "Chat",
        CfgTabRp                 = "Roleplay",
        CfgTabNotifications      = "Notifications",
        CfgTabMisc               = "Misc",

        CfgRpTooltipCard         = "Hover tooltip",
        CfgRpTooltip             = "Show a tooltip on available players",
        CfgRpTooltipHint         = "RP name, in-character state and glance appear next to the cursor. Nothing is asked of the server: only players already listed as available show up there.",
        CfgRpTooltipHover        = "Show on mouseover as well",
        CfgRpTooltipHoverHint    = "When off, the tooltip only appears on the selected target.",
        CfgRpTooltipModifier     = "Key to hold",
        CfgRpTooltipModifierHint = "The tooltip stays hidden until the key is held down.",
        CfgRpTooltipModNone      = "None",
        CfgRpTooltipModCtrl      = "Ctrl",
        CfgRpTooltipModAlt       = "Alt",

        CfgChatHeader          = "Chat",
        CfgChatEnabled         = "Chat settings",
        CfgChatEnabledHint     = "Emotes, out-of-character asides and speech are recoloured on display. Nothing is changed when sending: the people you talk to receive exactly what you typed, plugin or no plugin.",
        CfgChatOn              = "On: the settings below apply to your chat.",
        CfgChatOff             = "Off: nothing below has any effect. Ticking a box turns formatting back on.",
        CfgChatEmote           = "Colour emotes",
        CfgChatEmoteHint       = "For example: *opens the door*",
        CfgChatEmoteStyle      = "Recognised delimiters",
        CfgChatEmoteStyleStars = "Asterisks: *text* and **text**",
        CfgChatEmoteStyleAngle = "Angle brackets: <text>",
        CfgChatEmoteStyleBoth  = "Both",
        CfgChatOoc             = "Dim out-of-character asides",
        CfgChatOocHint         = "For example: (back in two minutes)",
        CfgChatSpeech          = "Highlight speech",
        CfgChatSpeechHint      = "Between quotes: « good evening » or \"good evening\"",
        CfgChatColor           = "Colour",
        CfgChatColorDefault    = "The plugin's colour, matched to the game palette",
        CfgChatColorCustom     = "Custom colour…",
        CfgChatColorPicked     = "Chosen colour",
        CfgChatColorRendered   = "As it will appear in chat",
        CfgChatColorHint       = "The game's chat only displays colours from its own palette. The chosen shade is matched to the closest one, shown on the right.",
        CfgChatRpNameAccent    = "The colour is the one from the player's profile, matched to the closest shade the chat can display.",
        CfgChatChannels        = "Channels covered",
        CfgChatChannelsHint    = "Limited to Say and tells by default: elsewhere the log mostly carries game content.",
        CfgChatChanSay         = "Say",
        CfgChatChanTell        = "Tells",
        CfgChatChanShout       = "Shout",
        CfgChatChanYell        = "Yell",
        CfgChatChanParty       = "Party and alliance",
        CfgChatChanLinkshell   = "Linkshells",
        CfgChatChanFreeCompany = "Free company",
        CfgChatChanEmote       = "Game emotes",
        CfgChatRpNames         = "Show RP names",
        CfgChatRpNamesHint     = "The character name is replaced by their RP name, in their profile's colour. Only players already flagged as available are affected: nothing is asked of the server when a message arrives.",
        CfgChatTokens          = "Input tokens",
        CfgChatTokensHint      = "/eorzea rp <text> replaces %xt with your target's RP name and %xp with your own (%xtf, %xtl, %xpf, %xpl for first or last name only), then copies the result to the clipboard. Nothing is sent on your behalf.",
        ChatTokensUsage        = "Usage: /eorzea rp <text>. %xt = target's RP name, %xp = your own, %xtf and %xtl for their first and last name, %xpf and %xpl for yours.",
        ChatTokensCopied       = "Copied to the clipboard: {0}",
        CfgRpNsfwShow            = "Show the content of profiles flagged as sensitive",
        CfgRpNsfwShowHint        = "When off, the tooltip reports the flag but hides the glance and the current status. Opening a full profile depends on your website account instead.",
        RpTooltipNsfwHidden      = "Content hidden by your settings.",

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
        RpProfileNsfwWithheld     = "Profile flagged as sensitive",
        RpProfileNsfwWithheldHint = "Its author flagged it as adults only. The website only sends it to accounts that have accepted this kind of content.",
        RpProfileNsfwWithheldCta  = "Set it on the website",
        RpProfileViewOnSite = "View on the website",
        MenuViewRpProfile   = "View RP profile",
        RpFriendAdd         = "Add as RP friend",
        RpFriendAddHint     = "Opens the sections of YOUR profile reserved for friends. Gives you nothing on theirs, and they will not be told.",
        RpFriendAdded       = "[Eorzea Events] {0} can now see the sections of your profile reserved for your friends.",
        RpFriendRemoved     = "[Eorzea Events] {0} no longer sees the sections reserved for your friends.",
        RpFriendAddFailed   = "The friend list could not be changed.",
        RpFriendAddNotFound = "That character has no profile visible in game: there is nothing to open for them.",
        RpFriendAddLimit    = "Your friend list is full. Remove someone before adding.",
        RpFriendNoToken     = "Link this character in the settings to manage its RP friends.",
        RpFriendChip        = "Friend",
        RpFriendMutual      = "Added you back",
        RpFriendRenamed     = "added as {0}",
        RpFriendNote        = "Private note",
        RpFriendRemove      = "Remove",
        RpFriendRemoveArm   = "Confirm removal",
        TabFriends          = "RP friends",
        RpFriendsTitle      = "Who sees your friends-only sections",
        RpFriendsNoticeBody = "This list opens the sections of YOUR profile set to \"My RP friends\". It gives you access to nothing, and nobody is told they are on it.",
        RpFriendsEmpty      = "No friends yet. Right-click a player in game, then \"Add as RP friend\".",
        RpFriendsNoToken    = "Link this character to manage its RP friends.",
        RpFriendsGoVisibility   = "Set up my sections",
        RpProfileAudienceFriend = "My RP friends",
        RpProfileVisFriendNote  = "\"My RP friends\" only applies in game: those sections appear neither in the available players list nor on the web page. The list is managed in the RP friends tab.",
        RpProfilePresetFriends  = "Open to my friends",
        RpProfilePresetFriendsHint = "Sets every reserved section to \"My RP friends\".",
        RpProfilePreviewAsPublic   = "Seen by everyone",
        RpProfilePreviewAsFriend   = "Seen by a friend",
        RpProfileZoom       = "Click to enlarge the portrait",
        RpProfileZoomClose  = "Click or press Esc to close",

        RpProfileVisibility       = "Visibility",
        RpProfileVisWhere         = "Where my profile appears",
        RpProfileVisWho           = "Who sees what",
        RpProfileVisInGame        = "Visible to others",
        RpProfileVisInGameHint    = "Your profile can be viewed in game by other players (available players list, right-click) and it has a shareable address you can pass around. It is still neither listed in the site directory nor indexed by search engines.",
        RpProfileVisIndexable     = "Listed on the site and in search engines",
        RpProfileVisIndexableHint = "Your profile is published in the site's character directory, where anyone can browse and filter it, and it may appear in search engine results. Once indexed, it can linger there for a while even after you untick this box.",
        RpProfileVisAlwaysPublic  = "Level, approach style, languages, themes, quote and availability stay visible as soon as your profile is visible to others: they are what drives the nameplate marker. The banner and accent colour stay visible too: they are decoration. The portrait, however, follows the Identity section setting.",
        RpProfileTabOverview  = "Overview",
        RpProfileTabCharacter = "Character",
        RpProfileTabPlay      = "Play",
        RpProfileTabLinks     = "Links & Sync",
        RpProfileTabPrivacy   = "Privacy",
        RpProfileTabUnsaved   = "Unsaved changes",
        CfgRpProfileTabs      = "RP profile in tabs",
        CfgRpProfileTabsHint  = "Splits the profile into five tabs instead of one long page to scroll.",

        RpProfileAutoSaved        = "Saved.",
        RpProfileAutoSaving       = "Saving...",
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
        RpProfileRefreshHint      = "Reload your profile from the website, so the changes you just made there show up here.",
        RpProfileRefreshSaveFirst = "Save first: reloading would replace what you are currently typing.",
        RpProfileDescription      = "Description",

        RpProfileStaffModerator   = "Eorzea Events moderation",
        RpProfileStaffAdmin       = "Eorzea Events team",
        RpProfileStaffTitle       = "Member of the Eorzea Events site team",
        RpProfileStaffBadge       = "Show my team status",
        RpProfileStaffBadgeHint   = "A badge tells other players you are part of the team. Handy so they know who to reach out to in game.",

        RpProfileFrameCorners     = "Corner brackets, no motion",
        RpProfileFrameRipple      = "Ripple spreading from the frame",
        RpProfileFrameDuo         = "Two-tone outline, no motion",
        RpProfileTitleAnimHalo    = "Glowing halo around the text",
        RpProfileTitleAnimDuotone = "Two-tone gradient, no motion",
        RpProfileTitleAnimWave    = "Letters rippling in a wave",
        RpProfileTitleAnimNeon    = "Neon flicker",

        TabAround         = "Around me",
        AroundCount       = "{0} player(s) available",
        AroundRpTaggedCount = "{0} player(s) in character",
        AroundRpTaggedHint  = "These players have the Role-playing tag on and a visible profile. They have not declared themselves available: approach them with the tact you would use in game.",
        AroundRpTaggedChip  = "RP tag",
        AroundEmpty       = "Nobody available right now",
        AroundNoMatch     = "No player matches",
        AroundSearchHint  = "Search for a character",
        AroundMyWorldOnly = "My world only",
    };
}

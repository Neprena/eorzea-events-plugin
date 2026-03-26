namespace EorzeaEventsPlugin;

/// <summary>All user-facing strings for FR and EN.</summary>
internal sealed class Loc
{
    // ── Tabs ──────────────────────────────────────────────────────────────────
    public required string TabRp       { get; init; }
    public required string TabEvents   { get; init; }
    public required string TabEstabs   { get; init; }
    public required string TabSettings { get; init; }

    // ── Common ────────────────────────────────────────────────────────────────
    public required string Loading    { get; init; }
    public required string Refresh    { get; init; }
    public required string ViewOnline { get; init; }
    public required string Open       { get; init; }
    public required string Ongoing    { get; init; }
    public required string Recurring  { get; init; }
    public required string Map        { get; init; }
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

    // ── Events tab ────────────────────────────────────────────────────────────
    public required string EventsNoEvents { get; init; }
    public required string EventsCount    { get; init; }   // {0} événement(s)
    public required string EventsOngoing  { get; init; }   // {0} en cours
    public required string EventsTotal    { get; init; }   // · {0} au total

    // ── Establishments tab ────────────────────────────────────────────────────
    public required string EstabSearchHint { get; init; }
    public required string EstabNoResults  { get; init; }
    public required string EstabCount      { get; init; }   // {0} établissement(s)
    public required Dictionary<string, string> DistrictLabels { get; init; }

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

    // ── Setup window ──────────────────────────────────────────────────────────
    public required string SetupWelcomeL1       { get; init; }
    public required string SetupWelcomeL2       { get; init; }
    public required string SetupWelcomeL3       { get; init; }
    public required string SetupStart           { get; init; }
    public required string SetupStepTitle       { get; init; }
    public required string SetupStepDesc        { get; init; }
    public required string SetupOpenDashboard   { get; init; }
    public required string SetupTokenLabel      { get; init; }
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
    public required string CfgNotifNearby       { get; init; }
    public required string CfgNotifNearbyHint   { get; init; }
    public required string CfgSessionHeader     { get; init; }
    public required string CfgSuggestOnTag      { get; init; }
    public required string CfgAlertZone         { get; init; }
    public required string CfgAlertTag          { get; init; }
    public required string CfgLangHeader        { get; init; }
    public required string CfgLangAuto          { get; init; }
    public required string CfgLangFr            { get; init; }
    public required string CfgLangEn            { get; init; }
    public required string CfgTest              { get; init; }

    // ── Token invalid / blocked screens ──────────────────────────────────────
    public required string TokenInvalidLine1    { get; init; }
    public required string TokenInvalidLine2    { get; init; }
    public required string TokenInvalidLine3    { get; init; }
    public required string TokenReconfigure     { get; init; }
    public required string BlockedHint          { get; init; }

    // ── Notifications (toast / chat) ──────────────────────────────────────────
    public required string NotifTokenTitle      { get; init; }
    public required string NotifTokenContent    { get; init; }
    public required string NotifNewRpTitle      { get; init; }
    public required string NotifNearbyRp        { get; init; }   // {0} = title
    public required string NotifNewRpScreen     { get; init; }   // {0}=title {1}=loc {2}=server
    public required string NotifNewRpChat       { get; init; }   // {0}=title {1}=loc {2}=server
    public required string DtrRpTooltip         { get; init; }
    public required string DtrEventsTooltip     { get; init; }

    // ── Static instances ──────────────────────────────────────────────────────

    public static readonly Loc Fr = new()
    {
        TabRp       = "RP Ouvert",
        TabEvents   = "Evenements",
        TabEstabs   = "Lieux",
        TabSettings = "Parametres",

        Loading    = "Chargement...",
        Refresh    = "Actualiser",
        ViewOnline = "Voir en ligne",
        Open       = "Ouvrir",
        Ongoing    = "EN COURS",
        Recurring  = "recurrent",
        Map        = "Carte",
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
        RpBeFirst           = "Soyez le premier a en demarrer une !",
        RpInYourZone        = "✦  Dans votre zone ({0})",
        RpOtherServers      = "── Autres serveurs ──────────────────────────────────",
        RpYourSessionActive = "Votre session est en cours.",
        RpManageSession     = "Gerer ma session",
        RpNewSession        = "Nouvelle session RP ouverte",
        RpResume            = "Reprendre",

        EventsNoEvents = "Aucun evenement dans les 14 prochains jours.",
        EventsCount    = "{0} evenement(s)",
        EventsOngoing  = "{0} en cours",
        EventsTotal    = "· {0} evenement(s) au total",

        EstabSearchHint = "Recherchez par nom, serveur ou quartier.",
        EstabNoResults  = "Aucun resultat.",
        EstabCount      = "{0} lieu(x)",
        DistrictLabels  = new()
        {
            ["brumee"]     = "Brumee",
            ["lavandiere"] = "Lavandiere",
            ["coupe"]      = "La Coupe",
            ["shirogane"]  = "Shirogane",
            ["empyree"]    = "Empyree",
        },

        MySessionTitle        = "Ma session RP ouverte",
        SessionCreate         = "Nouvelle session RP ouverte",
        SessionActive         = "Session en cours",
        FieldTitle            = "Titre",
        FieldDesc             = "Description",
        FieldLocation         = "Zone",
        FieldServer           = "Serveur",
        FieldCharName         = "Nom du personnage",
        FieldDuration         = "Duree (heures)",
        FieldWard             = "Quartier",
        FieldPlot             = "Parcelle",
        FieldRoom             = "Appartement",
        FieldPosition         = "Position",
        FieldHousing          = "Logement",
        HousingWardPlot       = "Quartier {0}  —  Parcelle {1}",
        HousingWardRoom       = "Quartier {0}  —  Appartement {1}",
        HousingWard           = "Quartier {0}",
        WorldUnknown          = "Monde inconnu",
        ZoneUnknown           = "Zone inconnue",
        BtnCreate             = "Creer",
        BtnModify             = "Modifier",
        BtnEnd                = "Terminer la session",
        BtnUpdatePos          = "Update de la position",
        BtnExtend             = "Prolonger (+1h)",
        BtnConfigureNow       = "Configurer maintenant",
        StatusPosUpdated      = "Position mise a jour.",
        StatusCreating        = "Creation en cours...",
        StatusUpdating        = "Mise a jour...",
        StatusEnding          = "Fin de session...",
        StatusStarted         = "Session demarree !",
        StatusRecovered       = "Session existante recuperee.",
        StatusUpdated         = "Session mise a jour.",
        StatusEnded           = "Session terminee.",
        StatusExtended        = "Session prolongee de {0}h.",
        ErrCreate             = "Erreur lors de la creation.",
        ErrUpdate             = "Erreur lors de la mise a jour.",
        ErrExtend             = "Erreur lors de la prolongation.",
        ErrTitleRequired      = "Le titre est obligatoire.",
        ErrTokenMissing       = "Token API non configure.",
        HintNoLocation        = "Zone introuvable, remplissez manuellement.",
        HintNoServer          = "Serveur introuvable, remplissez manuellement.",
        ExpiresIn             = "Expire dans environ",
        Hours                 = "heure(s)",
        MySessionTokenMissingDesc = "Genere un token depuis ton dashboard pour acceder aux sessions RP.",
        MySessionTokenInvalidDesc = "Tu dois generer un nouveau token pour continuer a utiliser le plugin.",
        AlertZoneChanged      = "Vous avez change de zone.",
        AlertRpTagRemoved     = "Vous avez retire le tag RP.",
        AlertRpTagActivated   = "Tag RP active.",
        AlertZoneChangedTitle = "⚠  Changement de zone detecte",
        AlertZoneChangedDesc  = "Voulez-vous mettre a jour votre emplacement ou terminer la session ?",
        AlertRpTagRemovedTitle = "⚠  Tag RP retire",
        AlertRpTagRemovedDesc  = "Vous n'etes plus en mode RP. Souhaitez-vous terminer la session ?",
        AlertRpTagActivTitle  = "✦  Tag RP active !",
        AlertRpTagActivDesc   = "Vous etes en mode RP. Souhaitez-vous annoncer une session RP ouverte ?",

        SetupWelcomeL1     = "Ce plugin fonctionne de pair avec le site",
        SetupWelcomeL2     = "Il vous permet de gerer vos sessions RP ouvertes directement depuis FFXIV, sans quitter le jeu.",
        SetupWelcomeL3     = "La configuration ne prend que quelques secondes.",
        SetupStart         = "Commencer",
        SetupStepTitle     = "Etape 1 / 1 — Token API",
        SetupStepDesc      = "Generez un token API sur votre dashboard, puis collez-le ici.",
        SetupOpenDashboard = "Ouvrir le dashboard eorzea.events",
        SetupTokenLabel    = "Token API :",
        SetupTokenInvalid  = "Ton token API est expire ou invalide.\nGenere-en un nouveau depuis le dashboard pour continuer.",
        SetupErrPrefix     = "Le token doit commencer par « ee_ ».",
        SetupSkip          = "Passer",
        SetupDoneTitle     = "Tout est pret !",
        SetupDoneL1        = "Votre token est enregistre. Vous pouvez maintenant creer",
        SetupDoneL2        = "des sessions RP directement depuis le jeu.",
        SetupDoneHint      = "Utilisez /eorzea pour ouvrir le panneau principal.",
        SetupOpenPlugin    = "Ouvrir Eorzea Events",

        CfgTokenLabel       = "Token API :",
        CfgTokenOk          = "Configure",
        CfgTokenMissing     = "Non configure",
        CfgTokenEdit        = "Modifier",
        CfgNotifHeader      = "Quand une nouvelle session RP est annoncee",
        CfgNotifScreen      = "Afficher une alerte au centre de l'ecran",
        CfgNotifScreenHint  = "   Style natif FFXIV, comme les messages de bienvenue",
        CfgNotifDalamud     = "Afficher une bulle de notification",
        CfgNotifDalamudHint = "   Petite carte dans le coin superieur droit",
        CfgNotifChat        = "Ecrire un message dans le chat",
        CfgNotifMyWorld     = "Ignorer les sessions sur d'autres serveurs",
        CfgNotifNearby      = "Alerte prioritaire si la session est dans ma zone actuelle",
        CfgNotifNearbyHint  = "   Meme serveur et meme zone",
        CfgSessionHeader    = "Quand j'ai une session RP en cours",
        CfgSuggestOnTag     = "Me proposer de demarrer une session quand j'active le tag RP",
        CfgAlertZone        = "Me prevenir si je change de zone ou effectue un TP",
        CfgAlertTag         = "Me prevenir si je retire le tag RP",
        CfgLangHeader       = "Langue du plugin",
        CfgLangAuto         = "Automatique (langue du jeu)",
        CfgLangFr           = "Francais",
        CfgLangEn           = "English",
        CfgTest             = "Tester",

        TokenInvalidLine1 = "Token API invalide ou expire.",
        TokenInvalidLine2 = "Tu dois en generer un nouveau pour continuer",
        TokenInvalidLine3 = "a utiliser Eorzea Events.",
        TokenReconfigure  = "Reconfigurer le token",
        BlockedHint       = "Tape /xlplugins en jeu pour ouvrir le gestionnaire de plugins.",

        NotifTokenTitle   = "Token API expire — Eorzea Events",
        NotifTokenContent = "Ton token API n'est plus valide. Genere-en un nouveau depuis ton tableau de bord.",
        NotifNewRpTitle   = "Nouvelle session RP ouverte",
        NotifNearbyRp     = "RP ouvert dans votre zone !\n{0}",
        NotifNewRpScreen  = "Nouveau RP ouvert !\n{0} — {1} ({2})",
        NotifNewRpChat    = "Nouveau RP ouvert : {0} — {1} ({2})",
        DtrRpTooltip      = "Sessions RP ouvertes en cours\nCliquez pour ouvrir",
        DtrEventsTooltip  = "Evenements en cours\nCliquez pour ouvrir",
    };

    public static readonly Loc En = new()
    {
        TabRp       = "Open RP",
        TabEvents   = "Events",
        TabEstabs   = "Venues",
        TabSettings = "Settings",

        Loading    = "Loading...",
        Refresh    = "Refresh",
        ViewOnline = "View online",
        Open       = "Open",
        Ongoing    = "ONGOING",
        Recurring  = "recurring",
        Map        = "Map",
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
        RpInYourZone        = "✦  In your zone ({0})",
        RpOtherServers      = "── Other servers ────────────────────────────────────",
        RpYourSessionActive = "Your session is active.",
        RpManageSession     = "Manage my session",
        RpNewSession        = "New open RP session",
        RpResume            = "Resume",

        EventsNoEvents = "No events in the next 14 days.",
        EventsCount    = "{0} event(s)",
        EventsOngoing  = "{0} ongoing",
        EventsTotal    = "· {0} event(s) total",

        EstabSearchHint = "Search by name, server or ward.",
        EstabNoResults  = "No results found.",
        EstabCount      = "{0} venue(s)",
        DistrictLabels  = new()
        {
            ["brumee"]     = "The Mist",
            ["lavandiere"] = "The Lavender Beds",
            ["coupe"]      = "The Goblet",
            ["shirogane"]  = "Shirogane",
            ["empyree"]    = "The Empyrean",
        },

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
        MySessionTokenMissingDesc = "Generate a token from your dashboard to access RP sessions.",
        MySessionTokenInvalidDesc = "You need to generate a new token to continue using the plugin.",
        AlertZoneChanged      = "You changed zone.",
        AlertRpTagRemoved     = "You removed the RP tag.",
        AlertRpTagActivated   = "RP tag activated.",
        AlertZoneChangedTitle = "⚠  Zone change detected",
        AlertZoneChangedDesc  = "Do you want to update your location or end the session?",
        AlertRpTagRemovedTitle = "⚠  RP tag removed",
        AlertRpTagRemovedDesc  = "You're no longer in RP mode. Do you want to end the session?",
        AlertRpTagActivTitle  = "✦  RP tag activated!",
        AlertRpTagActivDesc   = "You're in RP mode. Do you want to announce an open RP session?",

        SetupWelcomeL1     = "This plugin works alongside the website",
        SetupWelcomeL2     = "It lets you manage your open RP sessions directly from FFXIV, without leaving the game.",
        SetupWelcomeL3     = "Setup only takes a few seconds.",
        SetupStart         = "Get started",
        SetupStepTitle     = "Step 1 / 1 — API Token",
        SetupStepDesc      = "Generate an API token from your dashboard, then paste it here.",
        SetupOpenDashboard = "Open eorzea.events dashboard",
        SetupTokenLabel    = "API Token:",
        SetupTokenInvalid  = "Your API token is expired or invalid.\nGenerate a new one from the dashboard to continue.",
        SetupErrPrefix     = "Token must start with \"ee_\".",
        SetupSkip          = "Skip",
        SetupDoneTitle     = "All set!",
        SetupDoneL1        = "Your token is saved. You can now create",
        SetupDoneL2        = "RP sessions directly from the game.",
        SetupDoneHint      = "Use /eorzea to open the main panel.",
        SetupOpenPlugin    = "Open Eorzea Events",

        CfgTokenLabel       = "API Token:",
        CfgTokenOk          = "Configured",
        CfgTokenMissing     = "Not configured",
        CfgTokenEdit        = "Edit",
        CfgNotifHeader      = "When a new RP session is announced",
        CfgNotifScreen      = "Show an alert in the center of the screen",
        CfgNotifScreenHint  = "   Native FFXIV style, like welcome messages",
        CfgNotifDalamud     = "Show a notification bubble",
        CfgNotifDalamudHint = "   Small card in the top-right corner",
        CfgNotifChat        = "Print a message in the chat",
        CfgNotifMyWorld     = "Ignore sessions on other servers",
        CfgNotifNearby      = "Priority alert if the session is in my current zone",
        CfgNotifNearbyHint  = "   Same server and same zone",
        CfgSessionHeader    = "While I have an active RP session",
        CfgSuggestOnTag     = "Suggest starting a session when I enable the RP tag",
        CfgAlertZone        = "Warn me if I change zone or teleport",
        CfgAlertTag         = "Warn me if I remove the RP tag",
        CfgLangHeader       = "Plugin language",
        CfgLangAuto         = "Auto (game language)",
        CfgLangFr           = "Francais",
        CfgLangEn           = "English",
        CfgTest             = "Test",

        TokenInvalidLine1 = "API token invalid or expired.",
        TokenInvalidLine2 = "You need to generate a new one to continue",
        TokenInvalidLine3 = "using Eorzea Events.",
        TokenReconfigure  = "Reconfigure token",
        BlockedHint       = "Type /xlplugins in-game to open the plugin manager.",

        NotifTokenTitle   = "API token expired — Eorzea Events",
        NotifTokenContent = "Your API token is no longer valid. Generate a new one from your dashboard.",
        NotifNewRpTitle   = "New Open RP Session",
        NotifNearbyRp     = "Open RP in your zone!\n{0}",
        NotifNewRpScreen  = "New open RP!\n{0} — {1} ({2})",
        NotifNewRpChat    = "New open RP: {0} — {1} ({2})",
        DtrRpTooltip      = "Active open RP sessions\nClick to open",
        DtrEventsTooltip  = "Ongoing events\nClick to open",
    };
}

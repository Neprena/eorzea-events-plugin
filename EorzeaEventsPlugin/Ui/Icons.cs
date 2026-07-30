using Dalamud.Interface;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Vocabulaire d'icônes du plugin.
///
/// Aucun glyphe ne doit être écrit en dur dans le code : les points de code
/// FontAwesome vivent dans la zone à usage privé (U+F000 et au-delà), invisibles
/// dans un éditeur et silencieusement perdus au moindre accident d'encodage.
/// C'est exactement ce qui est arrivé aux sept appels <c>DrawIcon("")</c> de
/// <c>MySessionWindow</c>, qui passent aujourd'hui une chaîne vide.
///
/// Nommer les icônes par leur sens plutôt que par leur apparence permet aussi
/// de changer le pictogramme d'un concept en un seul endroit.
/// </summary>
internal static class Icons
{
    // ─── Navigation ───────────────────────────────────────────────────────────
    public const FontAwesomeIcon RpLive   = FontAwesomeIcon.TheaterMasks;
    public const FontAwesomeIcon Around   = FontAwesomeIcon.Users;
    public const FontAwesomeIcon Events   = FontAwesomeIcon.CalendarAlt;
    public const FontAwesomeIcon Venues   = FontAwesomeIcon.Store;
    public const FontAwesomeIcon Profile  = FontAwesomeIcon.IdCard;
    public const FontAwesomeIcon Settings = FontAwesomeIcon.Cog;
    public const FontAwesomeIcon Debug    = FontAwesomeIcon.Bug;

    // ─── Contexte in-game ─────────────────────────────────────────────────────
    public const FontAwesomeIcon Location  = FontAwesomeIcon.MapMarkerAlt;
    public const FontAwesomeIcon Character = FontAwesomeIcon.User;
    public const FontAwesomeIcon Housing   = FontAwesomeIcon.Home;
    public const FontAwesomeIcon Map       = FontAwesomeIcon.Map;
    public const FontAwesomeIcon World     = FontAwesomeIcon.Globe;
    public const FontAwesomeIcon Clock     = FontAwesomeIcon.Clock;
    public const FontAwesomeIcon Recurring = FontAwesomeIcon.Redo;
    public const FontAwesomeIcon Language  = FontAwesomeIcon.Comments;

    // ─── Actions ──────────────────────────────────────────────────────────────
    public const FontAwesomeIcon Search   = FontAwesomeIcon.Search;
    public const FontAwesomeIcon Refresh  = FontAwesomeIcon.SyncAlt;
    public const FontAwesomeIcon External = FontAwesomeIcon.ExternalLinkAlt;
    public const FontAwesomeIcon Close    = FontAwesomeIcon.Times;
    public const FontAwesomeIcon Hide     = FontAwesomeIcon.EyeSlash;
    public const FontAwesomeIcon Show     = FontAwesomeIcon.Eye;
    public const FontAwesomeIcon Copy     = FontAwesomeIcon.Copy;
    public const FontAwesomeIcon Edit     = FontAwesomeIcon.Pen;
    public const FontAwesomeIcon Plus     = FontAwesomeIcon.Plus;
    public const FontAwesomeIcon Trash    = FontAwesomeIcon.TrashAlt;
    public const FontAwesomeIcon Travel   = FontAwesomeIcon.PaperPlane;

    // ─── États ────────────────────────────────────────────────────────────────
    public const FontAwesomeIcon Warning = FontAwesomeIcon.ExclamationTriangle;
    public const FontAwesomeIcon Info    = FontAwesomeIcon.InfoCircle;
    public const FontAwesomeIcon Check   = FontAwesomeIcon.Check;
    public const FontAwesomeIcon Blocked = FontAwesomeIcon.Ban;
    public const FontAwesomeIcon Sparkle = FontAwesomeIcon.Star;
    public const FontAwesomeIcon Diamond = FontAwesomeIcon.Gem;
    public const FontAwesomeIcon Chevron = FontAwesomeIcon.ChevronRight;

    // ─── Amis RP ──────────────────────────────────────────────────────────────
    public const FontAwesomeIcon Friend    = FontAwesomeIcon.UserFriends;
    public const FontAwesomeIcon FriendAdd = FontAwesomeIcon.UserPlus;

    /// <summary>
    /// Toutes les icônes réellement utilisées. Sert à ne fusionner dans l'atlas
    /// que la trentaine de glyphes nécessaires au lieu des ~2000 de FontAwesome.
    /// Toute icône ajoutée ci-dessus doit être reportée ici, sinon elle
    /// s'affichera en carré vide.
    /// </summary>
    public static readonly FontAwesomeIcon[] All =
    [
        RpLive, Around, Events, Venues, Profile, Settings, Debug,
        Location, Character, Housing, Map, World, Clock, Recurring, Language,
        Search, Refresh, External, Close, Hide, Show, Copy, Edit, Plus, Trash, Travel,
        Warning, Info, Check, Blocked, Sparkle, Diamond, Chevron,
        Friend, FriendAdd,
    ];

    /// <summary>Glyphe prêt à être passé à ImGui.</summary>
    public static string S(this FontAwesomeIcon icon) => icon.ToIconString();
}

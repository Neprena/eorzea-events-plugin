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
    public const FontAwesomeIcon Chat      = FontAwesomeIcon.CommentDots;

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

    /// <summary>Statut d'équipe : modération ou administration.</summary>
    public const FontAwesomeIcon Shield  = FontAwesomeIcon.ShieldAlt;

    // ─── Amis RP ──────────────────────────────────────────────────────────────
    public const FontAwesomeIcon Friend    = FontAwesomeIcon.UserFriends;
    public const FontAwesomeIcon FriendAdd = FontAwesomeIcon.UserPlus;

    // ─── Coup d'œil ───────────────────────────────────────────────────────────
    //
    // Vocabulaire fermé de 24 clés, aligné sur RP_GLANCE_ICONS
    // (src/lib/rp-vocabulary.ts). Les constantes portent le sens de la clé et
    // non le nom du glyphe retenu : changer de pictogramme ne doit toucher
    // qu'une ligne, et surtout jamais le vocabulaire stocké en base.

    public const FontAwesomeIcon GlanceSword   = FontAwesomeIcon.Khanda;
    public const FontAwesomeIcon GlanceShield  = FontAwesomeIcon.ShieldAlt;
    public const FontAwesomeIcon GlanceBook    = FontAwesomeIcon.Book;
    public const FontAwesomeIcon GlanceScroll  = FontAwesomeIcon.Scroll;
    public const FontAwesomeIcon GlanceFlask   = FontAwesomeIcon.Flask;
    public const FontAwesomeIcon GlanceMusic   = FontAwesomeIcon.Music;
    public const FontAwesomeIcon GlanceHeart   = FontAwesomeIcon.Heart;
    public const FontAwesomeIcon GlanceStar    = FontAwesomeIcon.Star;
    public const FontAwesomeIcon GlanceCoin    = FontAwesomeIcon.Coins;
    public const FontAwesomeIcon GlanceHammer  = FontAwesomeIcon.Hammer;
    public const FontAwesomeIcon GlanceLeaf    = FontAwesomeIcon.Leaf;
    public const FontAwesomeIcon GlanceFlame   = FontAwesomeIcon.Fire;
    public const FontAwesomeIcon GlanceMoon    = FontAwesomeIcon.Moon;
    public const FontAwesomeIcon GlanceSun     = FontAwesomeIcon.Sun;
    public const FontAwesomeIcon GlanceEye     = FontAwesomeIcon.Eye;
    public const FontAwesomeIcon GlanceMask    = FontAwesomeIcon.Mask;
    public const FontAwesomeIcon GlanceCrown   = FontAwesomeIcon.Crown;
    public const FontAwesomeIcon GlanceAnchor  = FontAwesomeIcon.Anchor;
    public const FontAwesomeIcon GlanceFeather = FontAwesomeIcon.Feather;
    public const FontAwesomeIcon GlanceKey     = FontAwesomeIcon.Key;
    public const FontAwesomeIcon GlanceSkull   = FontAwesomeIcon.Skull;
    public const FontAwesomeIcon GlanceCup     = FontAwesomeIcon.WineGlassAlt;
    public const FontAwesomeIcon GlanceMap     = FontAwesomeIcon.Map;
    public const FontAwesomeIcon GlancePaw     = FontAwesomeIcon.Paw;

    /// <summary>
    /// Glyphe d'une clé du coup d'œil. Une clé inconnue, servie par un serveur
    /// plus récent, retombe sur l'étoile : un carré vide en dirait moins qu'une
    /// icône approximative.
    /// </summary>
    public static FontAwesomeIcon Glance(string key) => key switch
    {
        "sword"   => GlanceSword,
        "shield"  => GlanceShield,
        "book"    => GlanceBook,
        "scroll"  => GlanceScroll,
        "flask"   => GlanceFlask,
        "music"   => GlanceMusic,
        "heart"   => GlanceHeart,
        "star"    => GlanceStar,
        "coin"    => GlanceCoin,
        "hammer"  => GlanceHammer,
        "leaf"    => GlanceLeaf,
        "flame"   => GlanceFlame,
        "moon"    => GlanceMoon,
        "sun"     => GlanceSun,
        "eye"     => GlanceEye,
        "mask"    => GlanceMask,
        "crown"   => GlanceCrown,
        "anchor"  => GlanceAnchor,
        "feather" => GlanceFeather,
        "key"     => GlanceKey,
        "skull"   => GlanceSkull,
        "cup"     => GlanceCup,
        "map"     => GlanceMap,
        "paw"     => GlancePaw,
        _         => Sparkle,
    };

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
        Warning, Info, Check, Blocked, Sparkle, Diamond, Chevron, Shield,
        Friend, FriendAdd,

        // Les 24 glyphes du coup d'œil : le joueur choisit librement parmi eux,
        // n'importe lequel peut donc apparaître sur n'importe quelle fiche.
        GlanceSword, GlanceShield, GlanceBook, GlanceScroll, GlanceFlask, GlanceMusic,
        GlanceHeart, GlanceStar, GlanceCoin, GlanceHammer, GlanceLeaf, GlanceFlame,
        GlanceMoon, GlanceSun, GlanceEye, GlanceMask, GlanceCrown, GlanceAnchor,
        GlanceFeather, GlanceKey, GlanceSkull, GlanceCup, GlanceMap, GlancePaw,
    ];

    /// <summary>Glyphe prêt à être passé à ImGui.</summary>
    public static string S(this FontAwesomeIcon icon) => icon.ToIconString();
}

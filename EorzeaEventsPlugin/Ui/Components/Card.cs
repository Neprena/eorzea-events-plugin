using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Ciel étoilé peint à la place d'une bannière absente : couleurs du haut et du
/// milieu, deux halos. Le bas se fond dans le fond de la carte.
/// </summary>
internal readonly record struct CardSky(Vector4 Top, Vector4 Middle, Vector4 Glow, Vector4 Glow2, bool Animated);

internal enum CardTone
{
    /// <summary>Surface neutre.</summary>
    Flat,

    /// <summary>Surface qui s'éclaircit au survol, pour un élément de liste.</summary>
    Interactive,
}

/// <summary>
/// Portée d'une carte. <c>ref struct</c> délibéré : il y a une carte par entrée
/// de liste et par frame, un type par référence allouerait à chaque image.
/// </summary>
internal ref struct CardScope
{
    // La clé est capturée à l'ouverture, avant le PushID de la carte.
    // La recalculer à la fermeture donnerait un identifiant différent, calculé
    // dans la pile d'identifiants de la carte : le cache ne serait jamais relu
    // et toutes les cartes garderaient la hauteur estimée par défaut.
    private readonly uint _key;
    private readonly Vector2 _origin;
    private readonly float _previousInset;

    /// <summary>Vrai si le curseur est sur la carte.</summary>
    public bool Hovered { get; }

    internal CardScope(uint key, Vector2 origin, bool hovered, float previousInset)
    {
        _key           = key;
        _origin        = origin;
        _previousInset = previousInset;
        Hovered        = hovered;
    }

    public void Dispose()
    {
        ImGui.Unindent(Theme.S(Theme.CardPadX));
        Card.RightInset = _previousInset;

        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.CardPadY)));

        // La hauteur réelle sert au rendu du fond de la frame suivante.
        Card.Remember(_key, ImGui.GetCursorScreenPos().Y - _origin.Y);

        ImGui.PopID();

        // Respiration entre deux cartes. L'espacement d'items d'ImGui, prévu
        // pour des lignes de texte, ne suffit pas à séparer deux surfaces
        // portant leur propre ombre : elles paraissent collées.
        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapM)));
    }
}

/// <summary>
/// Cartes.
///
/// L'implémentation historique découpait la liste de dessin en canaux
/// (<c>ChannelsSplit</c>) pour peindre le fond après avoir mesuré le contenu.
/// Ce mécanisme n'est pas réentrant : deux cartes imbriquées, ou une carte
/// contenant un nœud déplié, corrompaient l'ordre de rendu.
///
/// Ici la hauteur mesurée à la frame précédente est mémorisée, ce qui permet de
/// peindre le fond <em>avant</em> le contenu. La carte n'est donc exacte qu'à
/// partir de la deuxième frame, ce qui est imperceptible, et deux bénéfices
/// apparaissent : l'imbrication devient sûre, et le rectangle étant connu à
/// l'avance, le survol de la carte entière devient possible.
/// </summary>
internal static class Card
{
    private static readonly Dictionary<uint, float> Heights = [];

    private const float EstimatedHeight = 64f;

    /// <summary>Air entre une carte et la barre de défilement de sa zone.</summary>
    private const float ScrollGutter = 6f;

    /// <summary>
    /// Marge droite de la carte en cours, en pixels déjà mis à l'échelle.
    ///
    /// <c>ImGui.Indent</c> ne décale que le bord gauche : sans ce retrait, la
    /// largeur disponible mesurée à l'intérieur va jusqu'au bord de la carte et
    /// tout ce qui s'aligne à droite vient s'y coller.
    /// </summary>
    internal static float RightInset { get; set; }

    /// <summary>
    /// Largeur utile, marge droite déduite. À passer à
    /// <c>ImGui.SetNextItemWidth</c> pour un élément pleine largeur.
    /// </summary>
    public static float FullWidth => -Math.Max(1f, RightInset);

    internal static void Remember(uint key, float height) => Heights[key] = height;

    /// <summary>
    /// Ouvre une carte. L'identifiant doit être stable d'une frame à l'autre
    /// (par exemple l'identifiant métier de l'élément) : une clé changeante
    /// invalide le cache de hauteur et fait vibrer le fond.
    /// </summary>
    public static CardScope Begin(string id,
                                  CardTone tone = CardTone.Flat,
                                  bool interactive = true,
                                  Vector4? background = null,
                                  Vector4? border = null,
                                  Vector4? accent = null,
                                  IDalamudTextureWrap? banner = null,
                                  float bannerHeight = 96f,
                                  CardSky? sky = null)
    {
        // La bannière l'emporte toujours : le ciel n'est qu'un repli.
        var art = banner != null || sky != null;

        var key    = ImGui.GetID(id);
        var origin = ImGui.GetCursorScreenPos();
        // Quand la zone défile, la carte s'arrête avant la barre de défilement
        // au lieu de la toucher : ImGui ne sait pas mettre de marge entre les
        // deux, et le contenu venait s'y coller.
        // Seulement pour une carte de premier niveau : une carte imbriquée
        // hérite déjà du retrait de sa parente.
        var gutter = RightInset == 0f && ImGui.GetScrollMaxY() > 0f ? Theme.S(ScrollGutter) : 0f;
        var width  = ImGui.GetContentRegionAvail().X - gutter;
        // À la toute première frame la hauteur réelle est inconnue. L'estimation
        // inclut la bannière, sans quoi celle-ci recouvrirait entièrement la carte.
        var height = Heights.TryGetValue(key, out var cached)
            ? cached
            : Theme.S(EstimatedHeight + (art ? bannerHeight : 0f));

        var min = origin;
        var max = new Vector2(origin.X + width, origin.Y + height);

        var hovered = interactive
                      && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows)
                      && ImGui.IsMouseHoveringRect(min, max);

        // Translucides sur la nuit, pour laisser voir le halo. Posées sur la
        // fenêtre, elles ne descendent jamais sous son opacité.
        var bg = background ?? (tone == CardTone.Interactive && hovered
            ? Theme.CardFillHover
            : Theme.CardFill);

        var dl = ImGui.GetWindowDrawList();
        Surface.Panel(dl, min, max, bg, border ?? Theme.Border, highlight: !art);
        if (!art) Seasons.SeasonDecor.CardCap(dl, min, max);

        // Pas de dégradé de fond ici, à dessein : la liste de dessin ne sait pas
        // remplir un rectangle arrondi en dégradé. Un rectangle inséré sous le
        // rayon laisse des encoches aux angles, un rectangle pleine largeur
        // déborde des arrondis, et un dégradé plus court que la carte crée une
        // arête horizontale là où il s'arrête. La teinte plate de `background`
        // suffit : elle est assez discrète pour ne pas réclamer de fondu.
        if (banner != null)
            DrawBanner(dl, min, width, Theme.S(bannerHeight), banner);
        else if (sky is { } night)
            Surface.StarrySky(dl, min, new Vector2(min.X + width, min.Y + Theme.S(bannerHeight)),
                              Theme.S(Theme.RadiusCard), night.Top, night.Middle,
                              // Le milieu rendu transparent, et non le fond de la
                              // carte : ImGui interpole sans prémultiplier, et
                              // un fond translucide blanc donnerait une bande
                              // grise à mi-hauteur.
                              Theme.Alpha(night.Middle, 0f),
                              night.Glow, night.Glow2, night.Animated);
        if (accent is { } accentColor) Surface.AccentBar(dl, min, max, accentColor);

        ImGui.PushID(id);

        if (art) ImGui.Dummy(new Vector2(0f, Theme.S(bannerHeight)));
        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.CardPadY)));

        var previousInset = RightInset;
        ImGui.Indent(Theme.S(Theme.CardPadX));
        RightInset = previousInset + Theme.S(Theme.CardPadX) + gutter;

        return new CardScope(key, origin, hovered, previousInset);
    }

    /// <summary>
    /// Bannière recadrée en mode « couvrir » : hauteur fixe, rognage au centre,
    /// jamais de déformation.
    /// </summary>
    private static void DrawBanner(ImDrawListPtr dl, Vector2 origin, float width,
                                   float height, IDalamudTextureWrap texture)
    {
        var (uv0, uv1) = Surface.CoverUv(texture.Width, texture.Height, width, height);

        var end = new Vector2(origin.X + width, origin.Y + height);
        dl.AddImageRounded(texture.Handle, origin, end,
            uv0, uv1,
            ImGui.GetColorU32(Vector4.One),
            Theme.S(Theme.RadiusCard),
            ImDrawFlags.RoundCornersTop);

        // Dégradé sombre en pied de bannière : le titre reste lisible quelle
        // que soit la photo.
        var fadeTop = new Vector2(origin.X, end.Y - height * 0.5f);
        var clear   = ImGui.GetColorU32(Theme.Alpha(Theme.BgBase, 0f));
        var opaque  = ImGui.GetColorU32(Theme.Alpha(Theme.BgBase, 0.85f));
        dl.AddRectFilledMultiColor(fadeTop, end, clear, clear, opaque, opaque);
    }
}

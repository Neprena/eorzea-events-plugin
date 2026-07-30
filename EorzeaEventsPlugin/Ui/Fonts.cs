using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using System.Numerics;
using System.Reflection;
using System.Text.Unicode;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Polices du plugin : Inter, embarqué et sous-ensemblé, avec FontAwesome
/// fusionné dans le corps de texte pour que les icônes s'écrivent inline sans
/// pousser ni dépiler de police.
///
/// L'atlas est isolé (<c>CreateFontAtlas</c>) : une reconstruction ne touche pas
/// celui de Dalamud ni celui des autres plugins.
///
/// Toute la classe est tolérante à l'échec : si un TTF est corrompu ou si
/// l'atlas n'est pas encore construit, les <c>Push*</c> ne font rien et
/// l'interface reste lisible avec la police par défaut de Dalamud.
/// </summary>
internal static class Fonts
{
    private static readonly Assembly Asm = typeof(Fonts).Assembly;

    private static IFontAtlas? _atlas;
    private static ushort[]?   _textRanges;
    private static ushort[]?   _iconRanges;
    private static bool        _loggedFailure;

    /// <summary>Titres de page et de carte.</summary>
    public static IFontHandle? Title { get; private set; }

    /// <summary>En-têtes de section.</summary>
    public static IFontHandle? H2 { get; private set; }

    /// <summary>Corps de texte, FontAwesome fusionné.</summary>
    public static IFontHandle? Body { get; private set; }

    /// <summary>Métadonnées, chips, barre de statut.</summary>
    public static IFontHandle? Small { get; private set; }

    /// <summary>Corps de texte en graisse forte, pour le gras du Markdown.</summary>
    public static IFontHandle? BodyStrong { get; private set; }

    public static bool Ready => Body is { Available: true };

    // ─── Construction ─────────────────────────────────────────────────────────

    public static void Build(IDalamudPluginInterface pi)
    {
        try
        {
            // Ces plages doivent couvrir tout ce que le sous-ensemble Inter
            // contient : un glyphe présent dans le fichier mais absent d'ici
            // n'est pas chargé dans l'atlas et s'affiche en caractère de
            // remplacement. C'est ce qui arrivait au symbole « marque déposée ».
            _textRanges = new FluentGlyphRangeBuilder()
                .With(UnicodeRanges.BasicLatin)
                .With(UnicodeRanges.Latin1Supplement)
                .With(UnicodeRanges.LatinExtendedA)
                .With(UnicodeRanges.LatinExtendedB)
                .With(UnicodeRanges.CombiningDiacriticalMarks)
                .With(UnicodeRanges.GeneralPunctuation)
                .With(UnicodeRanges.CurrencySymbols)
                .With(UnicodeRanges.LetterlikeSymbols)
                .With(UnicodeRanges.NumberForms)
                .With(UnicodeRanges.Arrows)
                .With(UnicodeRanges.MathematicalOperators)
                .With(UnicodeRanges.MiscellaneousTechnical)
                .With(UnicodeRanges.EnclosedAlphanumerics)
                .With(UnicodeRanges.BoxDrawing)
                .With(UnicodeRanges.GeometricShapes)
                .With(UnicodeRanges.MiscellaneousSymbols)
                .With(UnicodeRanges.Dingbats)
                .With(UnicodeRanges.AlphabeticPresentationForms)
                .Build();


            _iconRanges = BuildIconRanges();

            _atlas = pi.UiBuilder.CreateFontAtlas(
                FontAtlasAutoRebuildMode.Async, isGlobalScaled: true, "EorzeaEvents");

            // FontAwesome est fusionné dans les quatre niveaux : les pastilles
            // utilisent Small et les en-têtes utilisent H2, ils ont donc besoin
            // des icônes autant que le corps de texte.
            Body  = _atlas.NewDelegateFontHandle(tk => tk.OnPreBuild(
                p => Compose(p, "Fonts.Inter-Regular.ttf",  15f)));
            Small = _atlas.NewDelegateFontHandle(tk => tk.OnPreBuild(
                p => Compose(p, "Fonts.Inter-Regular.ttf",  12f)));
            H2    = _atlas.NewDelegateFontHandle(tk => tk.OnPreBuild(
                p => Compose(p, "Fonts.Inter-SemiBold.ttf", 18f)));
            Title = _atlas.NewDelegateFontHandle(tk => tk.OnPreBuild(
                p => Compose(p, "Fonts.Inter-SemiBold.ttf", 24f)));
            BodyStrong = _atlas.NewDelegateFontHandle(tk => tk.OnPreBuild(
                p => Compose(p, "Fonts.Inter-SemiBold.ttf", 15f)));
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Chargement des polices impossible, repli sur la police Dalamud.");
            Title = H2 = Body = Small = BodyStrong = null;
            _atlas?.Dispose();
            _atlas = null;
        }
    }

    private static void Compose(IFontAtlasBuildToolkitPreBuild p, string resource, float sizePx)
    {
        var cfg = new SafeFontConfig { SizePx = sizePx, GlyphRanges = _textRanges };

        using var stream = Asm.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Ressource de police introuvable : {resource}");

        var font = p.AddFontFromStream(stream, in cfg, leaveOpen: false, resource);

        // Accents et alphabets supplémentaires selon la langue configurée dans Dalamud.
        var extra = new SafeFontConfig { SizePx = sizePx, MergeFont = font };
        p.AttachExtraGlyphsForDalamudLanguage(in extra);


        var icons = new SafeFontConfig
        {
            SizePx      = sizePx * 0.86f,
            MergeFont   = font,
            GlyphRanges = _iconRanges,
            GlyphOffset = new Vector2(0f, Theme.S(1f)),
        };
        p.AddFontAwesomeIconFont(in icons);

        p.SetFontScaleMode(font, FontScaleMode.Default);
    }

    private static ushort[] BuildIconRanges()
    {
        var builder = new FluentGlyphRangeBuilder();
        foreach (var icon in Icons.All)
            builder = builder.With((uint)icon);
        return builder.BuildExact();
    }

    // ─── Utilisation ──────────────────────────────────────────────────────────

    public static IDisposable PushTitle() => Use(Title);
    public static IDisposable PushH2()    => Use(H2);
    public static IDisposable PushBody()  => Use(Body);
    public static IDisposable PushSmall()      => Use(Small);
    public static IDisposable PushBodyStrong() => Use(BodyStrong);

    private static IDisposable Use(IFontHandle? handle)
    {
        if (handle is not { Available: true })
        {
            LogFailureOnce(handle);
            return NullScope.Instance;
        }

        return handle.Push();
    }

    private static void LogFailureOnce(IFontHandle? handle)
    {
        if (_loggedFailure || handle?.LoadException is not { } ex) return;
        _loggedFailure = true;
        Plugin.Log.Warning(ex, "Une police du plugin n'a pas pu être construite.");
    }

    public static void Dispose()
    {
        Title = H2 = Body = Small = BodyStrong = null;
        _atlas?.Dispose();
        _atlas = null;
    }

    /// <summary>Portée vide, retournée quand la police n'est pas disponible.</summary>
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

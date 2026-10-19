using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using EorzeaEventsPlugin.Api;
using System.Numerics;
using System.Text.RegularExpressions;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Morceaux communs à la carte d'une actualité et à sa fenêtre de lecture :
/// pastilles d'en-tête, corps Markdown avec ses images, adresse des images.
/// </summary>
internal static partial class NewsView
{
    private const float MaxImageHeight = 320f;

    /// <summary>
    /// Épinglée, catégorie et date de publication, sur une ligne.
    ///
    /// Les textes sont centrés sur la hauteur des pastilles. AlignTextToFramePadding
    /// les centrait sur celle d'un bouton, plus haute : la date tombait sous
    /// la ligne, et un texte posé tel quel restait collé en haut.
    /// </summary>
    /// <param name="showPinned">Faux là où l'épinglage est déjà dit autrement.</param>
    /// <param name="kicker">Libellé doré en tête de ligne, celui du bandeau.</param>
    public static void Header(NewsItemDto item, Loc l, bool showPinned = true, string? kicker = null)
    {
        var top   = ImGui.GetCursorPosY();
        var chipH = Chip.Height();
        var first = true;

        void Next(float gap)
        {
            if (!first) ImGui.SameLine(0f, Theme.S(gap));
            first = false;
        }

        void SmallText(Action draw)
        {
            using var _ = Fonts.PushSmall();
            ImGui.SetCursorPosY(top + (chipH - ImGui.GetTextLineHeight()) * 0.5f);
            draw();
        }

        if (kicker != null)
        {
            Next(0f);
            SmallText(() => Text.WithIcon(Icons.Pin, kicker, Theme.Gold, Theme.Gold));
        }

        if (item.IsPinned && showPinned)
        {
            Next(Theme.GapXs);
            ImGui.SetCursorPosY(top);
            Chip.Draw(l.NewsPinned, ChipTone.Gold, Icons.Pin);
        }

        Next(kicker != null ? Theme.GapM : Theme.GapXs);
        ImGui.SetCursorPosY(top);
        Chip.Draw(CategoryLabel(item.Category, l), CategoryTone(item.Category), CategoryIcon(item.Category));

        if (item.PublishedAt is { } at)
        {
            Next(Theme.GapS);
            SmallText(() => ImGui.TextColored(Theme.TextFaint, at.ToLocalTime().ToString("d MMMM yyyy", l.Culture)));
        }

        // La ligne suivante part sous les pastilles, quel que soit le dernier
        // élément posé. Le Dummy valide la position : ImGui refuse qu'un
        // SetCursorPos étende la fenêtre sans élément derrière.
        ImGui.SetCursorPosY(top + chipH);
        ImGui.Dummy(Vector2.Zero);
    }

    /// <summary>
    /// Corps de l'actualité, images comprises.
    ///
    /// <see cref="MarkdownView"/> ne sait pas dessiner d'image, et leur syntaxe
    /// apparaîtrait telle quelle : le texte est donc découpé autour de chaque
    /// image, chaque morceau rendu en Markdown et chaque image dessinée entre
    /// deux, à la largeur disponible.
    /// </summary>
    public static void Body(string markdown)
    {
        var last = 0;
        foreach (Match m in ImageSyntax().Matches(markdown))
        {
            MarkdownView.Draw(markdown[last..m.Index]);
            DrawImage(m.Groups[1].Value);
            last = m.Index + m.Length;
        }
        MarkdownView.Draw(markdown[last..]);
    }

    /// <summary>
    /// Image entière, jamais recadrée : réduite à la largeur disponible et
    /// plafonnée en hauteur. Tant qu'elle n'est pas arrivée, rien n'est réservé :
    /// une image qui ne se charge pas ne laisse pas de trou.
    /// </summary>
    private static void DrawImage(string url)
    {
        if (Textures.Get(AbsoluteUrl(url)) is not { } wrap || wrap.Width == 0 || wrap.Height == 0) return;

        var availW = ImGui.GetContentRegionAvail().X;
        var scale  = Math.Min(availW / wrap.Width, Theme.S(MaxImageHeight) / wrap.Height);
        scale      = Math.Min(scale, Theme.S(1f));
        var size   = new Vector2(wrap.Width * scale, wrap.Height * scale);

        Layout.Spacer(Theme.GapXs);
        Layout.Center(size.X);
        ImGui.Image(wrap.Handle, size);
        Layout.Spacer(Theme.GapS);
    }

    /// <summary>Les images du site peuvent être servies en chemin relatif.</summary>
    public static string? AbsoluteUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null
      : url.StartsWith('/') ? Plugin.Config.BaseUrl.TrimEnd('/') + url
      : url;

    /// <summary>Ouvre l'actualité sur le site.</summary>
    public static void OpenOnSite(string path)
    {
        try { Dalamud.Utility.Util.OpenLink(Plugin.Config.BaseUrl.TrimEnd('/') + path); }
        catch { /* navigateur indisponible */ }
    }

    // ─── Catégories ───────────────────────────────────────────────────────────

    private static string CategoryLabel(string category, Loc l) => category switch
    {
        "MAINTENANCE" => l.NewsCatMaintenance,
        "UPDATE"      => l.NewsCatUpdate,
        "RECRUITMENT" => l.NewsCatRecruitment,
        _             => l.NewsCatAnnouncement,
    };

    private static ChipTone CategoryTone(string category) => category switch
    {
        "MAINTENANCE" => ChipTone.Danger,
        "UPDATE"      => ChipTone.Success,
        _             => ChipTone.Accent,
    };

    private static FontAwesomeIcon CategoryIcon(string category) => category switch
    {
        "MAINTENANCE" => FontAwesomeIcon.Wrench,
        "UPDATE"      => Icons.Sparkle,
        "RECRUITMENT" => Icons.FriendAdd,
        _             => Icons.News,
    };

    /// <summary>Image Markdown, l'URL en premier groupe, titre éventuel ignoré.</summary>
    [GeneratedRegex(@"!\[[^\]]*\]\(\s*<?([^)\s>]+)>?(?:\s+""[^""]*"")?\s*\)")]
    private static partial Regex ImageSyntax();
}

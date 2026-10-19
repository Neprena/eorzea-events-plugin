using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Actualités du site, et bandeaux posés en tête de la fenêtre : l'alerte de
/// l'équipe en cours, puis l'actualité épinglée.
///
/// La liste n'est relevée que fenêtre ouverte : les bandeaux et la pastille ne
/// se voient que là, et une actualité n'a rien d'urgent. L'urgence passe par
/// l'alerte, servie avec le relevé agrégé du plugin.
/// </summary>
internal sealed class NewsPage
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

    private List<NewsItemDto> _items = [];
    private bool     _loading;
    private bool     _failed;
    private bool     _loaded;
    private DateTime _lastFetch = DateTime.MinValue;

    /// <summary>Langue du dernier relevé : le serveur traduit, changer de langue impose de relire.</summary>
    private string? _lang;

    /// <summary>Actualités publiées depuis la dernière visite de la page.</summary>
    public int UnreadCount
    {
        get
        {
            var seen = Plugin.Config.NewsSeenUpTo;
            return seen == null ? 0 : _items.Count(n => n.PublishedAt > seen);
        }
    }

    /// <summary>Actualité épinglée, s'il y en a une. Le serveur la place en tête.</summary>
    private NewsItemDto? Pinned => _items.FirstOrDefault(n => n.IsPinned);

    /// <summary>Relève la liste si elle date. À appeler à chaque image, fenêtre ouverte.</summary>
    public void Tick()
    {
        if (_loading) return;
        if (_lang == Plugin.L.Code && DateTime.UtcNow - _lastFetch < RefreshInterval) return;
        _ = FetchAsync();
    }

    private async Task FetchAsync()
    {
        _loading   = true;
        _lastFetch = DateTime.UtcNow;
        _lang      = Plugin.L.Code;
        try
        {
            var items = await Plugin.Api.GetNewsAsync(_lang);

            // Un échec garde ce qui est affiché : vider la page ferait passer
            // une panne pour « aucune actualité ».
            _failed = items == null;
            if (items == null) return;

            _items  = items;
            _loaded = true;

            // Premier relevé de l'installation : tout ce qui existe déjà est
            // tenu pour vu, seules les suivantes feront une pastille.
            if (Plugin.Config.NewsSeenUpTo == null) MarkAllSeen();
        }
        finally { _loading = false; }
    }

    private void MarkAllSeen()
    {
        var newest = _items.Max(n => n.PublishedAt);
        if (newest == null || newest <= Plugin.Config.NewsSeenUpTo) return;

        Plugin.Config.NewsSeenUpTo = newest;
        Plugin.Config.Save();
    }

    // ─── Bandeaux ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bandeaux en tête de toutes les pages : l'alerte de l'équipe, puis
    /// l'actualité épinglée. Chacun se ferme, et revient si son contenu change.
    /// </summary>
    /// <param name="onNewsPage">La page Actualités montre déjà l'épinglée en tête.</param>
    public void DrawBanners(bool onNewsPage)
    {
        var l = Plugin.L;

        if (Plugin.CurrentAlert is { } alert && alert.Id != Plugin.Config.DismissedAlertId)
        {
            DrawTeamAlert(alert, l);
            Layout.Spacer(Theme.GapS);
        }

        if (onNewsPage || Pinned is not { } pinned || pinned.Id == Plugin.Config.DismissedPinnedNewsId) return;

        DrawPinnedStrip(pinned, l);
        Layout.Spacer(Theme.GapS);
    }

    /// <summary>
    /// Message de l'équipe : un en-tête discret, coloré selon le niveau, avec la
    /// croix de fermeture, puis le message mis en forme.
    ///
    /// Feedback.Alert faisait du libellé « Message de l'équipe » le titre et du
    /// message un simple texte gris, sous lequel un bouton « Fermer » isolé
    /// prenait une ligne entière : le message, seule chose à lire, était ce qui
    /// se voyait le moins.
    /// </summary>
    private static void DrawTeamAlert(PluginAlertDto alert, Loc l)
    {
        var (color, icon) = alert.Level switch
        {
            "critical" => (Theme.Danger, Icons.Warning),
            "info"     => (Theme.Accent, Icons.Info),
            _          => (Theme.Idle,   Icons.Warning),
        };

        using var card = Card.Begin("##teamalert", CardTone.Flat, interactive: false,
                                    background: Theme.Mix(Theme.BgSurface, color, 0.14f),
                                    border: Theme.Alpha(color, 0.45f),
                                    accent: color);

        // En-tête : libellé centré sur la hauteur de la croix, qui se cale à droite.
        var top   = ImGui.GetCursorPosY();
        var frame = ImGui.GetFrameHeight();

        using (Fonts.PushSmall())
        {
            ImGui.SetCursorPosY(top + (frame - ImGui.GetTextLineHeight()) * 0.5f);
            Text.WithIcon(icon, l.AlertTitle.ToUpper(l.Culture), color, color);
        }

        ImGui.SameLine();
        Layout.RightAlign(frame);
        ImGui.SetCursorPosY(top);
        if (Btn.Icon(Icons.Close, "dismissalert", BtnTone.Ghost, l.BannerDismiss))
        {
            Plugin.Config.DismissedAlertId = alert.Id;
            Plugin.Config.Save();
        }

        MarkdownView.Draw(alert.MessageFor(l.Code), Theme.Text);
    }

    /// <summary>
    /// Actualité épinglée, sur une seule ligne : libellé, titre, « Lire » et
    /// fermeture. En carte, avec son accroche et ses boutons, elle occupait le
    /// haut de chaque page ; l'accroche se lit dans la fenêtre de lecture.
    /// Toute la ligne ouvre l'actualité.
    /// </summary>
    private static void DrawPinnedStrip(NewsItemDto pinned, Loc l)
    {
        var origin = ImGui.GetCursorScreenPos();
        var width  = ImGui.GetContentRegionAvail().X - Card.RightInset;
        var frame  = ImGui.GetFrameHeight();
        // Assez d'air autour des boutons pour qu'ils ne touchent pas le cadre :
        // à 3 px, « Lire » semblait posé en travers du bord.
        var padY   = Theme.S(6f);
        var height = frame + padY * 2f;
        var padX   = Theme.S(Theme.GapL);
        var gap    = Theme.S(Theme.GapS);
        var dl     = ImGui.GetWindowDrawList();
        var radius = Theme.S(Theme.RadiusFrame);

        // Boutons à droite, mesurés d'abord : le titre prend ce qui reste.
        var readW  = Btn.Measure(l.NewsRead, BtnSize.Small, Icons.News);
        var closeX = origin.X + width - padX - frame;
        var readX  = closeX - gap - readW;
        var y      = origin.Y + padY;

        // Zone cliquable : la partie texte seulement, les boutons gardent la leur.
        ImGui.SetCursorScreenPos(origin);
        var open    = ImGui.InvisibleButton("##pinnedstrip", new Vector2(Math.Max(1f, readX - gap - origin.X), height));
        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var tint = Theme.Mix(Theme.BgSurface, Theme.Gold, hovered ? 0.20f : 0.12f);
        dl.AddRectFilled(origin, origin + new Vector2(width, height), ImGui.GetColorU32(tint), radius);
        dl.AddRect(origin, origin + new Vector2(width, height),
                   ImGui.GetColorU32(Theme.Alpha(Theme.Gold, 0.45f)), radius);
        dl.AddRectFilled(origin, origin + new Vector2(Theme.S(3f), height),
                         ImGui.GetColorU32(Theme.Gold), radius, ImDrawFlags.RoundCornersLeft);

        // Libellé doré, en petit, centré sur la hauteur de la ligne.
        float x = origin.X + padX;
        using (Fonts.PushSmall())
        {
            var kicker = $"{Icons.Pin.S()}  {l.NewsPinnedKicker.ToUpper(l.Culture)}";
            var ky     = origin.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;
            dl.AddText(new Vector2(x, ky), ImGui.GetColorU32(Theme.Gold), kicker);
            x += ImGui.CalcTextSize(kicker).X + Theme.S(Theme.GapM);
        }

        using (Fonts.PushBodyStrong())
        {
            var room  = readX - gap - x;
            var title = Ellipsize(Glyphs.Safe(pinned.Title), room);
            var ty    = origin.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;
            dl.AddText(new Vector2(x, ty), ImGui.GetColorU32(Theme.Text), title);
        }

        ImGui.SetCursorScreenPos(new Vector2(readX, y));
        if (Btn.Draw(l.NewsRead, BtnTone.Primary, BtnSize.Small, Icons.News, id: "readpinned")) open = true;

        ImGui.SetCursorScreenPos(new Vector2(closeX, y));
        if (Btn.Icon(Icons.Close, "dismisspinned", BtnTone.Ghost, l.BannerDismiss))
        {
            Plugin.Config.DismissedPinnedNewsId = pinned.Id;
            Plugin.Config.Save();
            open = false;
        }

        if (open) Plugin.OpenNewsDetail(pinned);

        // Le curseur repart sous la ligne ; le Dummy valide la position.
        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
        ImGui.Dummy(Vector2.Zero);
    }

    /// <summary>Coupe le texte avec « … » pour qu'il tienne dans la largeur donnée.</summary>
    private static string Ellipsize(string text, float limit)
    {
        if (limit <= 0f) return string.Empty;
        if (ImGui.CalcTextSize(text).X <= limit) return text;

        const string dots = "…";
        var end = text.Length;
        while (end > 0 && ImGui.CalcTextSize(text[..end].TrimEnd() + dots).X > limit) end--;
        return end == 0 ? string.Empty : text[..end].TrimEnd() + dots;
    }

    // ─── Page ─────────────────────────────────────────────────────────────────

    public void Draw()
    {
        var l = Plugin.L;

        // La page ouverte, ce qu'elle liste est vu : la pastille s'éteint.
        if (UnreadCount > 0) MarkAllSeen();

        Layout.Spacer(Theme.GapXs);

        if (!_loaded)
        {
            if (_failed) Feedback.EmptyState(Icons.Warning, l.LoadFailed, null, l.Refresh, () => _ = FetchAsync());
            else         Feedback.SkeletonCards();
            return;
        }

        if (_failed) Feedback.Alert(Theme.Danger, Icons.Warning, l.LoadFailed);

        if (_items.Count == 0)
        {
            Feedback.EmptyState(Icons.News, l.NewsEmpty);
            return;
        }

        Layout.SectionHeader(l.TabNews, Icons.News);

        using var scroll = ImRaii.Child("##newsscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        foreach (var item in _items) DrawItem(item, l);

        Layout.Spacer(Theme.GapS);
        if (Text.Link(l.NewsAllOnSite)) NewsView.OpenOnSite("/actualites");
        Layout.Spacer(Theme.GapXl);
    }

    private static void DrawItem(NewsItemDto item, Loc l)
    {
        // Même bandeau que les cartes d'événement : la couverture reconnaît
        // l'actualité d'un coup d'œil, comme sur la page du site.
        using var card = Card.Begin($"news_{item.Id}", interactive: false,
                                    accent: item.IsPinned ? Theme.Gold : null,
                                    banner: Textures.Get(NewsView.AbsoluteUrl(item.ImageUrl)),
                                    bannerHeight: 140f);

        NewsView.Header(item, l);

        Layout.Spacer(Theme.GapXs);
        Text.CardTitle(item.Title, Theme.Accent);
        Layout.Spacer(Theme.GapXs);

        // L'accroche seulement : le texte entier se lit dans sa fenêtre.
        Text.Wrapped(item.Excerpt, Theme.TextMuted);

        Layout.Spacer(Theme.GapS);

        if (Btn.Draw(l.NewsRead, BtnTone.Primary, BtnSize.Medium, Icons.News, id: $"newsread_{item.Id}"))
            Plugin.OpenNewsDetail(item);
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.ViewOnline, BtnTone.Secondary, BtnSize.Medium, Icons.External, id: $"newssite_{item.Id}"))
            NewsView.OpenOnSite(item.Path);
    }
}

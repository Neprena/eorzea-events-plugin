using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Pages;

/// <summary>
/// Concours du site, et bandeau du concours mis en avant en tête de fenêtre.
/// L'inscription se fait sur le site : la page montre, puis y renvoie.
///
/// Même rythme que les actualités : relevé toutes les trois minutes fenêtre
/// ouverte, et aussitôt qu'on ouvre la fenêtre ou la page si la liste a plus de
/// trente secondes. La liste se relit aussi dès que le compte est lié ou
/// délié : l'état « Tu participes » en dépend, et l'attendre trois minutes
/// ferait croire que la liaison n'a rien changé.
/// </summary>
internal sealed class ContestsPage
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(3);

    /// <summary>Âge au-delà duquel ouvrir la fenêtre ou la page relit la liste.</summary>
    private static readonly TimeSpan StaleOnOpen = TimeSpan.FromSeconds(30);

    /// <summary>Rythme de relevé pendant qu'on attend une inscription faite sur le site.</summary>
    private static readonly TimeSpan WatchInterval = TimeSpan.FromSeconds(30);

    /// <summary>Durée de cette attente après un clic sur « Participer sur le site ».</summary>
    private static readonly TimeSpan WatchDuration = TimeSpan.FromMinutes(5);

    /// <summary>Fin de l'attente d'une inscription ; d'ici là, relevé toutes les trente secondes.</summary>
    private DateTime _watchUntil = DateTime.MinValue;

    public ContestsPage() => ContestView.EnterClicked += () => _watchUntil = DateTime.UtcNow + WatchDuration;

    /// <summary>
    /// Version la plus récente d'un concours, pour la fenêtre de détail : elle
    /// suit ainsi l'inscription, le tirage et la langue sans être rouverte.
    /// </summary>
    public ContestItemDto? Find(string id) => _items.FirstOrDefault(c => c.Id == id);

    private List<ContestItemDto> _items = [];
    private bool     _loading;
    private bool     _failed;
    private bool     _loaded;
    private DateTime _lastFetch = DateTime.MinValue;

    /// <summary>Langue du dernier relevé : le serveur traduit, changer de langue impose de relire.</summary>
    private string? _lang;

    /// <summary>Présence d'un jeton au dernier relevé : l'état du compte en dépend.</summary>
    private bool _hadToken;

    /// <summary>Image du dernier rendu de la page, pour reconnaître l'arrivée sur celle-ci.</summary>
    private int _lastDrawFrame = -1;

    /// <summary>Concours ouverts depuis la dernière visite, hors ceux où l'on participe déjà.</summary>
    public int UnreadCount
    {
        get
        {
            var seen = Plugin.Config.ContestsSeenUpTo;
            return seen == null ? 0 : _items.Count(c => c.Phase == "open" && c.StartAt > seen && c.Me?.Entered != true);
        }
    }

    /// <summary>Relève la liste si elle date. À appeler à chaque image, fenêtre ouverte.</summary>
    public void Tick()
    {
        if (_loading) return;
        var now      = DateTime.UtcNow;
        var interval = now < _watchUntil ? WatchInterval : RefreshInterval;
        var stale    = now - _lastFetch >= interval;
        if (!stale && _lang == Plugin.L.Code && _hadToken == Plugin.Api.HasToken) return;
        _ = FetchAsync();
    }

    /// <summary>La fenêtre principale vient de s'ouvrir : relire une liste qui date un peu.</summary>
    public void NotifyWindowOpened() => RefreshIfOlderThan(StaleOnOpen);

    /// <summary>Le site revient : une liste que la panne a fait échouer se relit aussitôt.</summary>
    public void OnServerBack()
    {
        if (_failed && !_loading) _ = FetchAsync();
    }

    private void RefreshIfOlderThan(TimeSpan age)
    {
        if (_loading || DateTime.UtcNow - _lastFetch < age) return;
        _ = FetchAsync();
    }

    private async Task FetchAsync()
    {
        _loading   = true;
        _lastFetch = DateTime.UtcNow;
        _lang      = Plugin.L.Code;
        _hadToken  = Plugin.Api.HasToken;
        try
        {
            var items = await Plugin.Api.GetContestsAsync(_lang);

            // Un échec garde ce qui est affiché : vider la page ferait passer
            // une panne pour « aucun concours ».
            _failed = items == null;
            if (items == null) return;

            _items  = items;
            _loaded = true;

            // Premier relevé de l'installation : les concours déjà ouverts sont
            // tenus pour vus, seuls les suivants feront une pastille.
            if (Plugin.Config.ContestsSeenUpTo == null) MarkAllSeen(force: true);
        }
        finally { _loading = false; }
    }

    private void MarkAllSeen(bool force = false)
    {
        var open   = _items.Where(c => c.Phase == "open").ToList();
        // Aucun concours ouvert au premier relevé : la date du relevé sert de
        // repère, pour que le prochain concours ouvert fasse une pastille.
        DateTime? newest = open.Count > 0 ? open.Max(c => c.StartAt) : (force ? DateTime.UtcNow : null);
        if (newest == null || newest <= Plugin.Config.ContestsSeenUpTo) return;

        Plugin.Config.ContestsSeenUpTo = newest;
        Plugin.Config.Save();
    }

    // ─── Bandeau ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Bandeau du concours mis en avant, en tête de toutes les pages sauf
    /// celle-ci. Absent si l'on participe déjà ; fermé, il revient pour un autre
    /// concours.
    /// </summary>
    public void DrawBanner(bool onContestsPage)
    {
        if (onContestsPage) return;

        var featured = _items.FirstOrDefault(c => c.Phase == "open" && c.IsFeatured
                                                  && c.Me?.Entered != true
                                                  && c.Id != Plugin.Config.DismissedContestId);
        if (featured == null) return;

        var l    = Plugin.L;
        var open = BannerStrip.Draw("contest", Icons.Gift, l.ContestBannerKicker, Theme.Gold,
                                    featured.Title, l.ContestDetails, Icons.Gift, out var dismissed);
        if (dismissed)
        {
            Plugin.Config.DismissedContestId = featured.Id;
            Plugin.Config.Save();
        }
        if (open) Plugin.OpenContestDetail(featured);

        Layout.Spacer(Theme.GapS);
    }

    // ─── Page ─────────────────────────────────────────────────────────────────

    public void Draw()
    {
        var l = Plugin.L;

        // Arrivée sur la page (elle n'était pas dessinée à l'image précédente) :
        // on relit la liste si elle a un peu vécu.
        var frame = ImGui.GetFrameCount();
        if (_lastDrawFrame != frame - 1) RefreshIfOlderThan(StaleOnOpen);
        _lastDrawFrame = frame;

        // La page ouverte, ce qu'elle liste est vu : la pastille s'éteint.
        if (UnreadCount > 0) MarkAllSeen();

        Layout.Spacer(Theme.GapXs);

        if (!_loaded)
        {
            if (_failed) Feedback.EmptyState(Icons.Warning, l.LoadFailed, null, l.Refresh, () => _ = FetchAsync());
            else         Feedback.SkeletonCards();
            return;
        }

        // Le bandeau du site dit déjà la panne, en tête de fenêtre : le répéter
        // ici ferait deux alertes pour une seule cause.
        if (_failed && !Plugin.Api.IsServerDown) Feedback.Alert(Theme.Danger, Icons.Warning, l.LoadFailed);

        // Seuls comptent les concours où l'on peut encore s'inscrire : ouverts
        // d'abord, puis à venir, chacun dans l'ordre servi (mis en avant en tête).
        // Les concours terminés restent sur le site.
        var current = _items.Where(c => c.Phase == "open")
                            .Concat(_items.Where(c => c.Phase == "upcoming")).ToList();

        if (current.Count == 0)
        {
            Feedback.EmptyState(Icons.Gift, l.ContestsEmpty, null, l.ContestsAllOnSite,
                                () => NewsView.OpenOnSite("/concours"));
            return;
        }

        Layout.SectionHeader(l.TabContests, Icons.Gift,
                             actions: () =>
                             {
                                 if (Btn.Draw(_loading ? l.Loading : l.Refresh, BtnTone.Secondary, BtnSize.Medium,
                                              Icons.Refresh, disabled: _loading, id: "contests_refresh"))
                                     _ = FetchAsync();
                             },
                             actionsWidth: Math.Max(Btn.Measure(l.Refresh, Icons.Refresh),
                                                    Btn.Measure(l.Loading, Icons.Refresh)));

        using var scroll = ImRaii.Child("##contestsscroll", new Vector2(-1f, -1f));
        if (!scroll) return;

        foreach (var c in current) DrawItem(c, l);

        Layout.Spacer(Theme.GapS);
        if (Text.Link(l.ContestsAllOnSite)) NewsView.OpenOnSite("/concours");
        Layout.Spacer(Theme.GapXl);
    }

    private static void DrawItem(ContestItemDto c, Loc l)
    {
        using var card = Card.Begin($"contest_{c.Id}", interactive: false,
                                    accent: c.IsFeatured ? Theme.Gold : null,
                                    banner: Textures.Get(NewsView.AbsoluteUrl(c.ImageUrl)),
                                    bannerHeight: 140f);

        ContestView.Chips(c, l);
        Layout.Spacer(Theme.GapXs);
        Text.CardTitle(c.Title, Theme.Accent);
        ContestView.Dates(c, l);

        Layout.Spacer(Theme.GapS);
        ContestView.Prizes(c, l);
        using (Fonts.PushSmall())
            Text.Muted(string.Format(l.ContestEntries, c.EntryCount));

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw(l.ContestDetails, BtnTone.Secondary, BtnSize.Medium, Icons.Info, id: $"contestdetail_{c.Id}"))
            Plugin.OpenContestDetail(c);
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ContestView.SiteButton(c, l, $"contestsite_{c.Id}");
    }
}

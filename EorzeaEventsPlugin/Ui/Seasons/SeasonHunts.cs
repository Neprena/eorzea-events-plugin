using Dalamud.Interface;
using EorzeaEventsPlugin.Ui.Components;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Une chasse de saison : un objet caché sur chacune des pages listées, à
/// ramasser d'un clic. La progression repart de zéro chaque année.
/// </summary>
internal sealed record Hunt(Season Season, string Prefix, string[] Pages)
{
    public string Key => $"{Prefix}-{SeasonState.Now.Year}";
}

/// <summary>
/// Les surprises qui retiennent quelque chose d'une session à l'autre :
/// chasses aux citrouilles et aux œufs, cases du calendrier de l'Avent,
/// récapitulatif de l'année. Tout tient dans <see cref="Configuration.SeasonalFinds"/>.
/// </summary>
internal static class SeasonHunts
{
    /// <summary>Sept pages toujours ouvertes, compte lié ou non.</summary>
    public static readonly Hunt Pumpkins = new(Season.Halloween, "halloween",
        ["rp", "around", "encounters", "events", "venues", "news", "settings"]);

    /// <summary>Un œuf par page du menu, debug exclu.</summary>
    public static readonly Hunt Eggs = new(Season.Easter, "easter",
        ["rp", "around", "encounters", "profile", "friends", "events", "venues", "news", "settings"]);

    private static readonly string[] EggArt = ["egg-mint", "egg-yellow", "egg-pink", "egg-lavender"];

    /// <summary>La chasse ouverte en ce moment, ou null.</summary>
    public static Hunt? Active => !SeasonState.EasterEggs ? null : SeasonState.Current switch
    {
        Season.Halloween => Pumpkins,
        Season.Easter    => Eggs,
        _                => null,
    };

    public static bool Has(string key, string item) =>
        Plugin.Config.SeasonalFinds.TryGetValue(key, out var set) && set.Contains(item);

    public static int Count(string key) =>
        Plugin.Config.SeasonalFinds.TryGetValue(key, out var set) ? set.Count : 0;

    /// <summary>Retient l'élément ; faux s'il l'était déjà.</summary>
    public static bool Add(string key, string item)
    {
        var finds = Plugin.Config.SeasonalFinds;
        if (!finds.TryGetValue(key, out var set)) finds[key] = set = [];
        if (!set.Add(item)) return false;

        Plugin.Config.Save();
        return true;
    }

    public static string ArtFor(Hunt hunt, string page) =>
        hunt == Pumpkins ? "pumpkin" : EggArt[Math.Max(0, Array.IndexOf(hunt.Pages, page)) % EggArt.Length];

    /// <summary>
    /// Abscisse de la cachette, en fraction de la largeur : stable pour une page
    /// et une année, différente d'une page à l'autre. FNV-1a, comme
    /// <see cref="Theme.FromName"/>.
    /// </summary>
    public static float Spot(Hunt hunt, string page)
    {
        var hash = 2166136261u;
        foreach (var ch in hunt.Key + page)
        {
            hash ^= ch;
            hash *= 16777619u;
        }

        return 0.12f + hash % 1000u / 1000f * 0.76f;
    }

    /// <summary>Ramasse l'objet de la page et l'annonce, avec le total quand la chasse est finie.</summary>
    public static void Collect(Hunt hunt, string page)
    {
        if (!Add(hunt.Key, page)) return;

        var l     = Plugin.L;
        var found = Count(hunt.Key);
        var total = hunt.Pages.Length;
        var done  = found >= total;

        var message = hunt == Pumpkins
            ? done ? string.Format(l.HuntPumpkinsDone, total) : string.Format(l.HuntPumpkinFound, found, total)
            : done ? string.Format(l.HuntEggsDone, total) : string.Format(l.HuntEggFound, found, total);

        Snackbar.Show(message);
    }

    // ─── Calendriers ──────────────────────────────────────────────────────────

    /// <summary>Le calendrier de l'Avent : une case par jour, du 1er au 24 décembre.</summary>
    public static readonly SeasonDaily Advent = new(Season.Christmas, "advent", 12, 1, 24, "gift", Icons.Gift,
        l => l.AdventTitle, l => l.AdventDay, l => l.AdventHint, l => l.AdventChallenges);

    /// <summary>Les billets doux de la Valentione : un par jour, du 7 au 14 février.</summary>
    public static readonly SeasonDaily LoveNotes = new(Season.Valentine, "valentione", 2, 7, 14, "heart", Icons.Heart,
        l => l.LoveTitle, l => l.LoveDay, l => l.LoveHint, l => l.LovePrompts);

    /// <summary>Le calendrier dont c'est la période, surprises actives, ou null.</summary>
    public static SeasonDaily? ActiveDaily
    {
        get
        {
            if (!SeasonState.EasterEggs) return null;

            var now = SeasonState.Now;
            if (Covers(Advent, now)) return Advent;
            if (Covers(LoveNotes, now)) return LoveNotes;
            return null;
        }
    }

    /// <summary>
    /// Sur la date seule, sans regarder le thème : l'Avent couvre tout décembre
    /// alors que la Starlight de Square Enix n'en prend que la seconde moitié.
    /// </summary>
    private static bool Covers(SeasonDaily daily, DateTime now) =>
        now.Month == daily.Month && now.Day >= daily.FirstDay && now.Day <= daily.LastDay;

    public static bool Opened(SeasonDaily daily, int day) => Has(daily.Key, day.ToString());

    public static void Open(SeasonDaily daily, int day) => Add(daily.Key, day.ToString());
}

/// <summary>
/// Un calendrier de saison : une case par jour du mois, chacune avec son défi
/// RP. Les cases portent le quantième, et le défi d'un jour se lit encore les
/// jours suivants.
/// </summary>
internal sealed record SeasonDaily(Season Season, string Prefix, int Month, int FirstDay, int LastDay, string Art,
                                   FontAwesomeIcon Icon, Func<Loc, string> Title, Func<Loc, string> DayTitle,
                                   Func<Loc, string> Hint, Func<Loc, string[]> Prompts)
{
    public string Key => $"{Prefix}-{SeasonState.Now.Year}";

    public int Count => LastDay - FirstDay + 1;
}

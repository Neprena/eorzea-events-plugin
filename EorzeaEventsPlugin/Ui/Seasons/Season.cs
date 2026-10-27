namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>Les thèmes de saison. <see cref="None"/> : la Nuit dorée du reste de l'année.</summary>
internal enum Season
{
    None,
    NewYear,
    Valentine,
    Easter,
    Halloween,
    Christmas,
}

/// <summary>Une période de thème, bornes incluses, en UTC.</summary>
internal readonly record struct SeasonSpan(Season Season, DateTime Start, DateTime End, bool Official)
{
    public bool Contains(DateTime utc) => utc >= Start && utc <= End;
}

/// <summary>
/// Le calendrier des thèmes, calé sur les événements saisonniers de Square
/// Enix : Heavensturn, Valentione, Hatching-tide, All Saints' Wake et Starlight.
///
/// Deux sources. Les périodes officielles, relevées sur le Lodestone par le
/// site et transmises au plugin (<see cref="UseOfficial"/>), tombent à l'heure
/// près. À défaut, une estimation tirée des années passées, à la journée, sur
/// la date locale du joueur. Une période officielle remplace l'estimation de la
/// même occurrence dès qu'elle est connue, en général une à deux semaines avant
/// l'événement ; une saison pas encore annoncée garde son estimation.
/// </summary>
internal static class SeasonCalendar
{
    /// <summary>Les thèmes dans l'ordre de l'année. Le premier l'emporte quand deux périodes se touchent (31 décembre).</summary>
    public static readonly Season[] All =
        [Season.NewYear, Season.Valentine, Season.Easter, Season.Halloween, Season.Christmas];

    private static IReadOnlyList<SeasonSpan> _official = [];

    /// <summary>Change à chaque nouvelle liste officielle, pour que l'état en vigueur se recalcule.</summary>
    public static int Version { get; private set; }

    public static void UseOfficial(IEnumerable<SeasonSpan> spans)
    {
        _official = [.. spans];
        Version++;
    }

    /// <summary>Le thème de cet instant, ou <see cref="Season.None"/>.</summary>
    public static Season For(DateTime now)
    {
        foreach (var season in All)
            if (Occurrence(season, now) != null) return season;

        return Season.None;
    }

    /// <summary>La période du thème qui contient cet instant, ou null.</summary>
    public static SeasonSpan? Occurrence(Season season, DateTime now)
    {
        var utc = now.ToUniversalTime();
        foreach (var span in Spans(season, now.Year))
            if (span.Contains(utc)) return span;

        return null;
    }

    /// <summary>Le prochain thème à commencer après cet instant, et son début en UTC.</summary>
    public static (Season Season, DateTime Start) Next(DateTime now)
    {
        var utc  = now.ToUniversalTime();
        var best = (Season: Season.None, Start: DateTime.MaxValue);

        foreach (var season in All)
            foreach (var span in Spans(season, now.Year))
                if (span.Start > utc && span.Start < best.Start) best = (season, span.Start);

        return best;
    }

    /// <summary>
    /// Les périodes d'un thème autour d'une année : les officielles connues,
    /// puis les estimations qu'aucune d'elles ne remplace.
    /// </summary>
    private static IEnumerable<SeasonSpan> Spans(Season season, int year)
    {
        foreach (var span in _official)
            if (span.Season == season) yield return span;

        for (var y = year - 1; y <= year + 1; y++)
        {
            var estimate = Estimate(season, y);
            if (!_official.Any(o => o.Season == season && SameOccurrence(o, estimate))) yield return estimate;
        }
    }

    /// <summary>À moins d'un mois d'écart, deux périodes sont la même occurrence de l'événement.</summary>
    private static bool SameOccurrence(SeasonSpan a, SeasonSpan b) =>
        a.Start < b.End.AddDays(30) && b.Start < a.End.AddDays(30);

    /// <summary>
    /// L'estimation d'une occurrence, d'après les dates des années passées.
    /// Jours entiers, du premier à minuit au dernier à 23:59:59, heure locale.
    /// </summary>
    public static SeasonSpan Estimate(Season season, int year)
    {
        var (first, last) = season switch
        {
            // Heavensturn : du 31 décembre à la mi-janvier.
            Season.NewYear   => (new DateTime(year - 1, 12, 31), new DateTime(year, 1, 15)),
            // Valentione's Day : les deux premières semaines de février.
            Season.Valentine => (new DateTime(year, 2, 2), new DateTime(year, 2, 16)),
            // Hatching-tide : deux semaines autour de Pâques, qui bouge chaque année.
            Season.Easter    => (EasterSunday(year).AddDays(-7), EasterSunday(year).AddDays(7)),
            // All Saints' Wake : de la mi-octobre au 2 novembre.
            Season.Halloween => (new DateTime(year, 10, 16), new DateTime(year, 11, 2)),
            // Starlight Celebration : la deuxième quinzaine de décembre.
            Season.Christmas => (new DateTime(year, 12, 16), new DateTime(year, 12, 31)),
            _                => throw new ArgumentOutOfRangeException(nameof(season)),
        };

        return new SeasonSpan(season, LocalToUtc(first), LocalToUtc(last.AddDays(1)).AddTicks(-1), Official: false);
    }

    private static DateTime LocalToUtc(DateTime day) => DateTime.SpecifyKind(day, DateTimeKind.Local).ToUniversalTime();

    /// <summary>Le 1er avril, où les mogs prennent la barre.</summary>
    public static bool IsAprilFools(DateTime date) => date is { Month: 4, Day: 1 };

    /// <summary>
    /// Dimanche de Pâques grégorien, par l'algorithme anonyme (Meeus, Jones,
    /// Butcher). Vérifié : 2026 → 5 avril, 2027 → 28 mars.
    /// </summary>
    public static DateTime EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var n = h + l - 7 * m + 114;

        return new DateTime(year, n / 31, n % 31 + 1);
    }
}

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Les périodes officielles des événements saisonniers, tirées du site deux
/// fois par jour et gardées dans la configuration : un plugin hors ligne, ou
/// lancé avant que le site réponde, garde les dernières connues. Sans aucune,
/// le calendrier s'en tient à ses estimations.
/// </summary>
internal static class SeasonFeed
{
    private static readonly TimeSpan Every = TimeSpan.FromHours(12);
    private static readonly TimeSpan Retry = TimeSpan.FromHours(1);

    private static bool     _loaded;
    private static bool     _fetching;
    private static DateTime _nextFetch;

    /// <summary>Appelé à chaque image par <see cref="SeasonState"/> ; ne fait presque toujours rien.</summary>
    public static void Tick()
    {
        if (!_loaded)
        {
            _loaded = true;
            SeasonCalendar.UseOfficial(ToSpans(Plugin.Config.SeasonWindows));
        }

        if (_fetching || DateTime.UtcNow < _nextFetch) return;

        _fetching = true;
        _ = FetchAsync();
    }

    private static async Task FetchAsync()
    {
        try
        {
            var windows = await Plugin.Api.GetSeasonWindowsAsync();
            _nextFetch = DateTime.UtcNow + (windows == null ? Retry : Every);
            if (windows == null) return;

            var entries = windows
                .Select(w => new SeasonWindowEntry { Season = w.Season, Start = w.Start.ToUniversalTime(), End = w.End.ToUniversalTime() })
                .ToList();

            // La configuration s'écrit sur le thread de jeu, comme partout ailleurs.
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                Plugin.Config.SeasonWindows = entries;
                Plugin.Config.Save();
                SeasonCalendar.UseOfficial(ToSpans(entries));
            });
        }
        catch (Exception ex)
        {
            Plugin.Log.Debug(ex, "[EorzeaEvents] Dates des événements saisonniers non chargées.");
            _nextFetch = DateTime.UtcNow + Retry;
        }
        finally
        {
            _fetching = false;
        }
    }

    private static IEnumerable<SeasonSpan> ToSpans(IEnumerable<SeasonWindowEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (Parse(entry.Season) is not { } season || entry.End <= entry.Start) continue;

            yield return new SeasonSpan(season,
                                        DateTime.SpecifyKind(entry.Start, DateTimeKind.Utc),
                                        DateTime.SpecifyKind(entry.End, DateTimeKind.Utc),
                                        Official: true);
        }
    }

    private static Season? Parse(string key) => key switch
    {
        "newyear"   => Season.NewYear,
        "valentine" => Season.Valentine,
        "easter"    => Season.Easter,
        "halloween" => Season.Halloween,
        "christmas" => Season.Christmas,
        _           => null,
    };
}

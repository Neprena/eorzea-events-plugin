using System.Globalization;
using System.Linq;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Traduit une règle de récurrence iCalendar en une phrase lisible.
///
/// Le plugin ne sait aujourd'hui qu'afficher « récurrent », ce qui n'apprend
/// rien : savoir qu'un événement revient <em>chaque mercredi</em> permet de
/// décider s'il vaut la peine d'être noté.
///
/// Seules les formes réellement produites par le site sont traitées, à savoir
/// une fréquence et une éventuelle liste de jours. Toute règle non reconnue
/// retombe sur le libellé générique.
/// </summary>
internal static class Recurrence
{
    /// <summary>
    /// Décrit la règle donnée, ou renvoie <paramref name="fallback"/> si elle
    /// est absente ou d'une forme non gérée.
    /// </summary>
    public static string Describe(string? rule, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rule)) return fallback;

        var line = ExtractRuleLine(rule);
        if (line.Length == 0) return fallback;

        var frequency = Value(line, "FREQ");
        var days      = Value(line, "BYDAY");
        var interval  = Value(line, "INTERVAL");

        // Un intervalle supérieur à un se dirait « toutes les deux semaines » :
        // hors périmètre, le libellé générique reste plus honnête qu'une phrase
        // fausse.
        if (interval.Length > 0 && interval != "1") return fallback;

        return frequency switch
        {
            "DAILY"   => Plugin.L.RecurrenceDaily,
            "WEEKLY"  => days.Length > 0
                            ? string.Format(Plugin.L.RecurrenceWeeklyOn, DayNames(days))
                            : Plugin.L.RecurrenceWeekly,
            "MONTHLY" => Plugin.L.RecurrenceMonthly,
            _         => fallback,
        };
    }

    /// <summary>La règle est précédée d'une ligne DTSTART, à écarter.</summary>
    private static string ExtractRuleLine(string rule)
    {
        foreach (var line in rule.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
                return trimmed[6..];
        }

        // Certaines règles sont stockées sans préfixe.
        return rule.Contains("FREQ=", StringComparison.OrdinalIgnoreCase)
            ? rule.Trim()
            : string.Empty;
    }

    private static string Value(string rule, string key)
    {
        foreach (var part in rule.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = part.IndexOf('=');
            if (split <= 0) continue;

            if (part[..split].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return part[(split + 1)..].Trim();
        }

        return string.Empty;
    }

    /// <summary>« WE,FR » devient « mercredi et vendredi ».</summary>
    private static string DayNames(string byDay)
    {
        var culture = Plugin.L.Culture;
        var names   = new List<string>();

        foreach (var token in byDay.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            // Un jour peut être préfixé d'un rang, par exemple « 2WE ».
            var code = token.Trim();
            if (code.Length > 2) code = code[^2..];

            var day = code.ToUpperInvariant() switch
            {
                "MO" => DayOfWeek.Monday,
                "TU" => DayOfWeek.Tuesday,
                "WE" => DayOfWeek.Wednesday,
                "TH" => DayOfWeek.Thursday,
                "FR" => DayOfWeek.Friday,
                "SA" => DayOfWeek.Saturday,
                "SU" => DayOfWeek.Sunday,
                _    => (DayOfWeek?)null,
            };

            if (day is { } value) names.Add(culture.DateTimeFormat.GetDayName(value));
        }

        return names.Count switch
        {
            0 => string.Empty,
            1 => names[0],
            _ => string.Join(", ", names.Take(names.Count - 1)) + $" {Plugin.L.And} " + names[^1],
        };
    }
}

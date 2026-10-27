using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Ui.Components;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// « Ton année RP », du 1er au 6 janvier, en tête de la page RP Ouvert : les
/// personnages rencontrés pour la première fois l'année écoulée, celui qu'on a
/// le plus croisé, la zone des rencontres les plus nombreuses.
///
/// Calculé sur l'ordinateur du joueur depuis le registre des rencontres, sans
/// rien demander au site. Fermé, il ne revient pas avant l'an prochain. Sur la
/// date seule, comme le calendrier de l'Avent : il ne suit pas la Heavensturn.
/// </summary>
internal static class YearRecap
{
    private sealed record Stats(int Year, int Met, string? Familiar, int Visits, string? Zone);

    private static Stats?   _stats;
    private static DateTime _computedOn;

    private static string DismissKey => $"recap-{SeasonState.Now.Year}";

    public static void Draw(string activeId, Action openEncounters)
    {
        if (activeId != "rp" || !SeasonState.EasterEggs) return;
        if (SeasonState.Now is not { Month: 1, Day: <= 6 } || SeasonHunts.Has(DismissKey, "closed")) return;
        if (Compute() is not { } stats) return;

        var l = Plugin.L;

        using (Card.Begin("season_recap", interactive: false))
        {
            Text.Small(string.Format(l.RecapKicker, stats.Year).ToUpper(l.Culture), Theme.Accent);
            Layout.Spacer(Theme.GapS);

            if (ImGui.BeginTable("##recap", 3, ImGuiTableFlags.None))
            {
                Column(stats.Met.ToString(), l.RecapMet);
                Column(stats.Familiar, stats.Familiar == null ? null : string.Format(l.RecapFamiliar, stats.Visits));
                Column(stats.Zone, l.RecapZone);
                ImGui.EndTable();
            }

            Layout.Spacer(Theme.GapS);
            Text.Hint(l.RecapLocal, Theme.TextFaint);
            Layout.Spacer(Theme.GapXs);

            if (Btn.Draw(l.BannerDismiss, BtnTone.Secondary, BtnSize.Small, id: "recap_close"))
                SeasonHunts.Add(DismissKey, "closed");
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.RecapOpen, BtnTone.Primary, BtnSize.Small, Icons.History, id: "recap_open"))
                openEncounters();
        }

        Layout.Spacer(Theme.GapM);
    }

    /// <summary>Une colonne : la valeur en grand dans l'or des titres, son sens en dessous.</summary>
    private static void Column(string? value, string? caption)
    {
        ImGui.TableNextColumn();
        if (value == null || caption == null) return;

        using (Fonts.PushH1())
            Text.Wrapped(value, Theme.TitleGold);
        Text.Hint(caption);
    }

    /// <summary>Recalculé une fois par jour : le registre peut compter des milliers d'entrées.</summary>
    private static Stats? Compute()
    {
        var today = SeasonState.Now.Date;
        if (_computedOn == today) return _stats;
        _computedOn = today;

        var year = today.Year - 1;
        var all  = Plugin.Config.Encounters.Values;

        var met  = all.Count(e => e.FirstMetAt.ToLocalTime().Year == year);
        var seen = all.Where(e => (e.LastSeenAt ?? e.FirstMetAt).ToLocalTime().Year == year).ToList();

        var familiar = seen.Where(e => e.Visits > 1).MaxBy(e => e.Visits);
        var zone = seen.Where(e => e.LastZone is { Length: > 0 })
                       .GroupBy(e => e.LastZone!)
                       .MaxBy(g => g.Count())?.Key;

        _stats = met == 0 && familiar == null
            ? null
            : new Stats(year, met, familiar is null ? null : familiar.RpName ?? familiar.Name, familiar?.Visits ?? 0, zone);

        return _stats;
    }
}

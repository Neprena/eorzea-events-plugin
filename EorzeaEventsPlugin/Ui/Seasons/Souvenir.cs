using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Ui.Components;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Le souvenir de la fête de la Commémoration, en tête de RP Ouvert : le
/// personnage croisé pour la première fois il y a un an jour pour jour. S'il y
/// en a plusieurs, celui qu'on a revu le plus souvent depuis.
///
/// Tiré du registre des rencontres, sans rien demander au site, comme
/// <see cref="YearRecap"/>. Fermé, il revient le lendemain avec le souvenir du
/// jour, s'il y en a un.
/// </summary>
internal static class Souvenir
{
    private static RpEncounter? _pick;
    private static DateTime     _computedOn;

    private static string DismissKey => $"rising-{SeasonState.Now.Year}";

    public static void Draw(string activeId, Action openEncounters)
    {
        if (activeId != "rp" || !SeasonState.EasterEggs || SeasonState.Current != Season.Rising) return;

        var day = SeasonState.Now.ToString("MMdd");
        if (SeasonHunts.Has(DismissKey, day) || Pick() is not { } encounter) return;

        var l = Plugin.L;

        using (Card.Begin("season_souvenir", interactive: false))
        {
            Text.Small(l.SouvenirKicker.ToUpper(l.Culture), Theme.Accent);
            Layout.Spacer(Theme.GapXs);
            Text.Wrapped(string.Format(l.SouvenirMet, encounter.RpName ?? encounter.Name));
            Layout.Spacer(Theme.GapS);

            if (Btn.Draw(l.BannerDismiss, BtnTone.Secondary, BtnSize.Small, id: "souvenir_close"))
                SeasonHunts.Add(DismissKey, day);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.RecapOpen, BtnTone.Primary, BtnSize.Small, Icons.History, id: "souvenir_open"))
                openEncounters();
        }

        Layout.Spacer(Theme.GapM);
    }

    /// <summary>Recalculé une fois par jour : le registre peut compter des milliers d'entrées.</summary>
    private static RpEncounter? Pick()
    {
        var today = SeasonState.Now.Date;
        if (_computedOn == today) return _pick;
        _computedOn = today;

        var anniversary = today.AddYears(-1);
        _pick = Plugin.Config.Encounters.Values
                      .Where(e => e.FirstMetAt.ToLocalTime().Date == anniversary)
                      .MaxBy(e => e.Visits);

        return _pick;
    }
}

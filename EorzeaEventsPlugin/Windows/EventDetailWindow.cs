using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Détail d'un événement.
///
/// La carte de l'agenda coupe la description à cent cinquante caractères et ne
/// dit ni l'heure de fin ni l'adresse complète du lieu. Faute de cette fenêtre,
/// « + d'infos » ouvrait la fiche de l'ÉTABLISSEMENT, ce qui répondait à côté
/// de la question posée.
///
/// Rien n'est demandé au serveur : l'agenda porte déjà tout ce qui s'affiche
/// ici.
/// </summary>
public class EventDetailWindow : ThemedWindow
{
    private EventDto? _event;

    public EventDetailWindow() : base("##eventdetail", ImGuiWindowFlags.None)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 320),
            MaximumSize = new Vector2(760, 720),
        };
    }

    public void Open(EventDto ev)
    {
        _event     = ev;
        WindowName = Glyphs.Safe(ev.Title) + "##eventdetail";
        IsOpen     = true;
    }

    protected override void DrawContents()
    {
        if (_event is not { } ev) return;
        var l = Plugin.L;

        Cover.Draw(Textures.Get(ev.Image));

        // État et récurrence en tête : ce qui change la décision d'y aller se
        // lit avant le titre, comme sur la carte de l'agenda.
        var badge = false;
        if (ev.Cancelled)          { Chip.Draw(l.EventCancelled, ChipTone.Danger); badge = true; }
        else if (ev.IsRecurring)
        {
            Chip.Draw(Recurrence.Describe(ev.RecurrenceRule, l.Recurring), ChipTone.Success, Icons.Recurring);
            badge = true;
        }
        if (badge) Layout.Spacer(Theme.GapXs);

        Text.TitleWrapped(ev.Title, ev.Cancelled ? Theme.TextMuted : Theme.Accent);

        if (FormatWhen(ev) is { Length: > 0 } when)
        {
            Layout.Spacer(Theme.GapXs);
            Text.WithIcon(Icons.Clock, when, Theme.TextMuted, Theme.TextFaint, wrap: true);
        }

        // Description entière, là où la carte n'en montrait qu'un résumé.
        if (!string.IsNullOrWhiteSpace(ev.Description))
        {
            Layout.Divider(Theme.GapS);
            MarkdownView.Draw(ev.Description!);
        }

        DrawVenue(ev, l);
        DrawCounters(ev);

        Layout.Divider(Theme.GapM);
        if (Btn.Draw(l.ViewOnline, BtnTone.Secondary, BtnSize.Medium, Icons.External, id: "eventdetail_site"))
            OpenUrl(Plugin.Config.BaseUrl.TrimEnd('/') + "/calendrier");
    }

    /// <summary>Le lieu, son adresse, et de quoi s'y rendre.</summary>
    private static void DrawVenue(EventDto ev, Loc l)
    {
        if (ev.Establishment is not { } venue) return;

        Layout.Spacer(Theme.GapS);
        Layout.Divider(Theme.GapXs);

        Text.WithIcon(Icons.Venues, venue.Name, Theme.Text, Theme.TextMuted, wrap: true);

        var address = FormatAddress(venue, l);
        if (address.Length > 0)
            Text.WithIcon(Icons.Housing, address, Theme.TextMuted, Theme.TextFaint, wrap: true);

        Layout.Spacer(Theme.GapS);
        if (Btn.Draw(l.EstabDetail, BtnTone.Secondary, BtnSize.Medium, Icons.Info, id: "eventdetail_venue"))
            Plugin.OpenEstabDetail(venue);

        TravelButton.Draw(venue, "eventdetail_travel", sameLine: true);
    }

    /// <summary>
    /// Inscrits et joueurs détectés sur place. Les deux ne disent pas la même
    /// chose : l'un compte des intentions, l'autre des présences.
    /// </summary>
    private static void DrawCounters(EventDto ev)
    {
        var attendees = ev.Counts?.Attendees ?? 0;
        if (attendees == 0 && ev.NearbyCount == 0) return;

        Layout.Spacer(Theme.GapS);

        if (attendees > 0)
        {
            Chip.Draw(attendees.ToString(), ChipTone.Neutral, Icons.Friend);
            if (ev.NearbyCount > 0) ImGui.SameLine(0f, Theme.S(Theme.GapXs));
        }
        if (ev.NearbyCount > 0)
            Chip.Draw(ev.NearbyCount.ToString(), ChipTone.Success, Icons.Around);
    }

    /// <summary>
    /// Date et heure, dans le fuseau du joueur. La fin n'est répétée que si
    /// elle tombe un autre jour, sans quoi l'heure suffit.
    /// </summary>
    private static string FormatWhen(EventDto ev)
    {
        if (!DateTime.TryParse(ev.StartDate, out var start)) return string.Empty;
        start = start.ToLocalTime();

        if (!DateTime.TryParse(ev.EndDate, out var end)) return start.ToString("f");

        end = end.ToLocalTime();
        return end.Date == start.Date
            ? $"{start:f} - {end:HH:mm}"
            : $"{start:f} - {end:f}";
    }

    private static string FormatAddress(EstablishmentSummaryDto venue, Loc l)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(venue.Server))   parts.Add(venue.Server!);
        if (!string.IsNullOrEmpty(venue.District)) parts.Add(venue.District!);
        if (venue.Ward.HasValue)
            parts.Add(venue.Plot.HasValue
                          ? string.Format(l.HousingWardPlot, venue.Ward, venue.Plot)
                          : string.Format(l.HousingWard, venue.Ward));
        if (venue.ApartmentNumber.HasValue) parts.Add($"#{venue.ApartmentNumber}");
        return string.Join("  ·  ", parts);
    }

    private static void OpenUrl(string url)
    {
        try { Dalamud.Utility.Util.OpenLink(url); } catch { /* navigateur indisponible */ }
    }
}

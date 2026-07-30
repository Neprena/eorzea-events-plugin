using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ipc;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Bouton « Y aller », qui délègue le trajet à Lifestream.
///
/// Il ne s'affiche que si Lifestream est chargé et si l'adresse est complète :
/// proposer un voyage qui ne peut pas aboutir vaut moins que ne rien proposer,
/// le bouton « Carte » restant disponible dans tous les cas.
/// </summary>
internal static class TravelButton
{
    public static void Draw(HousingAddress? address, string id, bool sameLine = false)
    {
        if (address is not { } target) return;
        if (!Plugin.Config.EnableLifestreamTravel) return;
        if (!Plugin.Lifestream.IsAvailable) return;

        var l    = Plugin.L;
        var busy = Plugin.Lifestream.IsBusy();

        if (sameLine) ImGui.SameLine(0f, Theme.S(Theme.GapS));

        if (Btn.Draw(l.TravelGo, BtnTone.Secondary, BtnSize.Medium, Icons.Travel,
                     disabled: busy, tooltip: busy ? l.TravelBusy : null,
                     id: $"travel_{id}"))
        {
            Plugin.Lifestream.TravelTo(target);
        }
    }

    /// <summary>Variante pour les cartes d'événement, qui n'ont que le résumé du lieu.</summary>
    public static void Draw(EstablishmentSummaryDto venue, string id, bool sameLine = false) =>
        Draw(HousingAddress.From(venue.Server, venue.District, venue.Ward, venue.Plot,
                                 apartmentNumber: null, wing: false, venue.HousingType),
             id, sameLine);

    /// <summary>Variante pour la fiche complète, seule à porter l'aile et le numéro d'appartement.</summary>
    public static void Draw(EstablishmentDto venue, string id, bool sameLine = false) =>
        Draw(HousingAddress.From(venue.Server, venue.District, venue.Ward, venue.Plot,
                                 venue.ApartmentNumber, venue.Wing, venue.HousingType),
             id, sameLine);
}

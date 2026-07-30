using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace EorzeaEventsPlugin.Ipc;

// Entrée du carnet d'adresses de Lifestream, reproduite à l'identique. Les
// ValueTuple sont structurellement compatibles entre assemblies : inutile de
// référencer Lifestream pour lui en passer un.
using AddressTuple = (string Name, int World, int City, int Ward, int PropertyType,
                      int Plot, int Apartment, bool ApartmentSubdivision,
                      bool AliasEnabled, string Alias);

/// <summary>
/// Pont vers Lifestream, qui sait voyager jusqu'à une parcelle de logement.
///
/// Tout est optionnel et défensif : Lifestream peut être absent, désactivé en
/// cours de session, ou avoir changé d'API. Aucune de ces situations ne doit
/// faire tomber le plugin, donc chaque appel est isolé et un échec se traduit
/// simplement par un bouton « Y aller » qui n'apparaît pas.
/// </summary>
internal sealed class LifestreamIpc
{
    private readonly IDalamudPluginInterface _pi;

    private readonly ICallGateSubscriber<bool>   _isBusy;
    private readonly ICallGateSubscriber<object> _abort;
    private readonly ICallGateSubscriber<string, string, string, string, bool, bool, AddressTuple> _build;
    private readonly ICallGateSubscriber<AddressTuple, object> _goTo;

    /// <summary>La présence est resondée périodiquement, pas à chaque frame.</summary>
    private DateTime _lastProbe = DateTime.MinValue;
    private bool     _available;

    public LifestreamIpc(IDalamudPluginInterface pluginInterface)
    {
        _pi = pluginInterface;

        _isBusy    = _pi.GetIpcSubscriber<bool>("Lifestream.IsBusy");
        _abort     = _pi.GetIpcSubscriber<object>("Lifestream.Abort");
        _build     = _pi.GetIpcSubscriber<string, string, string, string, bool, bool, AddressTuple>(
            "Lifestream.BuildAddressBookEntry");
        _goTo      = _pi.GetIpcSubscriber<AddressTuple, object>("Lifestream.GoToHousingAddress");
    }

    /// <summary>
    /// Vrai si Lifestream est installé et chargé. Sondé toutes les 30 s, le
    /// joueur pouvant l'activer sans redémarrer le jeu.
    /// </summary>
    public bool IsAvailable
    {
        get
        {
            if ((DateTime.UtcNow - _lastProbe).TotalSeconds < 30) return _available;
            _lastProbe = DateTime.UtcNow;

            try
            {
                _available = _pi.InstalledPlugins.Any(
                    p => p.InternalName == "Lifestream" && p.IsLoaded);
            }
            catch
            {
                _available = false;
            }

            return _available;
        }
    }

    /// <summary>Vrai si Lifestream exécute déjà un déplacement.</summary>
    public bool IsBusy()
    {
        if (!IsAvailable) return false;
        try   { return _isBusy.InvokeFunc(); }
        catch { return false; }
    }

    /// <summary>Interrompt le déplacement en cours.</summary>
    public void Abort()
    {
        if (!IsAvailable) return;
        try { _abort.InvokeAction(); }
        catch (Exception ex) { Plugin.Log.Warning(ex, "[EorzeaEvents] Lifestream : interruption échouée."); }
    }

    /// <summary>
    /// Lance le voyage. Le retour est immédiat, Lifestream travaille en fond.
    ///
    /// Volontairement pas conditionné à <c>IsQuickTravelAvailable</c> : contre
    /// ce que son nom laisse croire, cette fonction ne dit pas si la
    /// destination est atteignable mais si le joueur se trouve déjà dans le bon
    /// quartier du bon monde, auquel cas Lifestream emprunte l'aethernet
    /// résidentiel. S'en servir pour décider de l'affichage masquerait le
    /// bouton dans la quasi-totalité des cas. <c>GoTo</c> sait faire le trajet
    /// complet, changement de monde compris, et signale lui-même ses refus.
    /// </summary>
    public bool TravelTo(HousingAddress address)
    {
        if (!IsAvailable) return false;

        try
        {
            var entry = Build(address);
            if (!entry.HasValue) return false;

            _goTo.InvokeAction(entry.Value);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[EorzeaEvents] Lifestream : voyage échoué.");
            return false;
        }
    }

    /// <summary>
    /// Traduit l'adresse en entrée de carnet Lifestream.
    ///
    /// Côté Lifestream, <c>BuildAddressBookEntry</c> déréférence sans contrôle
    /// le résultat d'un parseur qui peut échouer : un monde inconnu y lève une
    /// <c>NullReferenceException</c>. D'où le filet ici.
    /// </summary>
    private AddressTuple? Build(HousingAddress a)
    {
        try
        {
            return _build.InvokeFunc(a.World, a.City, a.Ward.ToString(),
                                     a.PlotOrApartment.ToString(),
                                     a.IsApartment, a.IsSubdivision);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, $"[EorzeaEvents] Lifestream : adresse refusée ({a.World}, {a.City}).");
            return null;
        }
    }
}

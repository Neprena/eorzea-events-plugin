namespace EorzeaEventsPlugin.Ipc;

/// <summary>
/// Adresse de logement, sous la forme attendue par Lifestream.
///
/// Lifestream ne prend pas les identifiants de zone du jeu mais des chaînes
/// qu'il reparse lui-même : le nom du monde et un mot-clé de quartier. La
/// conversion depuis le vocabulaire du site (« brumee », « HOUSE »…) est donc
/// faite ici, une fois pour toutes.
/// </summary>
internal readonly record struct HousingAddress(
    string World,
    string City,
    int    Ward,
    int    PlotOrApartment,
    bool   IsApartment,
    bool   IsSubdivision)
{
    /// <summary>
    /// Mots-clés reconnus par <c>ParseResidentialAetheryteKind</c> de
    /// Lifestream. Les libellés traduits ne conviennent pas : « Brumée » ne
    /// contient pas « mist ».
    /// </summary>
    private static readonly Dictionary<string, string> Districts = new()
    {
        ["brumee"]     = "mist",
        ["lavandiere"] = "lavender",
        ["coupe"]      = "goblet",
        ["shirogane"]  = "shiro",
        ["empyree"]    = "empyreum",
    };

    /// <summary>
    /// Construit une adresse depuis les champs d'un établissement, ou retourne
    /// <c>null</c> si elle est incomplète. Un voyage ne peut pas être proposé à
    /// moitié : sans monde, sans quartier ou sans numéro, le bouton n'a pas
    /// lieu d'exister.
    /// </summary>
    public static HousingAddress? From(string? server, string? district, int? ward,
                                       int? plot, int? apartmentNumber, bool wing,
                                       string? housingType)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(district)) return null;
        if (!Districts.TryGetValue(district, out var city)) return null;
        if (ward is not > 0) return null;

        // Le type déclaré prime, mais il manque sur les fiches anciennes : on
        // retombe alors sur le champ effectivement renseigné.
        var isApartment = string.Equals(housingType, "APARTMENT", StringComparison.OrdinalIgnoreCase)
                          || (housingType == null && apartmentNumber.HasValue);

        var number = isApartment ? apartmentNumber : plot;
        if (number is not > 0) return null;

        return new HousingAddress(server, city, ward.Value, number.Value, isApartment,
                                  isApartment && wing);
    }
}

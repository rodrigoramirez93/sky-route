namespace SkyRoute.DataAccessLayer.Airports;

using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Domain;

public sealed class AirportRepository : IAirportRepository
{
    private static readonly IReadOnlyList<Airport> Catalog =
    [
        new Airport("JFK", "John F. Kennedy Intl", "New York", "USA"),
        new Airport("LAX", "Los Angeles Intl", "Los Angeles", "USA"),
        new Airport("ORD", "O'Hare Intl", "Chicago", "USA"),
        new Airport("LHR", "Heathrow", "London", "United Kingdom"),
        new Airport("EZE", "Ministro Pistarini", "Buenos Aires", "Argentina"),
        new Airport("AEP", "Aeroparque Jorge Newbery", "Buenos Aires", "Argentina"),
        new Airport("MAD", "Adolfo Suarez Madrid-Barajas", "Madrid", "Spain")
    ];

    private static readonly Dictionary<string, Airport> ByCode =
        Catalog.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Airport> GetAll() => Catalog;

    public Airport? FindByCode(string code) =>
        !string.IsNullOrWhiteSpace(code) && ByCode.TryGetValue(code, out var airport)
            ? airport
            : null;
}

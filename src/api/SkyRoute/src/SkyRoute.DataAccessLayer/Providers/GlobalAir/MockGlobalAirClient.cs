namespace SkyRoute.DataAccessLayer.Providers.GlobalAir;

using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers.GlobalAir;

public sealed class MockGlobalAirClient : IGlobalAirClient
{
    public Task<IReadOnlyList<GlobalAirRawFlight>> FetchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var flights = MockFlightFactory
            .Generate("GA", providerSeed: 1, criteria, baseFareFloor: 120m, baseFareCeiling: 480m)
            .Select(f => new GlobalAirRawFlight(
                f.FlightNumber,
                f.OriginCode,
                f.DestinationCode,
                f.DepartureUtc,
                f.ArrivalUtc,
                f.Cabin,
                f.BaseFare,
                f.Currency))
            .ToList();

        return Task.FromResult<IReadOnlyList<GlobalAirRawFlight>>(flights);
    }
}

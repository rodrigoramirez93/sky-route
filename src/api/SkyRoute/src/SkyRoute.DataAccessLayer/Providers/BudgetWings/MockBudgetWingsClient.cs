namespace SkyRoute.DataAccessLayer.Providers.BudgetWings;

using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers.BudgetWings;

public sealed class MockBudgetWingsClient : IBudgetWingsClient
{
    public Task<IReadOnlyList<BudgetWingsRawFlight>> FetchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var flights = MockFlightFactory
            .Generate("BW", providerSeed: 2, criteria, baseFareFloor: 30m, baseFareCeiling: 280m)
            .Select(f => new BudgetWingsRawFlight(
                f.FlightNumber,
                f.OriginCode,
                f.DestinationCode,
                f.DepartureUtc,
                f.ArrivalUtc,
                f.Cabin,
                f.BaseFare,
                f.Currency))
            .ToList();

        return Task.FromResult<IReadOnlyList<BudgetWingsRawFlight>>(flights);
    }
}

namespace SkyRoute.DataAccessLayer.Providers;

using SkyRoute.BusinessLogic.Domain;

/// <summary>
/// Deterministic-ish flight generator shared by the mocked provider clients.
/// Produces a stable set of flights per (origin, destination, date, provider seed)
/// so demos and tests aren't flaky.
/// </summary>
internal static class MockFlightFactory
{
    public static IEnumerable<MockFlight> Generate(
        string providerCodePrefix,
        int providerSeed,
        FlightSearchCriteria criteria,
        decimal baseFareFloor,
        decimal baseFareCeiling)
    {
        var seed = HashSeed(providerSeed, criteria);
        var rng = new Random(seed);
        var flightCount = rng.Next(3, 6);

        var departureBase = criteria.DepartureDate
            .ToDateTime(new TimeOnly(6, 0))
            .ToUniversalTime();

        for (var i = 0; i < flightCount; i++)
        {
            var departure = new DateTimeOffset(departureBase.AddHours(i * 3 + rng.Next(0, 2)), TimeSpan.Zero);
            var durationMinutes = rng.Next(90, 600);
            var arrival = departure.AddMinutes(durationMinutes);

            var baseFare = (decimal)(rng.NextDouble() * (double)(baseFareCeiling - baseFareFloor)) + baseFareFloor;
            baseFare = ApplyCabinMultiplier(decimal.Round(baseFare, 2), criteria.Cabin);

            var flightNumber = $"{providerCodePrefix}{rng.Next(100, 999)}";

            yield return new MockFlight(
                FlightNumber: flightNumber,
                OriginCode: criteria.OriginCode,
                DestinationCode: criteria.DestinationCode,
                DepartureUtc: departure,
                ArrivalUtc: arrival,
                Cabin: criteria.Cabin,
                BaseFare: baseFare,
                Currency: "USD");
        }
    }

    private static decimal ApplyCabinMultiplier(decimal baseFare, CabinClass cabin) =>
        cabin switch
        {
            CabinClass.Business => decimal.Round(baseFare * 2.2m, 2),
            CabinClass.First => decimal.Round(baseFare * 3.5m, 2),
            _ => baseFare
        };

    private static int HashSeed(int providerSeed, FlightSearchCriteria criteria) =>
        HashCode.Combine(
            providerSeed,
            criteria.OriginCode.ToUpperInvariant(),
            criteria.DestinationCode.ToUpperInvariant(),
            criteria.DepartureDate.DayNumber,
            (int)criteria.Cabin);
}

internal sealed record MockFlight(
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    CabinClass Cabin,
    decimal BaseFare,
    string Currency);

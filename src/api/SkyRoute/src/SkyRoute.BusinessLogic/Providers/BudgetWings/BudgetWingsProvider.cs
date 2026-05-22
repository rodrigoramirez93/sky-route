namespace SkyRoute.BusinessLogic.Providers.BudgetWings;

using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Domain;

public sealed class BudgetWingsProvider : IFlightProviderStrategy
{
    public const string Key = "BudgetWings";
    private const decimal PromoDiscountMultiplier = 0.90m;
    private const decimal MinimumFinalPrice = 29.99m;

    private readonly IBudgetWingsClient _client;
    private readonly IAirportRepository _airports;

    public BudgetWingsProvider(IBudgetWingsClient client, IAirportRepository airports)
    {
        _client = client;
        _airports = airports;
    }

    public string ProviderKey => Key;

    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var raw = await _client.FetchAsync(criteria, cancellationToken).ConfigureAwait(false);
        var offers = new List<FlightOffer>(raw.Count);

        foreach (var flight in raw)
        {
            var origin = _airports.FindByCode(flight.OriginCode);
            var destination = _airports.FindByCode(flight.DestinationCode);
            if (origin is null || destination is null)
            {
                continue;
            }

            var discounted = PriceCalculator.Round2(flight.BaseFare * PromoDiscountMultiplier);
            var pricePerPassenger = Math.Max(discounted, MinimumFinalPrice);

            offers.Add(new FlightOffer(
                ProviderKey: Key,
                FlightNumber: flight.FlightNumber,
                Origin: origin,
                Destination: destination,
                DepartureUtc: flight.DepartureUtc,
                ArrivalUtc: flight.ArrivalUtc,
                Cabin: flight.Cabin,
                PricePerPassenger: pricePerPassenger,
                Passengers: criteria.Passengers,
                Currency: flight.Currency,
                IsInternational: !string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase)));
        }

        return offers;
    }
}

namespace SkyRoute.BusinessLogic.Providers.GlobalAir;

using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Domain;

public sealed class GlobalAirProvider : IFlightProviderStrategy
{
    public const string Key = "GlobalAir";
    private const decimal FuelSurchargeMultiplier = 1.15m;

    private readonly IGlobalAirClient _client;
    private readonly IAirportRepository _airports;

    public GlobalAirProvider(IGlobalAirClient client, IAirportRepository airports)
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

            var pricePerPassenger = PriceCalculator.Round2(flight.BaseFare * FuelSurchargeMultiplier);

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

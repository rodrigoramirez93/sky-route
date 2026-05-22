namespace SkyRoute.BusinessLogic.Domain;

public sealed record FlightOffer(
    string ProviderKey,
    string FlightNumber,
    Airport Origin,
    Airport Destination,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    CabinClass Cabin,
    decimal PricePerPassenger,
    int Passengers,
    string Currency,
    bool IsInternational)
{
    public TimeSpan Duration => ArrivalUtc - DepartureUtc;

    public decimal TotalPrice =>
        decimal.Round(PricePerPassenger * Passengers, 2, MidpointRounding.AwayFromZero);
}

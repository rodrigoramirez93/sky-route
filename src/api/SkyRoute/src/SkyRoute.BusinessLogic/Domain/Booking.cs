namespace SkyRoute.BusinessLogic.Domain;

public sealed record PassengerDetails(
    string FullName,
    string Email,
    string DocumentNumber);

public sealed record BookingRequest(
    string ProviderKey,
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    CabinClass Cabin,
    int Passengers,
    decimal PricePerPassenger,
    string Currency,
    IReadOnlyList<PassengerDetails> PassengerDetails);

public sealed record BookingConfirmation(
    string Reference,
    decimal TotalPrice,
    string Currency,
    DateTimeOffset CreatedAtUtc);

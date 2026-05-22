namespace SkyRoute.Api.Contracts;

using SkyRoute.BusinessLogic.Domain;

internal static class ContractMappings
{
    public static AirportDto ToDto(this Airport airport) =>
        new(airport.Code, airport.Name, airport.City, airport.Country);

    public static FlightOfferDto ToDto(this FlightOffer offer) =>
        new(
            ProviderKey: offer.ProviderKey,
            FlightNumber: offer.FlightNumber,
            Origin: offer.Origin.ToDto(),
            Destination: offer.Destination.ToDto(),
            DepartureUtc: offer.DepartureUtc,
            ArrivalUtc: offer.ArrivalUtc,
            DurationMinutes: (int)offer.Duration.TotalMinutes,
            Cabin: offer.Cabin,
            PricePerPassenger: offer.PricePerPassenger,
            TotalPrice: offer.TotalPrice,
            Passengers: offer.Passengers,
            Currency: offer.Currency,
            IsInternational: offer.IsInternational);

    public static FlightSearchCriteria ToDomain(this FlightSearchRequestDto dto) =>
        new(
            OriginCode: dto.OriginCode,
            DestinationCode: dto.DestinationCode,
            DepartureDate: dto.DepartureDate,
            Passengers: dto.Passengers,
            Cabin: dto.Cabin);

    public static BookingRequest ToDomain(this BookingRequestDto dto) =>
        new(
            ProviderKey: dto.ProviderKey,
            FlightNumber: dto.FlightNumber,
            OriginCode: dto.OriginCode,
            DestinationCode: dto.DestinationCode,
            DepartureUtc: dto.DepartureUtc,
            ArrivalUtc: dto.ArrivalUtc,
            Cabin: dto.Cabin,
            Passengers: dto.Passengers,
            PricePerPassenger: dto.PricePerPassenger,
            Currency: dto.Currency,
            PassengerDetails: dto.PassengerDetails
                .Select(p => new PassengerDetails(p.FullName, p.Email, p.DocumentNumber))
                .ToList());

    public static BookingConfirmationDto ToDto(this BookingConfirmation confirmation) =>
        new(confirmation.Reference, confirmation.TotalPrice, confirmation.Currency, confirmation.CreatedAtUtc);
}

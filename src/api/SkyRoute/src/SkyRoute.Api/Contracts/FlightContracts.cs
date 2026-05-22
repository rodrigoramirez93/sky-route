namespace SkyRoute.Api.Contracts;

using System.ComponentModel.DataAnnotations;
using SkyRoute.BusinessLogic.Domain;

public sealed class FlightSearchRequestDto
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public required string OriginCode { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public required string DestinationCode { get; init; }

    [Required]
    public required DateOnly DepartureDate { get; init; }

    [Range(1, 9)]
    public int Passengers { get; init; } = 1;

    [Required]
    public CabinClass Cabin { get; init; } = CabinClass.Economy;
}

public sealed record AirportDto(string Code, string Name, string City, string Country);

public sealed record FlightOfferDto(
    string ProviderKey,
    string FlightNumber,
    AirportDto Origin,
    AirportDto Destination,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    int DurationMinutes,
    CabinClass Cabin,
    decimal PricePerPassenger,
    decimal TotalPrice,
    int Passengers,
    string Currency,
    bool IsInternational);

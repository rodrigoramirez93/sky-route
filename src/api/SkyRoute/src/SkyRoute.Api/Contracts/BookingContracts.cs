namespace SkyRoute.Api.Contracts;

using System.ComponentModel.DataAnnotations;
using SkyRoute.BusinessLogic.Domain;

public sealed class PassengerDto
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string DocumentNumber { get; init; }
}

public sealed class BookingRequestDto
{
    [Required]
    public required string ProviderKey { get; init; }

    [Required]
    public required string FlightNumber { get; init; }

    [Required]
    public required string OriginCode { get; init; }

    [Required]
    public required string DestinationCode { get; init; }

    [Required]
    public required DateTimeOffset DepartureUtc { get; init; }

    [Required]
    public required DateTimeOffset ArrivalUtc { get; init; }

    [Required]
    public CabinClass Cabin { get; init; } = CabinClass.Economy;

    [Range(1, 9)]
    public int Passengers { get; init; } = 1;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal PricePerPassenger { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public required string Currency { get; init; }

    [Required]
    [MinLength(1)]
    public required List<PassengerDto> PassengerDetails { get; init; }
}

public sealed record BookingConfirmationDto(
    string Reference,
    decimal TotalPrice,
    string Currency,
    DateTimeOffset CreatedAtUtc);

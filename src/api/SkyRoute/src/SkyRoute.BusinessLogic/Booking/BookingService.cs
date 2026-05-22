namespace SkyRoute.BusinessLogic.Booking;

using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Documents;
using SkyRoute.BusinessLogic.Domain;

public interface IBookingService
{
    Task<BookingConfirmation> CreateBookingAsync(
        BookingRequest request,
        CancellationToken cancellationToken);
}

public sealed class BookingValidationException : Exception
{
    public BookingValidationException(string message) : base(message) { }
}

public sealed class BookingService : IBookingService
{
    private readonly IAirportRepository _airports;
    private readonly IDocumentValidatorFactory _documentValidatorFactory;
    private readonly IBookingRepository _repository;
    private readonly IBookingReferenceGenerator _referenceGenerator;
    private readonly TimeProvider _timeProvider;

    public BookingService(
        IAirportRepository airports,
        IDocumentValidatorFactory documentValidatorFactory,
        IBookingRepository repository,
        IBookingReferenceGenerator referenceGenerator,
        TimeProvider timeProvider)
    {
        _airports = airports;
        _documentValidatorFactory = documentValidatorFactory;
        _repository = repository;
        _referenceGenerator = referenceGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<BookingConfirmation> CreateBookingAsync(
        BookingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PassengerDetails.Count == 0 || request.PassengerDetails.Count != request.Passengers)
        {
            throw new BookingValidationException(
                "Passenger count must match the number of passenger detail entries.");
        }

        var origin = _airports.FindByCode(request.OriginCode)
            ?? throw new BookingValidationException($"Unknown origin airport '{request.OriginCode}'.");
        var destination = _airports.FindByCode(request.DestinationCode)
            ?? throw new BookingValidationException($"Unknown destination airport '{request.DestinationCode}'.");

        var isInternational = !string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);
        var documentValidator = _documentValidatorFactory.For(isInternational);

        foreach (var passenger in request.PassengerDetails)
        {
            if (string.IsNullOrWhiteSpace(passenger.FullName))
            {
                throw new BookingValidationException("Passenger full name is required.");
            }

            if (!IsLikelyEmail(passenger.Email))
            {
                throw new BookingValidationException($"Invalid email for passenger '{passenger.FullName}'.");
            }

            if (!documentValidator.IsValid(passenger.DocumentNumber))
            {
                throw new BookingValidationException(
                    $"Invalid {(isInternational ? "passport number" : "national ID")} for passenger '{passenger.FullName}'.");
            }
        }

        var totalPrice = decimal.Round(
            request.PricePerPassenger * request.Passengers,
            2,
            MidpointRounding.AwayFromZero);

        var reference = _referenceGenerator.Generate();
        var createdAt = _timeProvider.GetUtcNow();

        await _repository.AddAsync(
            new BookingRecord(reference, request, totalPrice, createdAt),
            cancellationToken).ConfigureAwait(false);

        return new BookingConfirmation(reference, totalPrice, request.Currency, createdAt);
    }

    private static bool IsLikelyEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var at = email.IndexOf('@');
        return at > 0 && at < email.Length - 1 && email.IndexOf('.', at) > at;
    }
}

namespace SkyRoute.BusinessLogic.Booking;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Diagnostics;
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
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IAirportRepository airports,
        IDocumentValidatorFactory documentValidatorFactory,
        IBookingRepository repository,
        IBookingReferenceGenerator referenceGenerator,
        TimeProvider timeProvider,
        ILogger<BookingService> logger)
    {
        _airports = airports;
        _documentValidatorFactory = documentValidatorFactory;
        _repository = repository;
        _referenceGenerator = referenceGenerator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<BookingConfirmation> CreateBookingAsync(
        BookingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var activity = SkyRouteDiagnostics.ActivitySource.StartActivity(
            SkyRouteDiagnostics.SpanNames.BookingCreate,
            ActivityKind.Internal);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingOrigin, request.OriginCode);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingDestination, request.DestinationCode);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingCabin, request.Cabin.ToString());
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingPassengerCount, request.Passengers);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingProvider, request.ProviderKey);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingCurrency, request.Currency);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Operation"] = SkyRouteDiagnostics.SpanNames.BookingCreate,
            ["Origin"] = request.OriginCode,
            ["Destination"] = request.DestinationCode,
            ["Provider"] = request.ProviderKey,
            ["Cabin"] = request.Cabin,
            ["Passengers"] = request.Passengers,
        });

        if (request.PassengerDetails.Count == 0 || request.PassengerDetails.Count != request.Passengers)
        {
            return FailValidation(activity, "passenger_count_mismatch",
                "Passenger count must match the number of passenger detail entries.");
        }

        var origin = _airports.FindByCode(request.OriginCode);
        if (origin is null)
        {
            return FailValidation(activity, "unknown_origin",
                $"Unknown origin airport '{request.OriginCode}'.");
        }

        var destination = _airports.FindByCode(request.DestinationCode);
        if (destination is null)
        {
            return FailValidation(activity, "unknown_destination",
                $"Unknown destination airport '{request.DestinationCode}'.");
        }

        var isInternational = !string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingIsInternational, isInternational);

        ValidateDocuments(request, isInternational, activity);

        var totalPrice = decimal.Round(
            request.PricePerPassenger * request.Passengers,
            2,
            MidpointRounding.AwayFromZero);

        var reference = _referenceGenerator.Generate();
        var createdAt = _timeProvider.GetUtcNow();

        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingReference, reference);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingTotalPrice, (double)totalPrice);

        await _repository.AddAsync(
            new BookingRecord(reference, request, totalPrice, createdAt),
            cancellationToken).ConfigureAwait(false);

        SkyRouteMetrics.BookingsCreated.Add(
            1,
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.BookingProvider, request.ProviderKey),
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.BookingIsInternational, isInternational),
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.BookingCabin, request.Cabin.ToString()));

        SkyRouteMetrics.BookingTotalPrice.Record(
            (double)totalPrice,
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.BookingCurrency, request.Currency),
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.BookingIsInternational, isInternational));

        _logger.LogInformation(
            "Booking {Reference} created: {Origin}->{Destination} ({Cabin}, {Passengers} pax, {International}) total {Total} {Currency}",
            reference,
            request.OriginCode,
            request.DestinationCode,
            request.Cabin,
            request.Passengers,
            isInternational ? "international" : "domestic",
            totalPrice,
            request.Currency);

        return new BookingConfirmation(reference, totalPrice, request.Currency, createdAt);
    }

    private void ValidateDocuments(BookingRequest request, bool isInternational, Activity? parent)
    {
        using var activity = SkyRouteDiagnostics.ActivitySource.StartActivity(
            SkyRouteDiagnostics.SpanNames.BookingValidateDocuments,
            ActivityKind.Internal);
        var documentType = isInternational ? "passport" : "national_id";
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingDocumentType, documentType);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.BookingPassengerCount, request.Passengers);

        var documentValidator = _documentValidatorFactory.For(isInternational);

        for (var i = 0; i < request.PassengerDetails.Count; i++)
        {
            var passenger = request.PassengerDetails[i];

            if (string.IsNullOrWhiteSpace(passenger.FullName))
            {
                FailValidationInner(activity, parent, "missing_passenger_name",
                    "Passenger full name is required.", i);
            }

            if (!IsLikelyEmail(passenger.Email))
            {
                FailValidationInner(activity, parent, "invalid_email",
                    "Invalid email for one or more passengers.", i);
            }

            if (!documentValidator.IsValid(passenger.DocumentNumber))
            {
                var label = isInternational ? "passport number" : "national ID";
                FailValidationInner(activity, parent, "invalid_document",
                    $"Invalid {label} for one or more passengers.", i);
            }
        }
    }

    private void FailValidationInner(Activity? validateActivity, Activity? createActivity, string reason, string message, int passengerIndex)
    {
        validateActivity?.SetStatus(ActivityStatusCode.Error, reason);
        validateActivity?.SetTag("validation.reason", reason);
        validateActivity?.SetTag("passenger.index", passengerIndex);
        createActivity?.SetStatus(ActivityStatusCode.Error, reason);
        SkyRouteMetrics.BookingValidationFailures.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason));
        _logger.LogWarning(
            "Booking validation failed for passenger #{PassengerIndex}: {Reason}",
            passengerIndex,
            reason);
        throw new BookingValidationException(message);
    }

    private BookingConfirmation FailValidation(Activity? activity, string reason, string message)
    {
        activity?.SetStatus(ActivityStatusCode.Error, reason);
        activity?.SetTag("validation.reason", reason);
        SkyRouteMetrics.BookingValidationFailures.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason));
        _logger.LogWarning("Booking rejected: {Reason}", reason);
        throw new BookingValidationException(message);
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

namespace SkyRoute.BusinessLogic.Diagnostics;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

/// <summary>
/// Central, in-process ActivitySource and Meter used by every SkyRoute layer
/// to emit business-meaningful traces and metrics. The Api project registers
/// these via <c>AddSource("SkyRoute")</c> / <c>AddMeter("SkyRoute")</c> so all
/// custom signals land on a single OTLP pipeline.
/// </summary>
public static class SkyRouteDiagnostics
{
    public const string SourceName = "SkyRoute";

    private static readonly string SourceVersion =
        typeof(SkyRouteDiagnostics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(SkyRouteDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "1.0.0";

    public static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);

    public static readonly Meter Meter = new(SourceName, SourceVersion);

    public static class Attributes
    {
        public const string FlightOrigin = "flight.origin";
        public const string FlightDestination = "flight.destination";
        public const string FlightDepartureDate = "flight.departure_date";
        public const string FlightPassengerCount = "flight.passenger_count";
        public const string FlightCabin = "flight.cabin";
        public const string FlightProvider = "flight.provider";
        public const string FlightProviderCount = "flight.provider_count";
        public const string FlightResultsCount = "flight.results_count";

        public const string BookingReference = "booking.reference";
        public const string BookingIsInternational = "booking.is_international";
        public const string BookingCurrency = "booking.currency";
        public const string BookingTotalPrice = "booking.total_price";
        public const string BookingPassengerCount = "booking.passenger_count";
        public const string BookingDocumentType = "booking.document_type";
        public const string BookingOrigin = "booking.origin";
        public const string BookingDestination = "booking.destination";
        public const string BookingCabin = "booking.cabin";
        public const string BookingProvider = "booking.provider";

        public const string CorrelationId = "enduser.correlation_id";
        public const string SessionId = "session.id";
        public const string CodeNamespace = "code.namespace";
    }

    public static class SpanNames
    {
        public const string FlightsSearch = "Flights.Search";
        public const string FlightsSearchProvider = "Flights.Search.Provider";
        public const string BookingCreate = "Booking.Create";
        public const string BookingValidateDocuments = "Booking.ValidateDocuments";
        public const string AirportsList = "Airports.List";
    }
}

namespace SkyRoute.BusinessLogic.Diagnostics;

using System.Diagnostics.Metrics;

/// <summary>
/// Strongly-typed business metric instruments. Instantiated once and shared via
/// the singleton <see cref="SkyRouteDiagnostics.Meter"/>.
/// </summary>
public static class SkyRouteMetrics
{
    public static readonly Counter<long> FlightSearches =
        SkyRouteDiagnostics.Meter.CreateCounter<long>(
            "skyroute.flights.searches",
            unit: "{search}",
            description: "Number of flight searches issued.");

    public static readonly Histogram<int> FlightSearchResults =
        SkyRouteDiagnostics.Meter.CreateHistogram<int>(
            "skyroute.flights.search.results",
            unit: "{offer}",
            description: "Number of offers returned by a provider for a single search.");

    public static readonly Counter<long> BookingsCreated =
        SkyRouteDiagnostics.Meter.CreateCounter<long>(
            "skyroute.bookings.created",
            unit: "{booking}",
            description: "Number of bookings successfully created.");

    public static readonly Counter<long> BookingValidationFailures =
        SkyRouteDiagnostics.Meter.CreateCounter<long>(
            "skyroute.bookings.validation_failed",
            unit: "{failure}",
            description: "Number of booking attempts rejected by validation.");

    public static readonly Histogram<double> FlightSearchDuration =
        SkyRouteDiagnostics.Meter.CreateHistogram<double>(
            "skyroute.flights.search.duration",
            unit: "ms",
            description: "Wall-clock duration of a single provider flight search.");

    public static readonly UpDownCounter<long> FlightSearchesInFlight =
        SkyRouteDiagnostics.Meter.CreateUpDownCounter<long>(
            "skyroute.flights.search.in_flight",
            unit: "{search}",
            description: "Number of flight searches currently being processed.");

    public static readonly Histogram<double> BookingTotalPrice =
        SkyRouteDiagnostics.Meter.CreateHistogram<double>(
            "skyroute.bookings.total_price",
            unit: "USD",
            description: "Total price of confirmed bookings.");

    public static readonly Counter<long> ProviderCalls =
        SkyRouteDiagnostics.Meter.CreateCounter<long>(
            "skyroute.providers.calls",
            unit: "{call}",
            description: "Outbound calls to a flight provider, tagged by outcome.");
}

namespace SkyRoute.BusinessLogic.Search;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SkyRoute.BusinessLogic.Diagnostics;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers;

public sealed class FlightSearchService : IFlightSearchService
{
    private readonly IReadOnlyList<IFlightProviderStrategy> _providers;
    private readonly ILogger<FlightSearchService> _logger;

    public FlightSearchService(
        IEnumerable<IFlightProviderStrategy> providers,
        ILogger<FlightSearchService> logger)
    {
        _providers = providers.ToList();
        _logger = logger;
    }

    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        using var activity = SkyRouteDiagnostics.ActivitySource.StartActivity(
            SkyRouteDiagnostics.SpanNames.FlightsSearch,
            ActivityKind.Internal);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightOrigin, criteria.OriginCode);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightDestination, criteria.DestinationCode);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightDepartureDate, criteria.DepartureDate.ToString("yyyy-MM-dd"));
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightPassengerCount, criteria.Passengers);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightCabin, criteria.Cabin.ToString());
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightProviderCount, _providers.Count);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Operation"] = SkyRouteDiagnostics.SpanNames.FlightsSearch,
            ["Origin"] = criteria.OriginCode,
            ["Destination"] = criteria.DestinationCode,
            ["DepartureDate"] = criteria.DepartureDate,
            ["Cabin"] = criteria.Cabin,
            ["Passengers"] = criteria.Passengers,
        });

        SkyRouteMetrics.FlightSearches.Add(
            1,
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.FlightOrigin, criteria.OriginCode),
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.FlightDestination, criteria.DestinationCode),
            new KeyValuePair<string, object?>(SkyRouteDiagnostics.Attributes.FlightCabin, criteria.Cabin.ToString()));

        _logger.LogInformation(
            "Flight search {Origin}->{Destination} on {DepartureDate} for {Passengers} pax ({Cabin}) across {ProviderCount} providers",
            criteria.OriginCode,
            criteria.DestinationCode,
            criteria.DepartureDate,
            criteria.Passengers,
            criteria.Cabin,
            _providers.Count);

        var startedAt = Stopwatch.GetTimestamp();
        SkyRouteMetrics.FlightSearchesInFlight.Add(1);

        try
        {
            var tasks = _providers
                .Select(provider => SafeSearchAsync(provider, criteria, cancellationToken))
                .ToArray();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var aggregated = results.SelectMany(r => r).ToList();
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

            activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightResultsCount, aggregated.Count);

            _logger.LogInformation(
                "Flight search completed with {ResultCount} offers in {ElapsedMs:F1} ms",
                aggregated.Count,
                elapsedMs);

            return aggregated;
        }
        finally
        {
            SkyRouteMetrics.FlightSearchesInFlight.Add(-1);
        }
    }

    private async Task<IReadOnlyList<FlightOffer>> SafeSearchAsync(
        IFlightProviderStrategy provider,
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        using var activity = SkyRouteDiagnostics.ActivitySource.StartActivity(
            SkyRouteDiagnostics.SpanNames.FlightsSearchProvider,
            ActivityKind.Internal);
        activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightProvider, provider.ProviderKey);

        var providerTag = new KeyValuePair<string, object?>(
            SkyRouteDiagnostics.Attributes.FlightProvider, provider.ProviderKey);
        var providerStartedAt = Stopwatch.GetTimestamp();

        try
        {
            var offers = await provider.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
            activity?.SetTag(SkyRouteDiagnostics.Attributes.FlightResultsCount, offers.Count);
            SkyRouteMetrics.FlightSearchResults.Record(offers.Count, providerTag);
            SkyRouteMetrics.FlightSearchDuration.Record(
                Stopwatch.GetElapsedTime(providerStartedAt).TotalMilliseconds,
                providerTag);
            SkyRouteMetrics.ProviderCalls.Add(
                1,
                providerTag,
                new KeyValuePair<string, object?>("outcome", "success"));

            _logger.LogDebug(
                "Provider {Provider} returned {ResultCount} offers",
                provider.ProviderKey,
                offers.Count);

            return offers;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            SkyRouteMetrics.FlightSearchDuration.Record(
                Stopwatch.GetElapsedTime(providerStartedAt).TotalMilliseconds,
                providerTag);
            SkyRouteMetrics.ProviderCalls.Add(
                1,
                providerTag,
                new KeyValuePair<string, object?>("outcome", "error"));
            _logger.LogWarning(ex, "Provider {Provider} failed during search", provider.ProviderKey);
            return Array.Empty<FlightOffer>();
        }
    }
}

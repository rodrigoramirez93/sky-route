namespace SkyRoute.BusinessLogic.Search;

using Microsoft.Extensions.Logging;
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

        var tasks = _providers
            .Select(provider => SafeSearchAsync(provider, criteria, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<FlightOffer>> SafeSearchAsync(
        IFlightProviderStrategy provider,
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Provider {Provider} failed during search", provider.ProviderKey);
            return Array.Empty<FlightOffer>();
        }
    }
}

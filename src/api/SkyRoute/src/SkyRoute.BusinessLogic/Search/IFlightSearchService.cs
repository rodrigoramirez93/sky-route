namespace SkyRoute.BusinessLogic.Search;

using SkyRoute.BusinessLogic.Domain;

public interface IFlightSearchService
{
    Task<IReadOnlyList<FlightOffer>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken);
}

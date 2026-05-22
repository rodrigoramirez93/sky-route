namespace SkyRoute.BusinessLogic.Providers.BudgetWings;

using SkyRoute.BusinessLogic.Domain;

public sealed record BudgetWingsRawFlight(
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    CabinClass Cabin,
    decimal BaseFare,
    string Currency);

public interface IBudgetWingsClient
{
    Task<IReadOnlyList<BudgetWingsRawFlight>> FetchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken);
}

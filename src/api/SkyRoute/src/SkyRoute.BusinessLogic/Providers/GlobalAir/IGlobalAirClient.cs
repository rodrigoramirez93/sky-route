namespace SkyRoute.BusinessLogic.Providers.GlobalAir;

using SkyRoute.BusinessLogic.Domain;

/// <summary>
/// Raw feed returned by the (mocked) GlobalAir client. Lives in BL only as a contract;
/// the implementation is in DataAccessLayer.
/// </summary>
public sealed record GlobalAirRawFlight(
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    CabinClass Cabin,
    decimal BaseFare,
    string Currency);

public interface IGlobalAirClient
{
    Task<IReadOnlyList<GlobalAirRawFlight>> FetchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken);
}

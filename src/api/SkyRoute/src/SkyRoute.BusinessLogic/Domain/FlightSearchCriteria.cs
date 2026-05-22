namespace SkyRoute.BusinessLogic.Domain;

public sealed record FlightSearchCriteria(
    string OriginCode,
    string DestinationCode,
    DateOnly DepartureDate,
    int Passengers,
    CabinClass Cabin);

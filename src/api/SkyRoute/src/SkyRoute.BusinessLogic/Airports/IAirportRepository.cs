namespace SkyRoute.BusinessLogic.Airports;

using SkyRoute.BusinessLogic.Domain;

public interface IAirportRepository
{
    IReadOnlyList<Airport> GetAll();

    Airport? FindByCode(string code);
}

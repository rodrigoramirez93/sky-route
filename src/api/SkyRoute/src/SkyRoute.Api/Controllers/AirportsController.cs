namespace SkyRoute.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Contracts;
using SkyRoute.BusinessLogic.Airports;

[ApiController]
[Route("api/airports")]
public sealed class AirportsController : ControllerBase
{
    private readonly IAirportRepository _airports;

    public AirportsController(IAirportRepository airports)
    {
        _airports = airports;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AirportDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AirportDto>> GetAll() =>
        Ok(_airports.GetAll().Select(a => a.ToDto()).ToList());
}

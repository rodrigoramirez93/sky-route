namespace SkyRoute.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Contracts;
using SkyRoute.BusinessLogic.Search;

[ApiController]
[Route("api/flights")]
public sealed class FlightsController : ControllerBase
{
    private readonly IFlightSearchService _searchService;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(IFlightSearchService searchService, ILogger<FlightsController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(IReadOnlyList<FlightOfferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<FlightOfferDto>>> Search(
        [FromBody] FlightSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.Equals(request.OriginCode, request.DestinationCode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Flight search rejected: origin and destination match ({Origin})",
                request.OriginCode);
            ModelState.AddModelError(nameof(request.DestinationCode), "Destination must differ from origin.");
            return ValidationProblem(ModelState);
        }

        var offers = await _searchService.SearchAsync(request.ToDomain(), cancellationToken).ConfigureAwait(false);
        return Ok(offers.Select(o => o.ToDto()).ToList());
    }
}

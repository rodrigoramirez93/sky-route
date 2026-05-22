namespace SkyRoute.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Contracts;
using SkyRoute.BusinessLogic.Booking;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookingConfirmationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingConfirmationDto>> Create(
        [FromBody] BookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmation = await _bookingService
                .CreateBookingAsync(request.ToDomain(), cancellationToken)
                .ConfigureAwait(false);

            return CreatedAtAction(nameof(Create), new { reference = confirmation.Reference }, confirmation.ToDto());
        }
        catch (BookingValidationException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }
}

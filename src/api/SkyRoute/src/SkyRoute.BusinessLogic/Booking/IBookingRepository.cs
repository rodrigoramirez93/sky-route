namespace SkyRoute.BusinessLogic.Booking;

using SkyRoute.BusinessLogic.Domain;

public interface IBookingRepository
{
    Task<BookingRecord> AddAsync(BookingRecord record, CancellationToken cancellationToken);

    Task<BookingRecord?> FindAsync(string reference, CancellationToken cancellationToken);
}

public sealed record BookingRecord(
    string Reference,
    BookingRequest Request,
    decimal TotalPrice,
    DateTimeOffset CreatedAtUtc);

namespace SkyRoute.DataAccessLayer.Booking;

using System.Collections.Concurrent;
using SkyRoute.BusinessLogic.Booking;

public sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<string, BookingRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public Task<BookingRecord> AddAsync(BookingRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records[record.Reference] = record;
        return Task.FromResult(record);
    }

    public Task<BookingRecord?> FindAsync(string reference, CancellationToken cancellationToken)
    {
        _records.TryGetValue(reference, out var record);
        return Task.FromResult(record);
    }
}

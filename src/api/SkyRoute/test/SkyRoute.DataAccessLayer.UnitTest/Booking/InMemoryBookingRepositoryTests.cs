namespace SkyRoute.DataAccessLayer.UnitTest.Booking;

using SkyRoute.BusinessLogic.Booking;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.DataAccessLayer.Booking;
using Xunit;

public sealed class InMemoryBookingRepositoryTests
{
    [Fact]
    public async Task AddAndFind_Roundtrip()
    {
        var repo = new InMemoryBookingRepository();
        var record = new BookingRecord(
            "SR-ABC123",
            new BookingRequest(
                "GlobalAir", "GA1", "JFK", "LHR",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(7),
                CabinClass.Economy, 1, 100m, "USD",
                new List<PassengerDetails> { new("J Doe", "j@x.com", "AB123456") }),
            100m,
            DateTimeOffset.UtcNow);

        await repo.AddAsync(record, CancellationToken.None);

        var found = await repo.FindAsync("SR-ABC123", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(record.Reference, found!.Reference);
    }

    [Fact]
    public async Task Find_UnknownReturnsNull()
    {
        var repo = new InMemoryBookingRepository();
        var found = await repo.FindAsync("nope", CancellationToken.None);
        Assert.Null(found);
    }
}

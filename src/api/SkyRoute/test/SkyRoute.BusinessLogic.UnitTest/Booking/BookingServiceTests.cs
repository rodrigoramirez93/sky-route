namespace SkyRoute.BusinessLogic.UnitTest.Booking;

using Moq;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Booking;
using SkyRoute.BusinessLogic.Documents;
using SkyRoute.BusinessLogic.Domain;
using Xunit;

public sealed class BookingServiceTests
{
    private static readonly Airport Jfk = new("JFK", "JFK", "NY", "USA");
    private static readonly Airport Lhr = new("LHR", "LHR", "London", "UK");
    private static readonly Airport Lax = new("LAX", "LAX", "LA", "USA");

    [Fact]
    public async Task International_RequiresPassportAndPersists()
    {
        var (service, repo) = BuildService(Jfk, Lhr);

        var request = NewRequest(
            origin: "JFK",
            destination: "LHR",
            passengers: 2,
            pricePerPassenger: 200m,
            doc: "AB123456");

        var confirmation = await service.CreateBookingAsync(request, CancellationToken.None);

        Assert.StartsWith("SR-", confirmation.Reference);
        Assert.Equal(400m, confirmation.TotalPrice);
        repo.Verify(r => r.AddAsync(It.IsAny<BookingRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Domestic_RejectsPassportFormatDocument()
    {
        var (service, _) = BuildService(Jfk, Lax);

        var request = NewRequest("JFK", "LAX", 1, 150m, doc: "ABCDE6789");

        await Assert.ThrowsAsync<BookingValidationException>(() =>
            service.CreateBookingAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task PassengerCountMismatch_Throws()
    {
        var (service, _) = BuildService(Jfk, Lax);

        var request = NewRequest("JFK", "LAX", passengers: 3, pricePerPassenger: 100m, doc: "12345");

        var bad = request with { Passengers = 2 };
        await Assert.ThrowsAsync<BookingValidationException>(() =>
            service.CreateBookingAsync(bad, CancellationToken.None));
    }

    [Fact]
    public async Task UnknownAirport_Throws()
    {
        var airports = new Mock<IAirportRepository>();
        airports.Setup(a => a.FindByCode("JFK")).Returns(Jfk);
        airports.Setup(a => a.FindByCode("ZZZ")).Returns((Airport?)null);

        var service = new BookingService(
            airports.Object,
            new DocumentValidatorFactory(),
            new Mock<IBookingRepository>().Object,
            new BookingReferenceGenerator(),
            TimeProvider.System);

        var request = NewRequest("JFK", "ZZZ", 1, 100m, "12345");

        await Assert.ThrowsAsync<BookingValidationException>(() =>
            service.CreateBookingAsync(request, CancellationToken.None));
    }

    private static (BookingService service, Mock<IBookingRepository> repo) BuildService(Airport origin, Airport dest)
    {
        var airports = new Mock<IAirportRepository>();
        airports.Setup(a => a.FindByCode(origin.Code)).Returns(origin);
        airports.Setup(a => a.FindByCode(dest.Code)).Returns(dest);

        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<BookingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingRecord r, CancellationToken _) => r);

        var service = new BookingService(
            airports.Object,
            new DocumentValidatorFactory(),
            repo.Object,
            new BookingReferenceGenerator(),
            TimeProvider.System);

        return (service, repo);
    }

    private static BookingRequest NewRequest(
        string origin, string destination, int passengers, decimal pricePerPassenger, string doc)
    {
        var details = Enumerable.Range(0, passengers)
            .Select(i => new PassengerDetails($"Pax {i}", $"pax{i}@example.com", doc))
            .ToList();

        return new BookingRequest(
            ProviderKey: "GlobalAir",
            FlightNumber: "GA101",
            OriginCode: origin,
            DestinationCode: destination,
            DepartureUtc: new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            ArrivalUtc: new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero),
            Cabin: CabinClass.Economy,
            Passengers: passengers,
            PricePerPassenger: pricePerPassenger,
            Currency: "USD",
            PassengerDetails: details);
    }
}

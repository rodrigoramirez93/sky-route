namespace SkyRoute.BusinessLogic.UnitTest.Providers;

using Moq;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers.GlobalAir;
using Xunit;

public sealed class GlobalAirProviderTests
{
    private static readonly Airport Origin = new("JFK", "JFK", "NY", "USA");
    private static readonly Airport DestinationIntl = new("LHR", "LHR", "London", "United Kingdom");
    private static readonly Airport DestinationDom = new("LAX", "LAX", "LA", "USA");

    [Theory]
    [InlineData(100.00, 115.00)]
    [InlineData(199.99, 229.99)]   // 199.99 * 1.15 = 229.9885 → 229.99
    [InlineData(50.005, 57.51)]    // 50.005 * 1.15 = 57.50575 → 57.51
    public async Task ApplyPricing_AddsFifteenPercentFuelSurcharge(decimal baseFare, decimal expected)
    {
        var provider = BuildProvider(baseFare, Origin, DestinationIntl);
        var criteria = new FlightSearchCriteria("JFK", "LHR", new DateOnly(2026, 1, 1), 2, CabinClass.Economy);

        var offers = await provider.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(offers);
        Assert.Equal(expected, offers[0].PricePerPassenger);
        Assert.Equal(expected * 2, offers[0].TotalPrice);
        Assert.True(offers[0].IsInternational);
        Assert.Equal("GlobalAir", offers[0].ProviderKey);
    }

    [Fact]
    public async Task DomesticRoute_IsTaggedNonInternational()
    {
        var provider = BuildProvider(100m, Origin, DestinationDom);
        var criteria = new FlightSearchCriteria("JFK", "LAX", new DateOnly(2026, 1, 1), 1, CabinClass.Economy);

        var offers = await provider.SearchAsync(criteria, CancellationToken.None);

        Assert.False(offers[0].IsInternational);
    }

    [Fact]
    public async Task UnknownAirports_AreSkipped()
    {
        var clientMock = new Mock<IGlobalAirClient>();
        clientMock.Setup(c => c.FetchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new GlobalAirRawFlight("GA1", "ZZZ", "LHR",
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), CabinClass.Economy, 100m, "USD")
            });

        var airportsMock = new Mock<IAirportRepository>();
        airportsMock.Setup(a => a.FindByCode("ZZZ")).Returns((Airport?)null);
        airportsMock.Setup(a => a.FindByCode("LHR")).Returns(DestinationIntl);

        var provider = new GlobalAirProvider(clientMock.Object, airportsMock.Object);

        var offers = await provider.SearchAsync(
            new FlightSearchCriteria("ZZZ", "LHR", new DateOnly(2026, 1, 1), 1, CabinClass.Economy),
            CancellationToken.None);

        Assert.Empty(offers);
    }

    private static GlobalAirProvider BuildProvider(decimal baseFare, Airport origin, Airport destination)
    {
        var clientMock = new Mock<IGlobalAirClient>();
        clientMock.Setup(c => c.FetchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new GlobalAirRawFlight(
                    "GA101", origin.Code, destination.Code,
                    new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero),
                    CabinClass.Economy, baseFare, "USD")
            });

        var airportsMock = new Mock<IAirportRepository>();
        airportsMock.Setup(a => a.FindByCode(origin.Code)).Returns(origin);
        airportsMock.Setup(a => a.FindByCode(destination.Code)).Returns(destination);

        return new GlobalAirProvider(clientMock.Object, airportsMock.Object);
    }
}

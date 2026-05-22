namespace SkyRoute.BusinessLogic.UnitTest.Providers;

using Moq;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers.BudgetWings;
using Xunit;

public sealed class BudgetWingsProviderTests
{
    private static readonly Airport Origin = new("JFK", "JFK", "NY", "USA");
    private static readonly Airport DestinationDom = new("LAX", "LAX", "LA", "USA");

    [Theory]
    [InlineData(100.00, 90.00)]    // 100 * 0.9
    [InlineData(50.00, 45.00)]     // above floor
    [InlineData(33.32, 29.99)]     // 33.32 * 0.9 = 29.988 → 29.99 → equals floor
    [InlineData(33.31, 29.99)]     // 33.31 * 0.9 = 29.979 → 29.98 → floor applies
    [InlineData(10.00, 29.99)]     // well below floor
    public async Task ApplyPricing_AppliesDiscountAndEnforcesFloor(decimal baseFare, decimal expected)
    {
        var provider = BuildProvider(baseFare);
        var criteria = new FlightSearchCriteria("JFK", "LAX", new DateOnly(2026, 1, 1), 3, CabinClass.Economy);

        var offers = await provider.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(offers);
        Assert.Equal(expected, offers[0].PricePerPassenger);
        Assert.Equal(decimal.Round(expected * 3, 2), offers[0].TotalPrice);
        Assert.Equal("BudgetWings", offers[0].ProviderKey);
    }

    private static BudgetWingsProvider BuildProvider(decimal baseFare)
    {
        var clientMock = new Mock<IBudgetWingsClient>();
        clientMock.Setup(c => c.FetchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new BudgetWingsRawFlight(
                    "BW1", Origin.Code, DestinationDom.Code,
                    new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero),
                    CabinClass.Economy, baseFare, "USD")
            });

        var airportsMock = new Mock<IAirportRepository>();
        airportsMock.Setup(a => a.FindByCode(Origin.Code)).Returns(Origin);
        airportsMock.Setup(a => a.FindByCode(DestinationDom.Code)).Returns(DestinationDom);

        return new BudgetWingsProvider(clientMock.Object, airportsMock.Object);
    }
}

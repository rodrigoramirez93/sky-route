namespace SkyRoute.BusinessLogic.UnitTest.Search;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers;
using SkyRoute.BusinessLogic.Search;
using Xunit;

public sealed class FlightSearchServiceTests
{
    private static readonly Airport A = new("JFK", "JFK", "NY", "USA");
    private static readonly Airport B = new("LHR", "LHR", "London", "UK");

    [Fact]
    public async Task SearchAsync_AggregatesAllProviderResults()
    {
        var p1 = BuildProvider("P1", new[] { Offer("P1", "X1") });
        var p2 = BuildProvider("P2", new[] { Offer("P2", "Y1"), Offer("P2", "Y2") });

        var service = new FlightSearchService(new[] { p1, p2 }, NullLogger<FlightSearchService>.Instance);

        var result = await service.SearchAsync(
            new FlightSearchCriteria("JFK", "LHR", new DateOnly(2026, 1, 1), 1, CabinClass.Economy),
            CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, o => o.FlightNumber == "X1");
        Assert.Contains(result, o => o.FlightNumber == "Y2");
    }

    [Fact]
    public async Task SearchAsync_SwallowsProviderFailure_ReturnsOthers()
    {
        var failing = new Mock<IFlightProviderStrategy>();
        failing.SetupGet(p => p.ProviderKey).Returns("FAIL");
        failing.Setup(p => p.SearchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ok = BuildProvider("OK", new[] { Offer("OK", "Z1") });

        var service = new FlightSearchService(
            new[] { failing.Object, ok },
            NullLogger<FlightSearchService>.Instance);

        var result = await service.SearchAsync(
            new FlightSearchCriteria("JFK", "LHR", new DateOnly(2026, 1, 1), 1, CabinClass.Economy),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("OK", result[0].ProviderKey);
    }

    private static IFlightProviderStrategy BuildProvider(string key, IEnumerable<FlightOffer> offers)
    {
        var mock = new Mock<IFlightProviderStrategy>();
        mock.SetupGet(p => p.ProviderKey).Returns(key);
        mock.Setup(p => p.SearchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offers.ToList());
        return mock.Object;
    }

    private static FlightOffer Offer(string provider, string flightNumber) =>
        new(provider, flightNumber, A, B,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.Zero),
            CabinClass.Economy, 100m, 1, "USD", true);
}

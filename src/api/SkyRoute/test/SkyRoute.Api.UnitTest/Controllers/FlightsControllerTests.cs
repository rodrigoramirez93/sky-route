namespace SkyRoute.Api.UnitTest.Controllers;

using Microsoft.AspNetCore.Mvc;
using Moq;
using SkyRoute.Api.Contracts;
using SkyRoute.Api.Controllers;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Search;
using Xunit;

public sealed class FlightsControllerTests
{
    [Fact]
    public async Task Search_RejectsSameOriginAndDestination()
    {
        var service = new Mock<IFlightSearchService>();
        var controller = new FlightsController(service.Object);

        var request = new FlightSearchRequestDto
        {
            OriginCode = "JFK",
            DestinationCode = "JFK",
            DepartureDate = new DateOnly(2026, 1, 1),
            Passengers = 1,
            Cabin = CabinClass.Economy
        };

        var result = await controller.Search(request, CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        service.Verify(s => s.SearchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_ReturnsMappedOffers()
    {
        var offer = new FlightOffer(
            "GlobalAir", "GA1",
            new Airport("JFK", "JFK", "NY", "USA"),
            new Airport("LHR", "LHR", "London", "UK"),
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.Zero),
            CabinClass.Economy, 115m, 2, "USD", true);

        var service = new Mock<IFlightSearchService>();
        service.Setup(s => s.SearchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { offer });

        var controller = new FlightsController(service.Object);
        var request = new FlightSearchRequestDto
        {
            OriginCode = "JFK",
            DestinationCode = "LHR",
            DepartureDate = new DateOnly(2026, 1, 1),
            Passengers = 2,
            Cabin = CabinClass.Economy
        };

        var result = await controller.Search(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IReadOnlyList<FlightOfferDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal(230m, dtos[0].TotalPrice);
        Assert.True(dtos[0].IsInternational);
    }
}

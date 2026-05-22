namespace SkyRoute.DataAccessLayer.UnitTest.Airports;

using SkyRoute.DataAccessLayer.Airports;
using Xunit;

public sealed class AirportRepositoryTests
{
    [Fact]
    public void GetAll_ReturnsAtLeastSixAirportsAcrossTwoCountries()
    {
        var repo = new AirportRepository();
        var all = repo.GetAll();

        Assert.True(all.Count >= 6);
        Assert.True(all.Select(a => a.Country).Distinct().Count() >= 2);
    }

    [Theory]
    [InlineData("jfk")]
    [InlineData("JFK")]
    public void FindByCode_IsCaseInsensitive(string code)
    {
        var repo = new AirportRepository();
        var airport = repo.FindByCode(code);
        Assert.NotNull(airport);
        Assert.Equal("JFK", airport!.Code);
    }

    [Fact]
    public void FindByCode_UnknownReturnsNull()
    {
        var repo = new AirportRepository();
        Assert.Null(repo.FindByCode("ZZZ"));
    }
}

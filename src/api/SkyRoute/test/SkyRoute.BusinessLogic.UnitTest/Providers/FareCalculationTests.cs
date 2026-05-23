namespace SkyRoute.BusinessLogic.UnitTest.Providers;

using Moq;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers.BudgetWings;
using SkyRoute.BusinessLogic.Providers.GlobalAir;
using Xunit;

/// <summary>
/// Black-box coverage of the pricing math used by every provider strategy
/// and by <see cref="FlightOffer.TotalPrice"/>. Each test carries the Gherkin
/// scenario it implements so requirement coverage is easy to audit.
///
/// Source of truth: docs/requirements.txt §2 (Business Context) and §3.1
/// (Pricing Display).
/// </summary>
public sealed class FareCalculationTests
{
    private static readonly Airport JfkUsa = new("JFK", "JFK", "New York", "USA");
    private static readonly Airport LaxUsa = new("LAX", "LAX", "Los Angeles", "USA");
    private static readonly Airport LhrUk = new("LHR", "LHR", "London", "United Kingdom");

    // ---------------------------------------------------------------------
    // GlobalAir: base fare + 15% fuel surcharge, rounded to 2 decimals.
    // ---------------------------------------------------------------------

    // Scenario: GlobalAir applies the 15% fuel surcharge to a round base fare
    //   Given a GlobalAir flight with base fare 100.00 USD
    //   When the provider builds the offer
    //   Then the per-passenger price is 115.00 USD
    [Fact]
    public async Task GlobalAir_RoundBaseFare_AddsFifteenPercent()
    {
        var offer = await SingleGlobalAirOffer(baseFare: 100.00m);

        Assert.Equal(115.00m, offer.PricePerPassenger);
    }

    // Scenario: GlobalAir rounds the surcharged price to 2 decimals (half away from zero)
    //   Given a GlobalAir flight with base fare 199.99 USD
    //   When the provider computes 199.99 * 1.15 = 229.9885
    //   Then the per-passenger price is rounded to 229.99 USD
    [Theory]
    [InlineData(199.99, 229.99)]   // 229.9885 → 229.99
    [InlineData(50.005, 57.51)]    // 57.50575 → 57.51 (half-away-from-zero, not bankers')
    [InlineData(123.456, 141.97)]  // 141.9744 → 141.97
    [InlineData(0.01, 0.01)]       // 0.0115 → 0.01
    public async Task GlobalAir_RoundsSurchargedPriceToTwoDecimals_AwayFromZero(decimal baseFare, decimal expected)
    {
        var offer = await SingleGlobalAirOffer(baseFare);

        Assert.Equal(expected, offer.PricePerPassenger);
    }

    // Scenario: GlobalAir handles a zero base fare without negative or NaN output
    //   Given a GlobalAir flight with base fare 0.00 USD
    //   When the provider builds the offer
    //   Then the per-passenger price is 0.00 USD
    [Fact]
    public async Task GlobalAir_ZeroBaseFare_ProducesZeroPrice()
    {
        var offer = await SingleGlobalAirOffer(baseFare: 0m);

        Assert.Equal(0m, offer.PricePerPassenger);
    }

    // ---------------------------------------------------------------------
    // BudgetWings: base fare − 10% discount, floor at $29.99.
    // ---------------------------------------------------------------------

    // Scenario: BudgetWings applies the 10% promotional discount above the floor
    //   Given a BudgetWings flight with base fare 100.00 USD
    //   When the provider applies the 10% discount
    //   Then the per-passenger price is 90.00 USD (above the 29.99 floor)
    [Fact]
    public async Task BudgetWings_RoundBaseFare_AppliesTenPercentDiscount()
    {
        var offer = await SingleBudgetWingsOffer(baseFare: 100.00m);

        Assert.Equal(90.00m, offer.PricePerPassenger);
    }

    // Scenario: BudgetWings rounds the discounted price to 2 decimals before applying the floor
    //   Given a BudgetWings flight with base fare 77.77 USD
    //   When the provider computes 77.77 * 0.9 = 69.993
    //   Then the rounded per-passenger price is 69.99 USD
    [Theory]
    [InlineData(77.77, 69.99)]     // 69.993 → 69.99
    [InlineData(123.45, 111.11)]   // 111.105 → 111.11 (away from zero)
    [InlineData(50.005, 45.00)]    // 45.0045 → 45.00
    public async Task BudgetWings_RoundsDiscountedPriceToTwoDecimals_AwayFromZero(decimal baseFare, decimal expected)
    {
        var offer = await SingleBudgetWingsOffer(baseFare);

        Assert.Equal(expected, offer.PricePerPassenger);
    }

    // Scenario: BudgetWings enforces the $29.99 minimum final price when the discount goes below
    //   Given a BudgetWings flight with a low base fare
    //   When 90% of the fare is below the 29.99 floor
    //   Then the per-passenger price is exactly 29.99 USD
    [Theory]
    [InlineData(10.00)]            // 9.00 → floored to 29.99
    [InlineData(0.00)]             // 0.00 → floored to 29.99
    [InlineData(33.31)]            // 29.979 → 29.98 → floored to 29.99
    [InlineData(33.32)]            // 29.988 → 29.99 → equal to floor
    public async Task BudgetWings_BelowFloor_IsClampedToMinimum(decimal baseFare)
    {
        var offer = await SingleBudgetWingsOffer(baseFare);

        Assert.Equal(29.99m, offer.PricePerPassenger);
    }

    // Scenario: BudgetWings keeps the discounted price when it sits just above the floor
    //   Given a BudgetWings flight with base fare 33.33 USD
    //   When the provider computes 33.33 * 0.9 = 29.997
    //   Then the rounded per-passenger price is 30.00 USD (above floor, no clamp)
    [Fact]
    public async Task BudgetWings_JustAboveFloor_KeepsDiscountedPrice()
    {
        var offer = await SingleBudgetWingsOffer(baseFare: 33.33m);

        Assert.Equal(30.00m, offer.PricePerPassenger);
    }

    // Scenario: The discount is applied to the base fare only, never compounded with the floor
    //   Given a BudgetWings flight with base fare 200.00 USD
    //   When the provider builds the offer
    //   Then the per-passenger price is 180.00 USD (no floor interference for higher fares)
    [Fact]
    public async Task BudgetWings_DiscountIsAppliedToBaseFareOnly()
    {
        var offer = await SingleBudgetWingsOffer(baseFare: 200.00m);

        Assert.Equal(180.00m, offer.PricePerPassenger);
    }

    // ---------------------------------------------------------------------
    // Total price = per-passenger price × passenger count, rounded to 2 dec.
    // Drives the "USD X total / USD Y per person" UI distinction (§3.1).
    // ---------------------------------------------------------------------

    // Scenario: Total price multiplies the per-passenger price by the passenger count
    //   Given a per-passenger price of 160.00 USD and 2 passengers
    //   When the UI reads FlightOffer.TotalPrice
    //   Then it returns 320.00 USD
    [Theory]
    [InlineData(160.00, 2, 320.00)]
    [InlineData(115.00, 1, 115.00)]
    [InlineData(29.99, 9, 269.91)]
    [InlineData(0.00, 5, 0.00)]
    public void TotalPrice_MultipliesPerPassengerByPassengerCount(
        decimal pricePerPassenger, int passengers, decimal expectedTotal)
    {
        var offer = NewOffer(pricePerPassenger, passengers);

        Assert.Equal(expectedTotal, offer.TotalPrice);
    }

    // Scenario: Total price is rounded to 2 decimals (away from zero), matching per-passenger rounding
    //   Given a per-passenger price of 33.335 USD and 3 passengers
    //   When the UI reads FlightOffer.TotalPrice
    //   Then it returns 100.01 USD (not the truncated 100.00)
    [Fact]
    public void TotalPrice_RoundsToTwoDecimals_AwayFromZero()
    {
        var offer = NewOffer(pricePerPassenger: 33.335m, passengers: 3);

        Assert.Equal(100.01m, offer.TotalPrice);
    }

    // Scenario: Per-passenger price and total price are distinct values in the response
    //   Given a GlobalAir flight (base 100, 3 passengers)
    //   When the provider builds the offer
    //   Then PricePerPassenger is 115.00 and TotalPrice is 345.00 — two distinct numbers
    [Fact]
    public async Task PerPassengerAndTotal_AreSurfacedAsDistinctValues()
    {
        var offer = await SingleGlobalAirOffer(baseFare: 100m, passengers: 3);

        Assert.Equal(115.00m, offer.PricePerPassenger);
        Assert.Equal(345.00m, offer.TotalPrice);
        Assert.NotEqual(offer.PricePerPassenger, offer.TotalPrice);
    }

    // ---------------------------------------------------------------------
    // Cross-provider sanity: each operator owns its own math, independent of the other.
    // ---------------------------------------------------------------------

    // Scenario: Identical base fares produce different per-passenger prices across providers
    //   Given two providers receiving the same base fare 100.00 USD
    //   When each applies its own pricing rule
    //   Then GlobalAir charges 115.00 and BudgetWings charges 90.00 per passenger
    [Fact]
    public async Task BothProviders_ApplyOwnRules_OnSameBaseFare()
    {
        var globalAir = await SingleGlobalAirOffer(baseFare: 100m);
        var budgetWings = await SingleBudgetWingsOffer(baseFare: 100m);

        Assert.Equal(115.00m, globalAir.PricePerPassenger);
        Assert.Equal(90.00m, budgetWings.PricePerPassenger);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static FlightOffer NewOffer(decimal pricePerPassenger, int passengers) =>
        new(
            ProviderKey: "Test",
            FlightNumber: "T1",
            Origin: JfkUsa,
            Destination: LhrUk,
            DepartureUtc: new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero),
            ArrivalUtc: new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.Zero),
            Cabin: CabinClass.Economy,
            PricePerPassenger: pricePerPassenger,
            Passengers: passengers,
            Currency: "USD",
            IsInternational: true);

    private static async Task<FlightOffer> SingleGlobalAirOffer(decimal baseFare, int passengers = 1)
    {
        var client = new Mock<IGlobalAirClient>();
        client.Setup(c => c.FetchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new GlobalAirRawFlight(
                    "GA1", JfkUsa.Code, LhrUk.Code,
                    new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.Zero),
                    CabinClass.Economy, baseFare, "USD")
            });

        var airports = new Mock<IAirportRepository>();
        airports.Setup(a => a.FindByCode(JfkUsa.Code)).Returns(JfkUsa);
        airports.Setup(a => a.FindByCode(LhrUk.Code)).Returns(LhrUk);

        var provider = new GlobalAirProvider(client.Object, airports.Object);
        var offers = await provider.SearchAsync(
            new FlightSearchCriteria("JFK", "LHR", new DateOnly(2026, 1, 1), passengers, CabinClass.Economy),
            CancellationToken.None);

        return Assert.Single(offers);
    }

    private static async Task<FlightOffer> SingleBudgetWingsOffer(decimal baseFare, int passengers = 1)
    {
        var client = new Mock<IBudgetWingsClient>();
        client.Setup(c => c.FetchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new BudgetWingsRawFlight(
                    "BW1", JfkUsa.Code, LaxUsa.Code,
                    new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero),
                    CabinClass.Economy, baseFare, "USD")
            });

        var airports = new Mock<IAirportRepository>();
        airports.Setup(a => a.FindByCode(JfkUsa.Code)).Returns(JfkUsa);
        airports.Setup(a => a.FindByCode(LaxUsa.Code)).Returns(LaxUsa);

        var provider = new BudgetWingsProvider(client.Object, airports.Object);
        var offers = await provider.SearchAsync(
            new FlightSearchCriteria("JFK", "LAX", new DateOnly(2026, 1, 1), passengers, CabinClass.Economy),
            CancellationToken.None);

        return Assert.Single(offers);
    }
}

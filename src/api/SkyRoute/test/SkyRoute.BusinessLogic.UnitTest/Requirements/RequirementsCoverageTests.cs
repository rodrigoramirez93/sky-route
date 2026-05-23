namespace SkyRoute.BusinessLogic.UnitTest.Requirements;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Booking;
using SkyRoute.BusinessLogic.Documents;
using SkyRoute.BusinessLogic.Domain;
using SkyRoute.BusinessLogic.Providers;
using SkyRoute.BusinessLogic.Search;
using Xunit;

/// <summary>
/// One-test-per-requirement coverage map driven by docs/requirements.txt.
/// Each test carries the Gherkin scenario it implements so the requirement
/// → test mapping is easy to audit by reading this single file.
///
/// Pure fare math lives in <see cref="Providers.FareCalculationTests"/>;
/// this file focuses on functional requirements §3.1 – §3.4 and §2.
/// Frontend-only requirements (loading indicator, empty state, sortable UI)
/// are validated in the Angular test suite — they are listed here as
/// comments for traceability only.
/// </summary>
public sealed class RequirementsCoverageTests
{
    private static readonly Airport Jfk = new("JFK", "JFK", "New York", "USA");
    private static readonly Airport Lax = new("LAX", "LAX", "Los Angeles", "USA");
    private static readonly Airport Lhr = new("LHR", "LHR", "London", "United Kingdom");

    // =====================================================================
    // §2 Business Context — Provider pricing rules & extensibility
    // =====================================================================

    // Requirement §2: "The platform expects to onboard additional airline providers in the future."
    // Scenario: A new airline provider can be plugged in without changing existing code
    //   Given a fresh IFlightProviderStrategy implementation registered alongside the existing two
    //   When the FlightSearchService runs a search
    //   Then its results are aggregated with the others
    [Fact]
    public async Task NewProvider_CanBeOnboarded_WithoutModifyingExistingStrategies()
    {
        var existing = StubProvider("GlobalAir", new[] { Offer("GlobalAir", "GA1") });
        var newcomer = StubProvider("NewAir", new[] { Offer("NewAir", "NA1"), Offer("NewAir", "NA2") });

        var service = new FlightSearchService(
            new[] { existing, newcomer },
            NullLogger<FlightSearchService>.Instance);

        var results = await service.SearchAsync(SampleCriteria(), CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, o => o.ProviderKey == "NewAir");
    }

    // =====================================================================
    // §3.1 Flight Search — search criteria
    // =====================================================================

    // Requirement §3.1: search form captures origin, destination, date, passengers (1-9), cabin
    // Scenario: The backend search criteria contract carries every form field
    //   Given a user filled in JFK → LHR on 2026-03-15 for 4 passengers in Business
    //   When the API constructs a FlightSearchCriteria
    //   Then every field round-trips intact to the providers
    [Fact]
    public void SearchCriteria_CarriesEveryFormField()
    {
        var criteria = new FlightSearchCriteria(
            OriginCode: "JFK",
            DestinationCode: "LHR",
            DepartureDate: new DateOnly(2026, 3, 15),
            Passengers: 4,
            Cabin: CabinClass.Business);

        Assert.Equal("JFK", criteria.OriginCode);
        Assert.Equal("LHR", criteria.DestinationCode);
        Assert.Equal(new DateOnly(2026, 3, 15), criteria.DepartureDate);
        Assert.Equal(4, criteria.Passengers);
        Assert.Equal(CabinClass.Business, criteria.Cabin);
    }

    // Requirement §3.1: cabin class options are Economy, Business, First
    // Scenario: The domain enum exposes exactly the three cabin classes from the spec
    //   Given the CabinClass enum
    //   When listing its declared values
    //   Then they are Economy, Business and First
    [Fact]
    public void CabinClass_ExposesEconomyBusinessAndFirst()
    {
        var names = Enum.GetNames<CabinClass>();

        Assert.Contains(nameof(CabinClass.Economy), names);
        Assert.Contains(nameof(CabinClass.Business), names);
        Assert.Contains(nameof(CabinClass.First), names);
        Assert.Equal(3, names.Length);
    }

    // =====================================================================
    // §3.1 Flight Search results — response shape needed by the UI
    // =====================================================================

    // Requirement §3.1: results show airline provider, flight number, times, duration, cabin and price
    // Scenario: A FlightOffer surfaces every column the results table needs
    //   Given an offer returned by the search service
    //   When the UI renders the results table
    //   Then provider, flight number, departure, arrival, duration, cabin and price are all available
    [Fact]
    public void FlightOffer_ExposesAllResultsTableColumns()
    {
        var offer = new FlightOffer(
            ProviderKey: "GlobalAir",
            FlightNumber: "GA101",
            Origin: Jfk,
            Destination: Lhr,
            DepartureUtc: new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            ArrivalUtc: new DateTimeOffset(2026, 1, 1, 18, 30, 0, TimeSpan.Zero),
            Cabin: CabinClass.Economy,
            PricePerPassenger: 115.00m,
            Passengers: 2,
            Currency: "USD",
            IsInternational: true);

        Assert.Equal("GlobalAir", offer.ProviderKey);
        Assert.Equal("GA101", offer.FlightNumber);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero), offer.DepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 18, 30, 0, TimeSpan.Zero), offer.ArrivalUtc);
        Assert.Equal(TimeSpan.FromMinutes(510), offer.Duration);
        Assert.Equal(CabinClass.Economy, offer.Cabin);
        Assert.Equal(115.00m, offer.PricePerPassenger);
        Assert.Equal(230.00m, offer.TotalPrice);
    }

    // Requirement §3.1: "USD 320.00 total / USD 160.00 per person" — two distinct numbers
    // Scenario: Per-passenger and total prices are independently addressable on the offer
    //   Given an offer priced at 160.00 USD per passenger for 2 passengers
    //   When the UI renders the "total / per person" label
    //   Then PricePerPassenger returns 160.00 and TotalPrice returns 320.00
    [Fact]
    public void FlightOffer_DistinguishesPerPassengerAndTotalPrice()
    {
        var offer = new FlightOffer(
            "GlobalAir", "GA1", Jfk, Lhr,
            new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.Zero),
            CabinClass.Economy, 160.00m, 2, "USD", true);

        Assert.Equal(160.00m, offer.PricePerPassenger);
        Assert.Equal(320.00m, offer.TotalPrice);
    }

    // =====================================================================
    // §3.2 Sorting — backend exposes data shaped for client-side sorting
    // =====================================================================
    //
    // Note: §3.2 mandates that sorting happens on the frontend. The backend
    // contract must therefore expose price, duration and departure time as
    // first-class fields so the client can sort without a second request.
    // The loading indicator and empty-state UI are validated in the Angular
    // test suite — not testable here by design.

    // Requirement §3.2: sortable by Price, Duration and Departure time — client-side, no extra API call
    // Scenario: Every offer carries the fields the frontend needs to sort locally
    //   Given the API response containing offers
    //   When the client sorts by price, duration or departure time
    //   Then PricePerPassenger, Duration and DepartureUtc are all present without a second call
    [Fact]
    public void FlightOffer_ExposesSortableFields_ForClientSideSorting()
    {
        var offer = new FlightOffer(
            "GlobalAir", "GA1", Jfk, Lhr,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 16, 0, 0, TimeSpan.Zero),
            CabinClass.Economy, 115.00m, 1, "USD", true);

        Assert.True(offer.PricePerPassenger > 0);
        Assert.Equal(TimeSpan.FromHours(6), offer.Duration);
        Assert.NotEqual(default, offer.DepartureUtc);
    }

    // =====================================================================
    // §3.3 Booking flow
    // =====================================================================

    // Requirement §3.3: Confirm Booking submits to the backend and returns a booking reference code
    // Scenario: A valid booking request yields a confirmation with a reference code
    //   Given a valid booking request for a known route, with matching passenger details
    //   When the BookingService creates the booking
    //   Then it returns a non-empty reference code and the total price for the party
    [Fact]
    public async Task ConfirmBooking_ReturnsReferenceCodeAndTotal()
    {
        var (service, _) = BuildBookingService(Jfk, Lhr);

        var confirmation = await service.CreateBookingAsync(
            BookingFor("JFK", "LHR", passengers: 2, pricePerPassenger: 200m, doc: "AB123456"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(confirmation.Reference));
        Assert.Equal(400m, confirmation.TotalPrice);
        Assert.Equal("USD", confirmation.Currency);
    }

    // Requirement §3.3: Booking flow persists the booking
    // Scenario: Successful bookings are written to the repository exactly once
    //   Given a valid booking request
    //   When the BookingService creates the booking
    //   Then exactly one BookingRecord is added to the repository
    [Fact]
    public async Task ConfirmBooking_PersistsRecordExactlyOnce()
    {
        var (service, repo) = BuildBookingService(Jfk, Lhr);

        await service.CreateBookingAsync(
            BookingFor("JFK", "LHR", 1, 150m, "AB123456"),
            CancellationToken.None);

        repo.Verify(r => r.AddAsync(It.IsAny<BookingRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =====================================================================
    // §3.3 Document Type — Passport for international, National ID for domestic
    // =====================================================================

    // Requirement §3.3: international flights require a Passport Number
    //   "For international flights ... the document number field must be labelled
    //    'Passport Number' and validated accordingly."
    // Scenario: International routes pick the passport validator
    //   Given an origin in USA and a destination in the United Kingdom
    //   When the BookingService validates passenger documents
    //   Then a passport-formatted number is accepted
    [Fact]
    public async Task International_Booking_AcceptsPassportNumber()
    {
        var (service, _) = BuildBookingService(Jfk, Lhr);

        var confirmation = await service.CreateBookingAsync(
            BookingFor("JFK", "LHR", 1, 250m, doc: "AB123456"),
            CancellationToken.None);

        Assert.NotNull(confirmation.Reference);
    }

    // Requirement §3.3: domestic flights require a National ID
    //   "For domestic flights, it must be labelled 'National ID' instead."
    // Scenario: Domestic routes pick the national-ID validator
    //   Given an origin and destination both in USA
    //   When the BookingService validates passenger documents
    //   Then a national-ID-formatted number is accepted
    [Fact]
    public async Task Domestic_Booking_AcceptsNationalId()
    {
        var (service, _) = BuildBookingService(Jfk, Lax);

        var confirmation = await service.CreateBookingAsync(
            BookingFor("JFK", "LAX", 1, 120m, doc: "12345678"),
            CancellationToken.None);

        Assert.NotNull(confirmation.Reference);
    }

    // Requirement §3.3: domestic flights reject passport-style documents
    // Scenario: A national-ID-only route rejects a passport-style number
    //   Given a domestic JFK → LAX route
    //   When the user submits a document with letters mixed in
    //   Then the BookingService throws BookingValidationException
    [Fact]
    public async Task Domestic_Booking_RejectsPassportStyleDocument()
    {
        var (service, _) = BuildBookingService(Jfk, Lax);

        await Assert.ThrowsAsync<BookingValidationException>(() =>
            service.CreateBookingAsync(
                BookingFor("JFK", "LAX", 1, 120m, doc: "AB123456"),
                CancellationToken.None));
    }

    // Requirement §3.3: the document validator is selected from the route type
    // Scenario: The factory picks Passport for international routes and NationalId for domestic
    //   Given the document validator factory
    //   When asked for international vs domestic
    //   Then it returns the matching validator kind
    [Theory]
    [InlineData(true, DocumentKind.Passport)]
    [InlineData(false, DocumentKind.NationalId)]
    public void DocumentValidatorFactory_PicksValidatorByRouteType(bool isInternational, DocumentKind expected)
    {
        var factory = new DocumentValidatorFactory();

        Assert.Equal(expected, factory.For(isInternational).Kind);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static FlightSearchCriteria SampleCriteria() =>
        new("JFK", "LHR", new DateOnly(2026, 1, 1), 1, CabinClass.Economy);

    private static IFlightProviderStrategy StubProvider(string key, IEnumerable<FlightOffer> offers)
    {
        var mock = new Mock<IFlightProviderStrategy>();
        mock.SetupGet(p => p.ProviderKey).Returns(key);
        mock.Setup(p => p.SearchAsync(It.IsAny<FlightSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offers.ToList());
        return mock.Object;
    }

    private static FlightOffer Offer(string provider, string flightNumber) =>
        new(provider, flightNumber, Jfk, Lhr,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.Zero),
            CabinClass.Economy, 100m, 1, "USD", true);

    private static (BookingService service, Mock<IBookingRepository> repo) BuildBookingService(
        Airport origin, Airport destination)
    {
        var airports = new Mock<IAirportRepository>();
        airports.Setup(a => a.FindByCode(origin.Code)).Returns(origin);
        airports.Setup(a => a.FindByCode(destination.Code)).Returns(destination);

        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<BookingRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingRecord r, CancellationToken _) => r);

        var service = new BookingService(
            airports.Object,
            new DocumentValidatorFactory(),
            repo.Object,
            new BookingReferenceGenerator(),
            TimeProvider.System,
            NullLogger<BookingService>.Instance);

        return (service, repo);
    }

    private static BookingRequest BookingFor(
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

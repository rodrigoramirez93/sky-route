namespace SkyRoute.Api.Composition;

using Microsoft.Extensions.DependencyInjection.Extensions;
using SkyRoute.BusinessLogic.Airports;
using SkyRoute.BusinessLogic.Booking;
using SkyRoute.BusinessLogic.Documents;
using SkyRoute.BusinessLogic.Providers;
using SkyRoute.BusinessLogic.Providers.BudgetWings;
using SkyRoute.BusinessLogic.Providers.GlobalAir;
using SkyRoute.BusinessLogic.Search;
using SkyRoute.DataAccessLayer.Airports;
using SkyRoute.DataAccessLayer.Booking;
using SkyRoute.DataAccessLayer.Providers.BudgetWings;
using SkyRoute.DataAccessLayer.Providers.GlobalAir;

public static class SkyRouteServiceCollectionExtensions
{
    public static IServiceCollection AddSkyRoute(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IAirportRepository, AirportRepository>();
        services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();
        services.AddSingleton<IBookingReferenceGenerator, BookingReferenceGenerator>();
        services.AddSingleton<IDocumentValidatorFactory, DocumentValidatorFactory>();

        services.AddSingleton<IGlobalAirClient, MockGlobalAirClient>();
        services.AddSingleton<IBudgetWingsClient, MockBudgetWingsClient>();

        // Strategy registrations — each operator is independent. Adding a new
        // operator means appending one line here plus its own classes; no other
        // file needs to change.
        services.AddSingleton<IFlightProviderStrategy, GlobalAirProvider>();
        services.AddSingleton<IFlightProviderStrategy, BudgetWingsProvider>();

        services.AddScoped<IFlightSearchService, FlightSearchService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}

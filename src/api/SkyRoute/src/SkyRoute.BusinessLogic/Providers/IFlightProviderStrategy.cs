namespace SkyRoute.BusinessLogic.Providers;

using SkyRoute.BusinessLogic.Domain;

/// <summary>
/// Strategy contract for flight operators. Each operator is an independent
/// implementation; onboarding a new airline means adding a new class and a
/// single DI registration. No existing strategy is modified.
/// </summary>
public interface IFlightProviderStrategy
{
    string ProviderKey { get; }

    Task<IReadOnlyList<FlightOffer>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken);
}

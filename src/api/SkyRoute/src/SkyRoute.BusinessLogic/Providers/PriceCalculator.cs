namespace SkyRoute.BusinessLogic.Providers;

internal static class PriceCalculator
{
    public static decimal Round2(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

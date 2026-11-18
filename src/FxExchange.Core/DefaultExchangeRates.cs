namespace FxExchange.Core;

/// <summary>
/// The rates given in the exercise: DKK required to purchase 100 units of each currency.
/// Used by default so the tool works offline and gives deterministic results.
/// </summary>
public static class DefaultExchangeRates
{
    public static IReadOnlyDictionary<string, decimal> DkkPer100Units { get; } = new Dictionary<string, decimal>
    {
        ["EUR"] = 743.94m,
        ["USD"] = 663.11m,
        ["GBP"] = 852.85m,
        ["SEK"] = 76.10m,
        ["NOK"] = 78.40m,
        ["CHF"] = 683.58m,
        ["JPY"] = 5.9740m,
    };

    public static InMemoryExchangeRateProvider CreateProvider() => new(DkkPer100Units);
}

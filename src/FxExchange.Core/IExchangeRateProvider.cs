namespace FxExchange.Core;

/// <summary>
/// Source of exchange rates. All rates are quoted against one base currency (DKK),
/// the same way Danmarks Nationalbank publishes them: the amount of DKK needed to buy 100 units.
/// Any pair can then be calculated as a cross rate through DKK.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>ISO codes this provider knows, sorted alphabetically (used in error messages).</summary>
    IReadOnlyCollection<string> SupportedCurrencies { get; }

    /// <summary>Returns the DKK price of 100 units of the currency, or null if the currency is unknown.</summary>
    decimal? GetDkkPer100Units(string isoCode);
}

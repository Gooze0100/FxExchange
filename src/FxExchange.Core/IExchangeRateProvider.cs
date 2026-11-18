namespace FxExchange.Core;

/// <summary>
/// Source of exchange rates. All rates are quoted against one base currency (DKK),
/// the same way Danmarks Nationalbank publishes them: the amount of DKK needed to buy 100 units.
/// Any pair can then be calculated as a cross rate through DKK.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>Sorted alphabetically, because the list is shown in error messages.</summary>
    IReadOnlyCollection<string> SupportedCurrencies { get; }

    /// <summary>Null if the currency is unknown; the caller decides whether that is an error.</summary>
    decimal? GetDkkPer100Units(string isoCode);
}

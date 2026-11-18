namespace FxExchange.Core;

public sealed class CurrencyExchanger
{
    /// <summary>Result precision. The source rates have 4 significant decimals, so more would be false precision.</summary>
    public const int DefaultDecimals = 4;

    private readonly IExchangeRateProvider _rates;
    private readonly int _decimals;

    public CurrencyExchanger(IExchangeRateProvider rates, int decimals = DefaultDecimals)
    {
        ArgumentNullException.ThrowIfNull(rates);
        if (decimals < 0 || decimals > 28)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Decimals must be between 0 and 28.");
        }

        _rates = rates;
        _decimals = decimals;
    }

    /// <summary>
    /// Cross rate through DKK: amount * (DKK per 100 MAIN) / (DKK per 100 MONEY).
    /// The "per 100" cancels out. Multiplying before dividing keeps the most precision.
    /// </summary>
    public decimal Exchange(CurrencyPair pair, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(pair);

        if (amount < 0)
        {
            throw new ExchangeException("The amount to exchange must not be negative.");
        }

        // Validate both currencies first, so "XYZ/XYZ" is reported as unknown rather than silently echoed back.
        var mainRate = GetRate(pair.MainCurrency);
        var moneyRate = GetRate(pair.MoneyCurrency);

        if (pair.IsSameCurrency)
        {
            return amount;
        }

        try
        {
            var exchanged = amount * mainRate / moneyRate;
            return Math.Round(exchanged, _decimals, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new ExchangeException("The amount is too large to exchange.");
        }
    }

    private decimal GetRate(string isoCode) =>
        _rates.GetDkkPer100Units(isoCode) ?? throw new UnknownCurrencyException(isoCode, _rates.SupportedCurrencies);
}

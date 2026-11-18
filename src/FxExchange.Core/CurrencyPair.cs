namespace FxExchange.Core;

/// <summary>
/// An ISO currency pair such as EUR/DKK, where EUR is the main currency and DKK is the money currency.
/// Value object: immutable, validated on creation, compared by value.
/// </summary>
public sealed record CurrencyPair
{
    private CurrencyPair(string mainCurrency, string moneyCurrency)
    {
        MainCurrency = mainCurrency;
        MoneyCurrency = moneyCurrency;
    }

    public string MainCurrency { get; }

    public string MoneyCurrency { get; }

    public bool IsSameCurrency => MainCurrency == MoneyCurrency;

    /// <summary>
    /// Only the format is validated here; whether the currencies are known is the rate provider's job.
    /// </summary>
    public static CurrencyPair Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidCurrencyPairException(text);
        }

        var parts = text.Split('/');
        if (parts.Length != 2)
        {
            throw new InvalidCurrencyPairException(text);
        }

        var main = parts[0].Trim();
        var money = parts[1].Trim();
        if (!IsIsoCodeFormat(main) || !IsIsoCodeFormat(money))
        {
            throw new InvalidCurrencyPairException(text);
        }

        return new CurrencyPair(main.ToUpperInvariant(), money.ToUpperInvariant());
    }

    public override string ToString() => $"{MainCurrency}/{MoneyCurrency}";

    private static bool IsIsoCodeFormat(string code) => code.Length == 3 && code.All(char.IsAsciiLetter);
}

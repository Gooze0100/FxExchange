namespace FxExchange.Core;

/// <summary>
/// Base type for expected, user-facing exchange errors (bad input, unknown currency, ...).
/// The CLI catches this type and prints the message; anything else is a real bug and is allowed to crash.
/// </summary>
public class ExchangeException : Exception
{
    public ExchangeException(string message)
        : base(message)
    {
    }
}

public sealed class InvalidCurrencyPairException : ExchangeException
{
    public string? Input { get; }

    public InvalidCurrencyPairException(string? input)
        : base($"Invalid currency pair '{input}'. Expected the format MAIN/MONEY, e.g. EUR/DKK.")
    {
        Input = input;
    }
}

public sealed class UnknownCurrencyException : ExchangeException
{
    public string CurrencyCode { get; }
    public UnknownCurrencyException(string currencyCode, IEnumerable<string> supportedCurrencies)
        : base($"Unknown currency '{currencyCode}'. Supported currencies: {string.Join(", ", supportedCurrencies)}.")
    {
        CurrencyCode = currencyCode;
    }
}

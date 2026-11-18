using System.Globalization;
using FxExchange.Core;

namespace FxExchange.Tests;

public class CurrencyExchangerTests
{
    private readonly CurrencyExchanger _exchanger = new(DefaultExchangeRates.CreateProvider());

    // decimal can't be used in attributes, so amounts and expected results are invariant-culture strings.
    [Theory]
    [InlineData("EUR/DKK", "1", "7.4394")] // the example from the exercise
    [InlineData("EUR/DKK", "10", "74.394")]
    [InlineData("DKK/EUR", "100", "13.4419")]
    [InlineData("JPY/DKK", "100", "5.974")]
    [InlineData("EUR/USD", "1", "1.1219")] // cross rate, neither side is DKK
    [InlineData("USD/JPY", "1", "110.9993")]
    [InlineData("GBP/SEK", "1", "11.207")]
    [InlineData("CHF/NOK", "250.5", "2184.1427")]
    [InlineData("SEK/NOK", "1000", "970.6633")]
    public void Exchange_KnownPair_ReturnsExpectedAmount(string pair, string amount, string expected)
    {
        var result = _exchanger.Exchange(CurrencyPair.Parse(pair), Dec(amount));

        Assert.Equal(Dec(expected), result);
    }

    [Theory]
    [InlineData("EUR/EUR", "123.456789")]
    [InlineData("DKK/DKK", "1")]
    [InlineData("JPY/JPY", "0")]
    public void Exchange_SameCurrency_ReturnsPassedInAmountUnchanged(string pair, string amount)
    {
        var result = _exchanger.Exchange(CurrencyPair.Parse(pair), Dec(amount));

        Assert.Equal(Dec(amount), result);
    }

    [Fact]
    public void Exchange_ZeroAmount_ReturnsZero()
    {
        Assert.Equal(0m, _exchanger.Exchange(CurrencyPair.Parse("EUR/USD"), 0m));
    }

    [Theory]
    [InlineData("XYZ/DKK", "XYZ")]
    [InlineData("DKK/XYZ", "XYZ")]
    [InlineData("XYZ/XYZ", "XYZ")] // unknown wins over "same currency"
    [InlineData("PLN/USD", "PLN")]
    public void Exchange_UnknownCurrency_ThrowsWithCurrencyCode(string pair, string unknownCode)
    {
        var ex = Assert.Throws<UnknownCurrencyException>(() => _exchanger.Exchange(CurrencyPair.Parse(pair), 1m));

        Assert.Equal(unknownCode, ex.CurrencyCode);
        Assert.Contains("EUR", ex.Message); // message lists the supported currencies
    }

    [Fact]
    public void Exchange_NegativeAmount_Throws()
    {
        Assert.Throws<ExchangeException>(() => _exchanger.Exchange(CurrencyPair.Parse("EUR/DKK"), -1m));
    }

    [Fact]
    public void Exchange_HugeAmount_ThrowsExchangeExceptionInsteadOfOverflow()
    {
        Assert.Throws<ExchangeException>(() => _exchanger.Exchange(CurrencyPair.Parse("GBP/JPY"), decimal.MaxValue));
    }

    [Fact]
    public void Exchange_UsesRatesFromInjectedProvider()
    {
        // Independent of the real numbers: 1 AAA = 2 DKK, 1 BBB = 0.5 DKK, so 1 AAA = 4 BBB.
        var provider = new InMemoryExchangeRateProvider(new Dictionary<string, decimal>
        {
            ["AAA"] = 200m,
            ["BBB"] = 50m,
        });
        var exchanger = new CurrencyExchanger(provider);

        Assert.Equal(4m, exchanger.Exchange(CurrencyPair.Parse("AAA/BBB"), 1m));
        Assert.Equal(0.25m, exchanger.Exchange(CurrencyPair.Parse("BBB/AAA"), 1m));
    }

    [Fact]
    public void Exchange_RoundsMidpointAwayFromZero()
    {
        // 1 AAA = 0.00005 DKK (per 100 units: 0.005), so 1 AAA -> DKK = 0.00005, which rounds to 0.0001 at 4 decimals.
        var provider = new InMemoryExchangeRateProvider(new Dictionary<string, decimal> { ["AAA"] = 0.005m });
        var exchanger = new CurrencyExchanger(provider);

        Assert.Equal(0.0001m, exchanger.Exchange(CurrencyPair.Parse("AAA/DKK"), 1m));
    }

    [Theory]
    [InlineData("EUR", "USD")]
    [InlineData("GBP", "JPY")]
    [InlineData("SEK", "CHF")]
    public void Exchange_RoundTrip_ReturnsCloseToOriginal(string first, string second)
    {
        var precise = new CurrencyExchanger(DefaultExchangeRates.CreateProvider(), decimals: 20);
        var there = precise.Exchange(CurrencyPair.Parse($"{first}/{second}"), 1000m);
        var back = precise.Exchange(CurrencyPair.Parse($"{second}/{first}"), there);

        Assert.InRange(back, 999.9999m, 1000.0001m);
    }

    [Fact]
    public void Constructor_NullProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CurrencyExchanger(null!));
    }

    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}

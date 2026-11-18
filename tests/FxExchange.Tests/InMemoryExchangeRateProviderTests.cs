using FxExchange.Core;

namespace FxExchange.Tests;

public class InMemoryExchangeRateProviderTests
{
    [Fact]
    public void BaseCurrency_IsAlwaysKnown()
    {
        var provider = new InMemoryExchangeRateProvider(new Dictionary<string, decimal>());

        Assert.Equal(100m, provider.GetDkkPer100Units("DKK"));
    }

    [Fact]
    public void Lookup_IsCaseInsensitive()
    {
        var provider = DefaultExchangeRates.CreateProvider();

        Assert.Equal(743.94m, provider.GetDkkPer100Units("eur"));
    }

    [Fact]
    public void Lookup_UnknownCurrency_ReturnsNull()
    {
        Assert.Null(DefaultExchangeRates.CreateProvider().GetDkkPer100Units("XYZ"));
    }

    [Fact]
    public void SupportedCurrencies_ContainsAllDefaultsPlusDkk_Sorted()
    {
        var provider = DefaultExchangeRates.CreateProvider();

        Assert.Equal(
            new[] { "CHF", "DKK", "EUR", "GBP", "JPY", "NOK", "SEK", "USD" },
            provider.SupportedCurrencies);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_NonPositiveRate_Throws(int rate)
    {
        Assert.Throws<ArgumentException>(() =>
            new InMemoryExchangeRateProvider(new Dictionary<string, decimal> { ["EUR"] = rate }));
    }
}

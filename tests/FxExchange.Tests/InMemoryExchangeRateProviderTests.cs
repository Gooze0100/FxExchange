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

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_NonPositiveRate_Throws(int rate)
    {
        Assert.Throws<ArgumentException>(() =>
            new InMemoryExchangeRateProvider(new Dictionary<string, decimal> { ["EUR"] = rate }));
    }
}

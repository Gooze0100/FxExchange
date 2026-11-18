using FxExchange.Core;

namespace FxExchange.Tests;

public class CurrencyPairTests
{
    [Fact]
    public void Parse_ValidPair_SetsMainAndMoneyCurrency()
    {
        var pair = CurrencyPair.Parse("EUR/DKK");

        Assert.Equal("EUR", pair.MainCurrency);
        Assert.Equal("DKK", pair.MoneyCurrency);
    }

    [Theory]
    [InlineData("eur/dkk")]
    [InlineData("Eur/Dkk")]
    [InlineData(" EUR / DKK ")]
    public void Parse_IsCaseInsensitiveAndIgnoresSurroundingSpaces(string input)
    {
        var pair = CurrencyPair.Parse(input);

        Assert.Equal("EUR/DKK", pair.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("EUR")]
    [InlineData("EURDKK")]
    [InlineData("EUR-DKK")]
    [InlineData("EUR/")]
    [InlineData("/DKK")]
    [InlineData("EU/DKK")]
    [InlineData("EURO/DKK")]
    [InlineData("E1R/DKK")]
    [InlineData("EUR/DKK/USD")]
    public void Parse_InvalidFormat_ThrowsInvalidCurrencyPairException(string? input)
    {
        Assert.Throws<InvalidCurrencyPairException>(() => CurrencyPair.Parse(input));
    }

    [Theory]
    [InlineData("EUR/EUR", true)]
    [InlineData("eur/EUR", true)]
    [InlineData("EUR/DKK", false)]
    public void IsSameCurrency_ComparesMainAndMoney(string input, bool expected)
    {
        Assert.Equal(expected, CurrencyPair.Parse(input).IsSameCurrency);
    }

    [Fact]
    public void Pairs_WithSameCurrencies_AreEqual()
    {
        Assert.Equal(CurrencyPair.Parse("EUR/DKK"), CurrencyPair.Parse("eur/dkk"));
    }
}

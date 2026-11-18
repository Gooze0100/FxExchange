using System.Globalization;
using FxExchange.Cli;
using FxExchange.Core;

namespace FxExchange.Tests;

public class ExchangeCommandTests
{
    private static readonly CultureInfo Danish = CultureInfo.GetCultureInfo("da-DK");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void NoArguments_PrintsUsage()
    {
        var (exitCode, output, error) = Run(Danish);

        Assert.Equal(ExchangeCommand.InvalidArguments, exitCode);
        Assert.Equal(ExchangeCommand.Usage, output.Trim());
        Assert.Empty(error);
    }

    [Fact]
    public void ExampleFromExercise_PrintsExchangedAmountWithDanishFormatting()
    {
        var (exitCode, output, error) = Run(Danish, "EUR/DKK", "1");

        Assert.Equal(ExchangeCommand.Success, exitCode);
        Assert.Equal("7,4394", output.Trim());
        Assert.Empty(error);
    }

    [Fact]
    public void EnglishCulture_UsesDecimalPoint()
    {
        var (_, output, _) = Run(English, "EUR/DKK", "1.5");

        Assert.Equal("11.1591", output.Trim());
    }

    [Fact]
    public void DanishCulture_AcceptsDecimalCommaInput()
    {
        var (_, output, _) = Run(Danish, "EUR/DKK", "1,5");

        Assert.Equal("11,1591", output.Trim());
    }

    [Fact]
    public void SameCurrency_EchoesAmount()
    {
        var (exitCode, output, _) = Run(English, "USD/USD", "42.5");

        Assert.Equal(ExchangeCommand.Success, exitCode);
        Assert.Equal("42.5", output.Trim());
    }

    [Fact]
    public void UnknownCurrency_WritesErrorMessage()
    {
        var (exitCode, output, error) = Run(Danish, "EUR/XYZ", "1");

        Assert.Equal(ExchangeCommand.ExchangeFailed, exitCode);
        Assert.Empty(output);
        Assert.Contains("Unknown currency 'XYZ'", error);
    }

    [Fact]
    public void InvalidPair_WritesErrorMessage()
    {
        var (exitCode, _, error) = Run(Danish, "EURDKK", "1");

        Assert.Equal(ExchangeCommand.ExchangeFailed, exitCode);
        Assert.Contains("Invalid currency pair", error);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    public void InvalidAmount_WritesErrorMessage(string amount)
    {
        var (exitCode, _, error) = Run(Danish, "EUR/DKK", amount);

        Assert.Equal(ExchangeCommand.InvalidArguments, exitCode);
        Assert.Contains("Invalid amount", error);
    }

    [Fact]
    public void TooManyArguments_PrintsUsage()
    {
        var (exitCode, output, _) = Run(Danish, "EUR/DKK", "1", "extra");

        Assert.Equal(ExchangeCommand.InvalidArguments, exitCode);
        Assert.Contains("Usage", output);
    }

    [Theory]
    [InlineData("7.4394", "7,4394")]
    [InlineData("74.3940", "74,394")] // trailing zeros removed
    [InlineData("1000000", "1000000")] // no thousands separator
    public void FormatAmount_UsesCultureAndTrimsZeros(string value, string expected)
    {
        var amount = decimal.Parse(value, CultureInfo.InvariantCulture);

        Assert.Equal(expected, ExchangeCommand.FormatAmount(amount, Danish));
    }

    private static (int ExitCode, string Output, string Error) Run(CultureInfo culture, params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = ExchangeCommand.Run(args, DefaultExchangeRates.CreateProvider(), output, error, culture);

        return (exitCode, output.ToString(), error.ToString());
    }
}

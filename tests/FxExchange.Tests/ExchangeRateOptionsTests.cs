using FxExchange.Cli;
using FxExchange.Core;

namespace FxExchange.Tests;

public sealed class ExchangeRateOptionsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FxExchangeTests", Guid.NewGuid().ToString("N"));
    private readonly StringWriter _warnings = new();

    public ExchangeRateOptionsTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort clean-up; a leftover temp file must not fail a test.
        }
    }

    [Fact]
    public void NoSettingsFile_UsesDefaultsWithoutWarning()
    {
        var options = Load();

        Assert.Equal(NationalbankenRateSource.DefaultUri, options.SourceUrl);
        Assert.Equal(ExchangeRateOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
        Assert.Equal(string.Empty, _warnings.ToString());
    }

    [Fact]
    public void ValidSettings_AreUsed()
    {
        WriteSettings("""{"ExchangeRates":{"SourceUrl":"https://example.com/rates.xml","TimeoutSeconds":10}}""");

        var options = Load();

        Assert.Equal(new Uri("https://example.com/rates.xml"), options.SourceUrl);
        Assert.Equal(10, options.TimeoutSeconds);
        Assert.Equal(string.Empty, _warnings.ToString());
    }

    [Fact]
    public void MissingValues_KeepTheirDefaults()
    {
        WriteSettings("""{"ExchangeRates":{"TimeoutSeconds":15}}""");

        var options = Load();

        Assert.Equal(NationalbankenRateSource.DefaultUri, options.SourceUrl);
        Assert.Equal(15, options.TimeoutSeconds);
    }

    [Fact]
    public void MissingSection_UsesDefaults()
    {
        WriteSettings("{}");

        var options = Load();

        Assert.Equal(NationalbankenRateSource.DefaultUri, options.SourceUrl);
        Assert.Equal(ExchangeRateOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/rates.xml")]
    public void InvalidUrl_FallsBackToDefaultWithWarning(string url)
    {
        WriteSettings("{\"ExchangeRates\":{\"SourceUrl\":\"" + url + "\"}}");

        var options = Load();

        Assert.Equal(NationalbankenRateSource.DefaultUri, options.SourceUrl);
        Assert.Contains("Warning", _warnings.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void InvalidTimeout_FallsBackToDefaultWithWarning(int timeout)
    {
        WriteSettings("{\"ExchangeRates\":{\"TimeoutSeconds\":" + timeout + "}}");

        var options = Load();

        Assert.Equal(ExchangeRateOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
        Assert.Contains("TimeoutSeconds", _warnings.ToString());
    }

    [Fact]
    public void BrokenJson_FallsBackToDefaultsWithWarning()
    {
        WriteSettings("{ this is not json");

        var options = Load();

        Assert.Equal(NationalbankenRateSource.DefaultUri, options.SourceUrl);
        Assert.Contains("could not read appsettings.json", _warnings.ToString());
    }

    private ExchangeRateOptions Load() => ExchangeRateOptions.Load(_directory, _warnings);

    private void WriteSettings(string json) => File.WriteAllText(Path.Combine(_directory, "appsettings.json"), json);
}

using FxExchange.Cli;

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
    public void ValidSettings_AreReturned()
    {
        WriteSettings("""{"ExchangeRates":{"SourceUrl":"https://example.com/rates.xml","TimeoutSeconds":10}}""");

        var settings = Load();

        Assert.NotNull(settings);
        Assert.Equal(new Uri("https://example.com/rates.xml"), settings.SourceUrl);
        Assert.Equal(TimeSpan.FromSeconds(10), settings.Timeout);
        Assert.Equal(string.Empty, _warnings.ToString());
    }

    [Fact]
    public void NoSettingsFile_ReturnsNullWithWarning()
    {
        Assert.Null(Load());
        Assert.Contains("Warning", _warnings.ToString());
    }

    [Theory]
    [InlineData("""{"ExchangeRates":{"TimeoutSeconds":5}}""")] // URL missing
    [InlineData("""{"ExchangeRates":{"SourceUrl":"https://example.com/rates.xml"}}""")] // timeout missing
    public void MissingValue_ReturnsNullWithWarning(string json)
    {
        WriteSettings(json);

        Assert.Null(Load());
        Assert.Contains("Warning", _warnings.ToString());
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/rates.xml")]
    [InlineData("http://example.com/rates.xml")] // only https is accepted
    public void InvalidUrl_ReturnsNullWithWarning(string url)
    {
        WriteSettings("{\"ExchangeRates\":{\"SourceUrl\":\"" + url + "\",\"TimeoutSeconds\":5}}");

        Assert.Null(Load());
        Assert.Contains("Warning", _warnings.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void InvalidTimeout_ReturnsNullWithWarning(int timeout)
    {
        WriteSettings("{\"ExchangeRates\":{\"SourceUrl\":\"https://example.com/rates.xml\",\"TimeoutSeconds\":" + timeout + "}}");

        Assert.Null(Load());
        Assert.Contains("TimeoutSeconds", _warnings.ToString());
    }

    [Fact]
    public void BrokenJson_ReturnsNullWithWarning()
    {
        WriteSettings("{ this is not json");

        Assert.Null(Load());
        Assert.Contains("could not read appsettings.json", _warnings.ToString());
    }

    private LiveRateSettings? Load() => ExchangeRateOptions.Load(_directory, _warnings);

    private void WriteSettings(string json) => File.WriteAllText(Path.Combine(_directory, "appsettings.json"), json);
}

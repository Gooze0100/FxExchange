using System.Net;
using FxExchange.Cli;

namespace FxExchange.Tests;

/// <summary>Fake network: lets tests control the HTTP response without calling the real service.</summary>
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage> _respond;

    public StubHandler(Func<HttpResponseMessage> respond) => _respond = respond;

    public int CallCount { get; private set; }

    public Uri? LastRequestUri { get; private set; }

    public static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequestUri = request.RequestUri;
        return Task.FromResult(_respond());
    }
}

internal sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}

/// <summary>A cache file path in its own temp folder, deleted after the test.</summary>
internal sealed class TempCacheFile : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FxExchangeTests", Guid.NewGuid().ToString("N"));

    public TempCacheFile() => FilePath = Path.Combine(_directory, "rates-cache.json");

    public string FilePath { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort clean-up; a leftover temp file must not fail a test.
        }
    }
}

internal static class SampleData
{
    public static readonly LiveRateSettings LiveSettings =
        new(new Uri("https://rates.example.test/currencyratesxml"), TimeSpan.FromSeconds(5));

    public const string NationalbankenXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <exchangerates type="Exchange rates" author="Danmarks Nationalbank" refcur="DKK" refamt="1">
          <dailyrates id="2026-11-13">
            <currency code="EUR" desc="Euro" rate="746.13" />
            <currency code="USD" desc="US dollars" rate="642.50" />
            <currency code="JPY" desc="Japanese yen" rate="4.1830" />
            <currency code="XXX" desc="Broken row" rate="-" />
          </dailyrates>
        </exchangerates>
        """;
}

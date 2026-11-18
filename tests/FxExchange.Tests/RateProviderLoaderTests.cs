using System.Net;
using FxExchange.Cli;
using FxExchange.Core;

namespace FxExchange.Tests;

public sealed class RateProviderLoaderTests : IDisposable
{
    private static readonly DateTimeOffset Today = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    private readonly TempCacheFile _file = new();
    private readonly FixedTimeProvider _clock = new(Today);
    private readonly StringWriter _warnings = new();

    public void Dispose() => _file.Dispose();

    [Fact]
    public async Task NoCache_ServiceWorks_UsesLiveRatesAndSavesThem()
    {
        var handler = WorkingService();
        var cache = CreateCache();

        var provider = await LoadAsync(handler, cache);

        Assert.Equal(746.13m, provider.GetDkkPer100Units("EUR"));
        Assert.Equal(746.13m, cache.TryLoad()!.ToProvider().GetDkkPer100Units("EUR"));
        Assert.Equal(string.Empty, _warnings.ToString());
    }

    [Fact]
    public async Task CacheFromToday_UsesCacheWithoutCallingService()
    {
        var cache = CreateCache();
        cache.TrySave(DefaultExchangeRates.CreateProvider());
        var handler = WorkingService();

        var provider = await LoadAsync(handler, cache);

        Assert.Equal(0, handler.CallCount);
        Assert.Equal(743.94m, provider.GetDkkPer100Units("EUR"));
    }

    [Fact]
    public async Task OldCache_ServiceWorks_UsesLiveRatesAndUpdatesCache()
    {
        var cache = CreateCache();
        cache.TrySave(DefaultExchangeRates.CreateProvider());
        _clock.UtcNow = Today.AddDays(1);

        var provider = await LoadAsync(WorkingService(), cache);

        Assert.Equal(746.13m, provider.GetDkkPer100Units("EUR"));
        Assert.Equal(Today.AddDays(1), cache.TryLoad()!.FetchedAt);
    }

    [Fact]
    public async Task OldCache_ServiceDown_UsesCachedRatesWithWarning()
    {
        var cache = CreateCache();
        cache.TrySave(NationalbankenRateSource.Parse(SampleData.NationalbankenXml));
        _clock.UtcNow = Today.AddDays(3);

        var provider = await LoadAsync(new StubHandler(() => throw new HttpRequestException("no network")), cache);

        Assert.Equal(746.13m, provider.GetDkkPer100Units("EUR"));
        Assert.Contains("Using cached rates from 2026-11-18", _warnings.ToString());
    }

    [Fact]
    public async Task NoCache_ServiceDown_FallsBackToBuiltInRatesWithWarning()
    {
        var provider = await LoadAsync(new StubHandler(() => throw new HttpRequestException("no network")), CreateCache());

        Assert.Equal(743.94m, provider.GetDkkPer100Units("EUR"));
        Assert.Contains("Using built-in rates", _warnings.ToString());
    }

    [Fact]
    public async Task NoCache_ServiceReturnsError_FallsBackToBuiltInRates()
    {
        var handler = new StubHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var provider = await LoadAsync(handler, CreateCache());

        Assert.Equal(743.94m, provider.GetDkkPer100Units("EUR"));
    }

    [Fact]
    public async Task ConfiguredSourceUrl_IsTheOneCalled()
    {
        var handler = WorkingService();
        var url = new Uri("https://example.com/rates.xml");
        using var http = new HttpClient(handler);

        await RateProviderLoader.LoadLiveOrDefaultAsync(http, CreateCache(), _warnings, url);

        Assert.Equal(url, handler.LastRequestUri);
    }

    [Fact]
    public async Task NoSourceUrl_CallsNationalbanken()
    {
        var handler = WorkingService();

        await LoadAsync(handler, CreateCache());

        Assert.Equal(NationalbankenRateSource.DefaultUri, handler.LastRequestUri);
    }

    private static StubHandler WorkingService() => new(() => StubHandler.Ok(SampleData.NationalbankenXml));

    private FileRateCache CreateCache() => new(_file.FilePath, _clock);

    private async Task<IExchangeRateProvider> LoadAsync(StubHandler handler, FileRateCache cache)
    {
        using var http = new HttpClient(handler);
        return await RateProviderLoader.LoadLiveOrDefaultAsync(http, cache, _warnings);
    }
}

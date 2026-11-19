using System.Xml;
using FxExchange.Core;

namespace FxExchange.Cli;

public static class RateProviderLoader
{
    /// <summary>
    /// Picks the best available rates for --live, so the tool always produces an answer:
    /// 1. rates already downloaded today (from the cache, no network call),
    /// 2. otherwise today's rates from the configured source (and saves them to the cache),
    /// 3. if the download fails or live rates are not configured: the last cached rates, with a warning showing their date,
    /// 4. if there is no cache either: the built-in rates, with a warning.
    /// </summary>
    public static async Task<IExchangeRateProvider> LoadLiveOrDefaultAsync(
        HttpClient httpClient,
        FileRateCache cache,
        TextWriter warnings,
        LiveRateSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var cached = cache.TryLoad();
        if (cached is not null && cache.IsFromToday(cached))
        {
            return cached.ToProvider();
        }

        if (settings is null)
        {
            return Fallback(cached, warnings, "live rates are not configured");
        }

        try
        {
            var live = await NationalbankenRateSource.FetchAsync(httpClient, settings.SourceUrl, cancellationToken)
                .ConfigureAwait(false);
            cache.TrySave(live);
            return live;
        }
        catch (Exception ex) when (ex is HttpRequestException
                                       or TaskCanceledException
                                       or XmlException
                                       or ExchangeException)
        {
            return Fallback(cached, warnings, $"could not load live rates ({ex.Message})");
        }
    }

    private static IExchangeRateProvider Fallback(CachedRates? cached, TextWriter warnings, string reason)
    {
        if (cached is not null)
        {
            warnings.WriteLine($"Warning: {reason}. Using cached rates from {cached.FetchedAt.UtcDateTime:yyyy-MM-dd}.");
            return cached.ToProvider();
        }

        warnings.WriteLine($"Warning: {reason}. Using built-in rates.");
        return DefaultExchangeRates.CreateProvider();
    }
}

using System.Text.Json;

namespace FxExchange.Core;

/// <summary>
/// Zero setup: no database, the file is created on first use, and a missing or broken file is simply ignored.
/// </summary>
public sealed class FileRateCache
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly TimeProvider _time;

    public FileRateCache(string filePath, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FilePath = filePath;
        _time = timeProvider ?? TimeProvider.System;
    }

    public static string DefaultFilePath => Path.Combine(Path.GetTempPath(), "FxExchange", "rates-cache.json");

    public string FilePath { get; }

    public CachedRates? TryLoad()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var cached = JsonSerializer.Deserialize<CachedRates>(File.ReadAllText(FilePath), JsonOptions);

            // Treat an empty or tampered file as "no cache" rather than crashing.
            return cached is { DkkPer100Units.Count: > 0 } && cached.DkkPer100Units.Values.All(rate => rate > 0)
                ? cached
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns false instead of throwing if the file can't be written:
    /// a failed cache write must never stop the exchange itself.
    /// </summary>
    public bool TrySave(InMemoryExchangeRateProvider rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var cached = new CachedRates(_time.GetUtcNow(), new Dictionary<string, decimal>(rates.DkkPer100Units));

            // Write to a temporary file first, then swap it in, so a crash mid-write can't leave a half-written cache.
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(cached, JsonOptions));
            File.Move(tempPath, FilePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Nationalbanken publishes once per banking day, so rates fetched today are good enough.</summary>
    public bool IsFromToday(CachedRates cached)
    {
        ArgumentNullException.ThrowIfNull(cached);
        return cached.FetchedAt.UtcDateTime.Date == _time.GetUtcNow().UtcDateTime.Date;
    }
}

public sealed record CachedRates(DateTimeOffset FetchedAt, Dictionary<string, decimal> DkkPer100Units)
{
    public InMemoryExchangeRateProvider ToProvider() => new(DkkPer100Units);
}

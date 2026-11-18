using FxExchange.Core;

namespace FxExchange.Tests;

public sealed class FileRateCacheTests : IDisposable
{
    private static readonly DateTimeOffset Today = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    private readonly TempCacheFile _file = new();
    private readonly FixedTimeProvider _clock = new(Today);

    public void Dispose() => _file.Dispose();

    [Fact]
    public void TryLoad_NoFile_ReturnsNull()
    {
        Assert.Null(CreateCache().TryLoad());
    }

    [Fact]
    public void SaveThenLoad_ReturnsSameRatesAndTime()
    {
        var cache = CreateCache();

        Assert.True(cache.TrySave(DefaultExchangeRates.CreateProvider()));
        var loaded = cache.TryLoad();

        Assert.NotNull(loaded);
        Assert.Equal(Today, loaded.FetchedAt);
        Assert.Equal(743.94m, loaded.ToProvider().GetDkkPer100Units("EUR"));
        Assert.Equal(5.9740m, loaded.ToProvider().GetDkkPer100Units("JPY"));
    }

    [Fact]
    public void TrySave_CreatesMissingFolder()
    {
        CreateCache().TrySave(DefaultExchangeRates.CreateProvider());

        Assert.True(File.Exists(_file.FilePath));
    }

    [Theory]
    [InlineData("this is not json")]
    [InlineData("{}")]
    [InlineData("""{"FetchedAt":"2026-11-18T10:00:00+00:00","DkkPer100Units":{}}""")]
    [InlineData("""{"FetchedAt":"2026-11-18T10:00:00+00:00","DkkPer100Units":{"EUR":-1}}""")]
    public void TryLoad_BrokenFile_ReturnsNull(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file.FilePath)!);
        File.WriteAllText(_file.FilePath, content);

        Assert.Null(CreateCache().TryLoad());
    }

    [Fact]
    public void IsFromToday_SameDay_True_NextDay_False()
    {
        var cache = CreateCache();
        cache.TrySave(DefaultExchangeRates.CreateProvider());
        var loaded = cache.TryLoad()!;

        Assert.True(cache.IsFromToday(loaded));

        _clock.UtcNow = Today.AddDays(1);
        Assert.False(cache.IsFromToday(loaded));
    }

    private FileRateCache CreateCache() => new(_file.FilePath, _clock);
}

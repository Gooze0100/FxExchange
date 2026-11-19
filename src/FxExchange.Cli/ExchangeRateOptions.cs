using Microsoft.Extensions.Configuration;

namespace FxExchange.Cli;

/// <summary>
/// The "ExchangeRates" section of appsettings.json, which is the single source for these values.
/// <see cref="Load"/> validates them and turns them into <see cref="LiveRateSettings"/>.
/// </summary>
public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    public Uri? SourceUrl { get; set; }

    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Returns null, after a warning, when the settings are missing or invalid. --live then treats it like a
    /// failed download and falls back to cached or built-in rates, so a broken file never stops the tool.
    /// </summary>
    public static LiveRateSettings? Load(string directory, TextWriter warnings)
    {
        ExchangeRateOptions? options;
        try
        {
            options = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build()
                .GetSection(SectionName)
                .Get<ExchangeRateOptions>();
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or InvalidOperationException)
        {
            return Warn(warnings, $"could not read appsettings.json ({ex.Message}).");
        }

        if (options is null)
        {
            return Warn(warnings, $"the {SectionName} section is missing from appsettings.json.");
        }

        if (options.SourceUrl is not { IsAbsoluteUri: true } url || url.Scheme != Uri.UriSchemeHttps)
        {
            return Warn(warnings, $"{SectionName}:SourceUrl in appsettings.json is missing or not an https address.");
        }

        if (options.TimeoutSeconds is not int seconds || seconds is < 1 or > 60)
        {
            return Warn(warnings, $"{SectionName}:TimeoutSeconds in appsettings.json is missing or not between 1 and 60.");
        }

        return new LiveRateSettings(url, TimeSpan.FromSeconds(seconds));
    }

    private static LiveRateSettings? Warn(TextWriter warnings, string problem)
    {
        warnings.WriteLine($"Warning: {problem}");
        return null;
    }
}

public sealed record LiveRateSettings(Uri SourceUrl, TimeSpan Timeout);

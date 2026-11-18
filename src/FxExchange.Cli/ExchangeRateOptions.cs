using FxExchange.Core;
using Microsoft.Extensions.Configuration;

namespace FxExchange.Cli;

/// <summary>
/// Settings for --live, read from the "ExchangeRates" section of appsettings.json.
/// The defaults here are used when the file or a value is missing, so the tool still needs zero setup.
/// </summary>
public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";
    public const int DefaultTimeoutSeconds = 5;

    public Uri SourceUrl { get; set; } = NationalbankenRateSource.DefaultUri;

    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    /// <summary>
    /// A broken settings file or a bad value never stops the tool: it is reported as a warning
    /// and the default is used instead.
    /// </summary>
    public static ExchangeRateOptions Load(string directory, TextWriter warnings)
    {
        ExchangeRateOptions options;
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            options = configuration.GetSection(SectionName).Get<ExchangeRateOptions>() ?? new ExchangeRateOptions();
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or InvalidOperationException)
        {
            warnings.WriteLine($"Warning: could not read appsettings.json ({ex.Message}). Using default settings.");
            return new ExchangeRateOptions();
        }

        if (!options.SourceUrl.IsAbsoluteUri || (options.SourceUrl.Scheme != Uri.UriSchemeHttps && options.SourceUrl.Scheme != Uri.UriSchemeHttp))
        {
            warnings.WriteLine($"Warning: SourceUrl '{options.SourceUrl}' is not an http(s) address. Using the default.");
            options.SourceUrl = NationalbankenRateSource.DefaultUri;
        }

        if (options.TimeoutSeconds is < 1 or > 60)
        {
            warnings.WriteLine($"Warning: TimeoutSeconds must be between 1 and 60, but was {options.TimeoutSeconds}. Using {DefaultTimeoutSeconds}.");
            options.TimeoutSeconds = DefaultTimeoutSeconds;
        }

        return options;
    }
}

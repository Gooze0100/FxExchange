using System.Globalization;
using System.Xml.Linq;

namespace FxExchange.Core;

/// <summary>
/// Same source and format as the exercise's table (DKK per 100 units), free and without an API key.
/// </summary>
public static class NationalbankenRateSource
{
    public static readonly Uri DefaultUri = new("https://www.nationalbanken.dk/api/currencyratesxml?lang=en");

    public static async Task<InMemoryExchangeRateProvider> FetchAsync(
        HttpClient httpClient,
        Uri? uri = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        var xml = await httpClient.GetStringAsync(uri ?? DefaultUri, cancellationToken).ConfigureAwait(false);
        return Parse(xml);
    }

    /// <summary>
    /// Parses elements like &lt;currency code="EUR" desc="Euro" rate="746.13" /&gt;.
    /// Entries with a missing or unparsable rate are skipped instead of failing the whole file.
    /// </summary>
    public static InMemoryExchangeRateProvider Parse(string xml)
    {
        var document = XDocument.Parse(xml);
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "currency"))
        {
            var code = (string?)element.Attribute("code");
            var rateText = (string?)element.Attribute("rate");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(rateText))
            {
                continue;
            }

            // The Danish version of the feed uses a decimal comma, the English one a decimal point.
            var normalized = rateText.Trim().Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate)
                && rate > 0)
            {
                rates[code.Trim()] = rate;
            }
        }

        if (rates.Count == 0)
        {
            throw new ExchangeException("The Nationalbanken response did not contain any exchange rates.");
        }

        return new InMemoryExchangeRateProvider(rates);
    }
}

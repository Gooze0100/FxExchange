namespace FxExchange.Core;

/// <summary>
/// Used for built-in, downloaded and cached rates alike, so loading rates and looking them up stay separate.
/// </summary>
public sealed class InMemoryExchangeRateProvider : IExchangeRateProvider
{
    public const string BaseCurrency = "DKK";

    private readonly Dictionary<string, decimal> _dkkPer100Units;

    public InMemoryExchangeRateProvider(IEnumerable<KeyValuePair<string, decimal>> dkkPer100Units)
    {
        ArgumentNullException.ThrowIfNull(dkkPer100Units);

        _dkkPer100Units = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, rate) in dkkPer100Units)
        {
            if (rate <= 0)
            {
                throw new ArgumentException($"The rate for '{code}' must be positive, but was {rate}.", nameof(dkkPer100Units));
            }

            _dkkPer100Units[code.Trim().ToUpperInvariant()] = rate;
        }

        // The base currency is always known: 100 DKK costs 100 DKK.
        _dkkPer100Units[BaseCurrency] = 100m;

        SupportedCurrencies = _dkkPer100Units.Keys.OrderBy(code => code, StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyCollection<string> SupportedCurrencies { get; }

    public IReadOnlyDictionary<string, decimal> DkkPer100Units => _dkkPer100Units;

    public decimal? GetDkkPer100Units(string isoCode) =>
        _dkkPer100Units.TryGetValue(isoCode, out var rate) ? rate : null;
}

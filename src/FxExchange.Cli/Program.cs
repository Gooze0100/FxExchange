using System.Globalization;
using FxExchange.Cli;
using FxExchange.Core;

// Composition root: decide where rates come from, then hand over to the testable command.
var useLiveRates = args.Any(a => string.Equals(a, ExchangeCommand.LiveFlag, StringComparison.OrdinalIgnoreCase));
var positionalArgs = args
    .Where(a => !string.Equals(a, ExchangeCommand.LiveFlag, StringComparison.OrdinalIgnoreCase))
    .ToArray();

IExchangeRateProvider rates;
if (useLiveRates && positionalArgs.Length == 2)
{
    var options = ExchangeRateOptions.Load(AppContext.BaseDirectory, Console.Error);
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds) };
    var cache = new FileRateCache(FileRateCache.DefaultFilePath);
    rates = await RateProviderLoader.LoadLiveOrDefaultAsync(httpClient, cache, Console.Error, options.SourceUrl);
}
else
{
    rates = DefaultExchangeRates.CreateProvider();
}

return ExchangeCommand.Run(positionalArgs, rates, Console.Out, Console.Error, CultureInfo.CurrentCulture);

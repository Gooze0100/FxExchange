using System.Globalization;
using FxExchange.Core;

namespace FxExchange.Cli;

/// <summary>
/// The command-line behaviour, kept apart from Program.cs so it can be unit tested:
/// console streams, culture and rates are all passed in instead of being read from globals.
/// </summary>
public static class ExchangeCommand
{
    public const string LiveFlag = "--live";
    public const string Usage = "Usage: Exchange <currency pair> <amount to exchange> [--live]";

    public const int Success = 0;
    public const int InvalidArguments = 1;
    public const int ExchangeFailed = 2;

    // "0.####...": no thousands separators, no trailing zeros, culture-specific decimal separator.
    private static readonly string OutputFormat = "0." + new string('#', 28);

    public static int Run(
        IReadOnlyList<string> args,
        IExchangeRateProvider rates,
        TextWriter output,
        TextWriter error,
        CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(culture);

        if (args.Count != 2)
        {
            output.WriteLine(Usage);
            return InvalidArguments;
        }

        // Parse with the user's culture: "1,5" on a Danish machine, "1.5" on an English one.
        if (!decimal.TryParse(args[1], NumberStyles.Number, culture, out var amount))
        {
            error.WriteLine($"Invalid amount '{args[1]}'. Example: Exchange EUR/DKK {1.5m.ToString(culture)}");
            return InvalidArguments;
        }

        try
        {
            var pair = CurrencyPair.Parse(args[0]);
            var exchanged = new CurrencyExchanger(rates).Exchange(pair, amount);
            output.WriteLine(FormatAmount(exchanged, culture));
            return Success;
        }
        catch (ExchangeException ex)
        {
            error.WriteLine(ex.Message);
            return ExchangeFailed;
        }
    }

    public static string FormatAmount(decimal amount, CultureInfo culture) => amount.ToString(OutputFormat, culture);
}

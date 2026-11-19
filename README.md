# FX Exchange – command line tool

Exchanges an amount from one currency to another using an ISO currency pair (`MAIN/MONEY`).

```
> Exchange
Usage: Exchange <currency pair> <amount to exchange> [--live]

> Exchange EUR/DKK 1
7,4394

> Exchange EUR/USD 100
112,1895

> Exchange EUR/XYZ 1
Unknown currency 'XYZ'. Supported currencies: CHF, DKK, EUR, GBP, JPY, NOK, SEK, USD.
```

(Decimal separator follows the machine's regional settings, as in the exercise screenshot.)

## Build, test, run

Requires the .NET 10 SDK (the current LTS release). No database, no API keys, no setup.

```
git clone <repo-url>
cd FxExchange
dotnet build
dotnet test
dotnet run --project src/FxExchange.Cli -- EUR/DKK 1
```

The executable is called `Exchange` (`src/FxExchange.Cli/bin/Debug/net10.0/Exchange.exe`).

### Live rates (optional)

```
Exchange EUR/DKK 1 --live
```

`--live` uses today's official rates from Danmarks Nationalbank – the same source and format
(DKK per 100 units) as the table in the exercise. It picks the best rates available, so the tool always
answers during evaluation:

1. Rates already downloaded **today** are read from a local cache file – no network call.
2. Otherwise today's rates are downloaded (5 second timeout) and saved to the cache.
3. If Nationalbanken is unavailable, the **last cached rates** are used, with a warning showing their date.
4. If there is no cache either, the **built-in rates** are used, with a warning.

The cache is a small JSON file in the user's temp folder (`%TEMP%\FxExchange\rates-cache.json`). It is
created automatically; a missing, unreadable or corrupted file is ignored. No database, no setup.

Without `--live` the built-in rates are used, so results are deterministic and match the exercise.

### Configuration (optional)

`src/FxExchange.Cli/appsettings.json` (copied next to `Exchange.exe`) holds the settings for `--live`:

```json
{
  "ExchangeRates": {
    "SourceUrl": "https://www.nationalbanken.dk/api/currencyratesxml?lang=en",
    "TimeoutSeconds": 5
  }
}
```

The same values are defaults in code, so a missing file, a missing value or a broken file still works:
a broken or invalid value is reported as a warning and the default is used.

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

Requires the .NET 8 SDK (or newer). No database, no API keys, no setup.

```
git clone <repo-url>
cd FxExchange
dotnet build
dotnet test
dotnet run --project src/FxExchange.Cli -- EUR/DKK 1
```

The executable is called `Exchange` (`src/FxExchange.Cli/bin/Debug/net8.0/Exchange.exe`).

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

## Structure

```
src/FxExchange.Core     domain logic, no console or I/O dependencies
  CurrencyPair                    value object, parses and validates "EUR/DKK"
  IExchangeRateProvider           abstraction over where rates come from
  InMemoryExchangeRateProvider    rates held in a dictionary (DKK always = 100)
  DefaultExchangeRates            the rates from the exercise
  NationalbankenRateSource        downloads/parses the official XML feed
  FileRateCache                   remembers the last downloaded rates in a JSON file
  CurrencyExchanger               the calculation
  Exceptions                      ExchangeException + specific subtypes

src/FxExchange.Cli      thin console layer (assembly name: Exchange)
  Program                         composition root: chooses the rate source
  ExchangeCommand                 argument parsing, output, exit codes (testable)
  RateProviderLoader              live rates -> cache -> built-in fallback chain

tests/FxExchange.Tests  xUnit tests for every part above
```

## Design decisions

**Cross rates through DKK.** All rates are quoted against DKK, so any pair is
`amount × rate(MAIN) / rate(MONEY)`. The "per 100" cancels out. This needs only N rates instead of N²
pair rates, and adding a currency means adding one number.

**`decimal`, not `double`.** Money must not suffer binary floating point errors (`0.1 + 0.2 != 0.3` in
`double`). `decimal` is base-10 and has 28–29 significant digits.

**Multiply before dividing** to keep the most precision, then **round once at the end** to 4 decimals
(the precision of the source rates) with `MidpointRounding.AwayFromZero` – the "school" rounding most
users expect. .NET's default is banker's rounding (`ToEven`); which one is right is a business
decision, so it is explicit in the code.

**Same currency returns the amount unchanged** – not rounded, not multiplied by 1.

**Unknown currency beats same currency.** `XYZ/XYZ` is reported as unknown instead of echoing the
amount, because the user most likely mistyped.

**Validation split by responsibility.** `CurrencyPair` checks the *format* (three letters, slash).
The rate provider decides whether a currency is *known*. So when live rates add 30+ currencies,
`CurrencyPair` doesn't change.

**Negative amounts are rejected.** An exchange of a negative amount has no meaning here.

**Culture-aware input and output.** The amount is parsed and printed with the machine's culture
(`1,5` on Danish Windows, `1.5` on English). No thousands separators in the output, so the result is
easy to use in scripts. The core library never touches culture – it only works with `decimal`.

**Errors.** Expected problems (bad pair, unknown currency, bad amount) are `ExchangeException`s; the CLI
catches only those, prints a clear message to stderr and returns a non-zero exit code
(1 = bad arguments, 2 = exchange failed). Anything else is a bug and is not swallowed.

**Graceful degradation for live rates.** A banking tool should keep working when an external service
is down, but the user must know which rates were used. Hence the chain live → cached → built-in, with a
warning on stderr whenever the answer isn't based on today's rates.

**Persistence without setup.** The cache is a plain JSON file in the temp folder rather than a database,
as the exercise asks. It is written to a temporary file first and then moved into place, so a crash can't
leave a half-written cache. Cache failures (no write access, corrupted file) never stop an exchange – the
cache is an optimisation, not a dependency. Rates count as fresh for the same (UTC) day, because
Nationalbanken publishes once per banking day.

**Testability.** `ExchangeCommand.Run` gets the args, rate provider, output/error writers and culture as
parameters, so tests run without a real console or the machine's culture. The live source takes an
`HttpClient`, so tests use a fake `HttpMessageHandler` instead of the real network. The cache takes a
file path and a `TimeProvider` (.NET 8's injectable clock), so tests use their own temp folder and can
"move to tomorrow" to test stale-cache behaviour.

## What I would add in a real system

- Rates have a **timestamp/validity date** and stale rates are rejected or flagged.
- Retries with backoff for the download (e.g. Polly), and a shared cache (e.g. Redis) when many
  instances run as a service instead of one file per user.
- **Bid/ask spread** and fees – a bank doesn't exchange at the mid rate.
- Currency-specific **minor units** (JPY has 0 decimals, most others 2; ISO 4217).
- Dependency injection with the Generic Host, logging, configuration for the data source URL.
- A `Money` value type (amount + currency) so amounts in different currencies can't be mixed.
- Audit trail: which rate was used for each exchange.

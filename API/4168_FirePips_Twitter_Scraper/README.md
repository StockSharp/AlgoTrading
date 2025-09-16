# FirePips Twitter Scraper Strategy

## Overview
The **FirePips Twitter Scraper Strategy** is a faithful StockSharp port of the MetaTrader script `firepips_.mq4`. The original MQ4
file was not a trading robot but a WinInet-based utility that downloaded the public Twitter page of the FirePips signal service,
searched the HTML for occurrences of a specific order identifier, and then saved the raw response to disk for manual inspection.
This C# implementation mirrors the same data-acquisition workflow inside StockSharp so that the behaviour can be automated,
monitored through the platform's logging system, and combined with other infrastructure components if necessary.

Unlike typical trading strategies, this port does not subscribe to market data or place orders. Instead, it focuses on reliable
HTTP communication, deterministic text processing, and comprehensive telemetry that reproduces the diagnostics shown by the MQL
script's `Alert` and `Print` statements. The result is a self-contained strategy that can be started from Designer, Shell, or the
Runner to periodically snapshot the FirePips web page and verify that a target identifier (by default `"Order ID: 7"`) is present.

## Execution flow
1. **Startup** – `OnStarted` immediately schedules an asynchronous background task so the UI thread is not blocked during the
   HTTP request. Status messages are written with `AddInfoLog`, mirroring the `Alert` dialogs from MetaTrader.
2. **HTTP download** – the strategy builds an `HttpClient` with the user-defined timeout and downloads the configured URL. A
   non-success status code or a transport exception is captured and routed to `AddErrorLog`, matching the `Alert` failure branch
   in the original code.
3. **Content validation** – if the payload is empty, a warning is emitted and the workflow finishes early, reproducing the
   `Alert("Nicht nur ein paar daten")` message found in the MQ4 script.
4. **Search loop** – the downloaded string is scanned with the same iterative approach as `StringFind` in MQL: every occurrence of
   the search substring is located and the zero-based character index is logged. This replicates the `Print("order 7 found atz=",
   index)` output from the script.
5. **File persistence** – the response is written to the user-configurable file name using UTF-8 encoding, equivalent to
   `FileOpen("SavedFromInternet.htm", FILE_CSV|FILE_WRITE)` followed by `FileWrite` in the original implementation.
6. **Token extraction** – the strategy inspects the first token before the `;` delimiter, mimicking the CSV parsing branch that
   attempted to read an order type from the saved file. Successful integer conversions are logged; otherwise, descriptive messages
   explain why no numeric token was detected.
7. **Shutdown** – once the work is completed (or an error occurs), `Stop()` is invoked, which triggers `OnStopped` and writes a
   final completion message.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `RequestUrl` | `string` | `http://twitter.com/FirePips` | Remote address to download. Mirrors the hard-coded WinInet target in the MQL script. |
| `SearchText` | `string` | `Order ID: 7` | Substring searched within the HTML. Every match produces an informational log entry. |
| `OutputFileName` | `string` | `SavedFromInternet.htm` | Local file where the response is stored. Relative paths are resolved against the current working directory. |
| `RequestTimeout` | `int` | `30000` | HTTP timeout in milliseconds. Controls how long the strategy waits before aborting a slow request. |

All parameters are exposed through `StrategyParam<T>` so they can be tweaked from the StockSharp UI or optimised if the user wants
to evaluate different combinations (for example, multiple identifiers or alternative URLs).

## Mapping to the MQL script
- **WinInet vs. HttpClient** – `InternetOpenA`, `InternetOpenUrlA`, and `InternetReadFile` were replaced with `HttpClient` calls
  that provide the same functionality with automatic resource management (`using` blocks and `IDisposable`).
- **Loop control** – the MQ4 script polled `IsStopped()` inside a `while` loop. The StockSharp version relies on the framework's
  cooperative cancellation: once the download finishes, the strategy calls `Stop()` and no busy waiting is necessary.
- **String search** – the sequential `StringFind` loop is implemented by `FindOccurrences`, which preserves the exact matching
  semantics (case-sensitive and non-overlapping) while returning all offsets in one pass.
- **File handling** – instead of juggling multiple `FileOpen` modes to emulate CSV behaviour, `File.WriteAllText` writes the entire
  payload in one operation. The follow-up token extraction keeps the logic of attempting to parse the first integer value.
- **Alerts and diagnostics** – every branch that produced an `Alert` in the MQ4 script now generates a clearly labelled log entry.
  Errors are reported through `AddErrorLog`, warnings map to `AddWarningLog`, and standard progress updates use `AddInfoLog`.

## Usage tips
- Schedule the strategy with StockSharp Runner if you need recurrent downloads. The Runner can relaunch the strategy at fixed
  intervals while reusing the same parameter set.
- Adjust `RequestUrl` and `SearchText` to monitor other FirePips trade identifiers or even entirely different services. The logic
  is agnostic to the data source as long as the response is textual.
- When running in production, store `OutputFileName` inside a dedicated folder (for example, under `%APPDATA%/FirePips` or
  `/var/stocksharp/firepips`) to keep snapshots organised.
- Combine this scraper with a second StockSharp strategy that reads the saved file and reacts to new order IDs if you want to
  integrate the FirePips signals into an automated workflow.
- Use StockSharp's logging viewer to inspect the informational messages and verify that the search loop found the expected
  occurrences.

## Differences from the original implementation
- The port is asynchronous and non-blocking, whereas the MQ4 script performed a blocking loop with `Sleep(1)` delays.
- File encoding defaults to UTF-8, which avoids locale-dependent issues that could appear with the implicit ANSI encoding used by
  MetaTrader's `FileWrite`.
- Network errors produce detailed exception messages instead of generic "Error with InternetOpenUrlA()" strings, making diagnosis
  easier when proxies or firewalls interfere with the request.
- The strategy calls `Stop()` automatically after the download to signal completion. Users no longer need to close the script
  manually as they did in MetaTrader.

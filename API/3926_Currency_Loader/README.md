[Русский](README_ru.md) | [中文](README_cn.md)

# Currency Loader Strategy

The strategy ports the MetaTrader utility **Currency_Loader.mq4** into StockSharp.
It continuously monitors multiple candle timeframes, periodically exports their full history into CSV files,
and mirrors the logging behaviour of the original script.
No trading logic is executed – the component is intended only for data collection.

## How it works

- When the strategy starts it creates an `Export_History/<symbol>` directory using the current security identifier.
- Enabled timeframes are subscribed via the high-level candle API. Finished candles are cached up to `MaxBarsInFile` entries.
- A periodic timer (`FrequencyUpdateSeconds`) rewrites each requested CSV once enough history (`BarsMin`) is available.
- Each export overwrites the target file to keep a fresh snapshot, exactly as the MQL script did.
- Optional status messages are emitted through the platform log (`AllowInfo`) and, if enabled, through a side text log (`AllowLogFile`).

## Exported files

For each active timeframe a file `<symbol>_<TF>.csv` is produced inside the export directory. The
format matches the MQL version:

```
"Date" "Time" "Open" "High" "Low" "Close" "Volume"
2023.09.11,14:25,1.07230,1.07310,1.07180,1.07250,123
...
```

- Date is printed as `yyyy.MM.dd` and time as `HH:mm` (minute precision, identical to `TIME_MINUTES`).
- Prices are formatted using the detected price step of the security, so the decimal count matches the feed.
- Volume is rounded to the nearest integer, preserving the original script's behaviour.

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `BarsMin` | Minimum number of finished candles required before any export occurs. | 100 |
| `MaxBarsInFile` | Upper limit of candles saved per timeframe; older rows are discarded from the cache. | 20000 |
| `FrequencyUpdateSeconds` | Interval of the periodic timer that triggers file rewriting. | 60 |
| `LoadM1` / `LoadM5` / ... / `LoadMN` | Enable CSV generation for the respective timeframe (M1, M5, M15, M30, H1, H4, D1, W1, MN1). | `false` |
| `AllowInfo` | Write informational messages into the StockSharp log. | `true` |
| `AllowLogFile` | Append messages to `LOGCurrency_Loader_<date>.log` inside the export directory. | `true` |

## Differences from the MQL script

- The file hierarchy is identical, but symbol names are sanitized to remove invalid filesystem characters.
- The timer uses StockSharp's strategy timer instead of `Sleep()` loops, providing deterministic scheduling in backtests.
- Candle values are collected via `SubscribeCandles().Bind(...)`, avoiding manual `ArrayCopyRates` calls.
- Logging integrates with the StockSharp infrastructure while still offering the optional external log file.
- CSV exports include the same header, ordering, and numeric formatting as the original implementation.

## Usage tips

1. Assign the desired security and enable the target timeframes before launching the strategy.
2. Ensure the account has candle history so that `BarsMin` can be satisfied; otherwise exports are skipped.
3. Stop the strategy to force an immediate final export – the cache is cleared afterwards to release memory.
4. Monitor the `Export_History/<symbol>` folder for the generated CSV files and optional `LOGCurrency_Loader_<date>.log`.

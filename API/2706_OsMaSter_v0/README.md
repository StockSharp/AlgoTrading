# OsMaSter V0 Strategy

## Overview
- Converted from the MetaTrader 5 expert "OsMaSter v0" (MQL file `OsMaSter v0.mq5`).
- Uses a MACD histogram (OsMA) pattern to identify momentum reversals after a short consolidation.
- Designed to operate on a single instrument and timeframe selected by the user through the `CandleType` parameter.
- Automatically converts pip-based risk settings (stop-loss and take-profit) to absolute price offsets using the instrument's price step and decimal precision.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `FastPeriod` | 9 | Fast EMA length for the MACD histogram. |
| `SlowPeriod` | 26 | Slow EMA length for the MACD histogram. |
| `SignalPeriod` | 5 | Signal EMA length used to smooth the histogram. |
| `StopLossPips` | 30 | Distance to the protective stop in pips. Set to `0` to disable. |
| `TakeProfitPips` | 50 | Distance to the profit target in pips. Set to `0` to disable. |
| `TradeVolume` | 1 | Order volume (lots) used for market entries. |
| `CandleType` | 15-minute candles | Timeframe used for indicator calculations. |

## Signal Logic
1. The strategy keeps the latest four MACD histogram values (`hist0` = current, `hist1` = previous, ..., `hist3` = three candles ago).
2. **Long entry** when `hist3 > hist2`, `hist2 < hist1`, and `hist1 < hist0` &mdash; a rising sequence after a local minimum.
3. **Short entry** when `hist3 < hist2`, `hist2 > hist1`, and `hist1 > hist0` &mdash; a falling sequence after a local maximum.
4. Only one position can be open at a time. The strategy ignores new signals while a trade is active.

## Position Management
- Orders are sent with `BuyMarket()` or `SellMarket()` using the configured `TradeVolume`.
- `StartProtection` attaches stop-loss and take-profit offsets based on pip inputs. Pip size follows the forex convention (price step × 10 for 3/5 decimal instruments, otherwise the price step itself).
- There are no additional exit rules; positions are managed exclusively by the protective orders or by manual intervention.

## Notes
- Ensure the `Security` has correct `PriceStep` and `Decimals` values so that pip conversion matches the broker specification.
- Adjust candle timeframe and volume to match the trading horizon of the target market.
- Because the strategy waits for stop or target execution, consecutive signals in the same direction are skipped while a position remains open.

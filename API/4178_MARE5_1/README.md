# MARE5.1 Strategy

## Overview
The MARE5.1 strategy is a C# port of the MetaTrader 4 expert advisor `MARE5_1.mq4`. The original robot traded on M1 data and relied on a pair of simple moving averages evaluated at three different historical offsets to detect regime changes. This StockSharp implementation reproduces the behaviour with configurable parameters, attaches MetaTrader-style protective orders, and exposes a detailed trading window filter.

## Trading Logic
1. **Market data**
   - A single candle subscription defined by `CandleType` (default: 1 minute) feeds the calculations.
   - Every candle is processed only after it closes to avoid using half-formed bars.
2. **Indicators**
   - Two `SimpleMovingAverage` instances represent the fast (`FastPeriod`) and slow (`SlowPeriod`) components.
   - Both averages are shifted forward by `MovingAverageShift`, exactly like the `ma_shift` argument in the MQL `iMA` function.
   - Additional delayed copies of each average are obtained with shifts of `MovingAverageShift + 2` and `MovingAverageShift + 5` to mirror the original `iMA(..., shift=2/5)` calls.
3. **Signal detection**
   - The difference between the averages must exceed at least one price step (`Point` in MetaTrader terms). If the instrument has zero `PriceStep`, any positive difference suffices.
   - **Sell setup:**
     - The previous candle must be bearish (`Close < Open`).
     - The current shifted slow average is greater than the fast average.
     - Two and five candles back the fast average was still above the slow average, signalling a momentum flip.
   - **Buy setup:**
     - The previous candle must be bullish (`Close > Open`).
     - The current shifted fast average is greater than the slow average.
     - Two and five candles back the slow average was still leading, confirming a transition from bearish to bullish conditions.
   - Only one position may be open at a time, replicating the `OrdersTotal() < 1` guard from the EA.
4. **Time filter**
   - Trading is allowed only when the closing hour of the evaluated candle falls inside the `[TimeOpenHour, TimeCloseHour]` interval.
   - If the end hour is less than the start hour the window is treated as overnight (e.g., `22` to `5`).

## Risk Management
- `StartProtection` is configured with a stop-loss and take-profit distance converted from MetaTrader points into absolute price offsets using the instrument `PriceStep`.
- No trailing stop is implemented because the original code declared `TrailingStop` but never used it.
- Orders are submitted with the volume defined by `TradeVolume`. The strategy does not pyramid or scale out positions.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `TradeVolume` | Lot size for market entries. | `7.8` | Rounded according to exchange rules by the StockSharp connector. |
| `FastPeriod` | Period of the fast simple moving average. | `13` | Controls how quickly the strategy reacts to price changes. |
| `SlowPeriod` | Period of the slow simple moving average. | `55` | Provides the longer-term trend reference. |
| `MovingAverageShift` | Forward shift applied to both moving averages. | `2` | Matches the `ma_shift` parameter of the MQL `iMA` function. |
| `StopLossPoints` | Protective stop distance in MetaTrader points. | `80` | Converted to an absolute offset through the instrument `PriceStep`. |
| `TakeProfitPoints` | Profit target distance in MetaTrader points. | `110` | Set to `0` to disable the take-profit. |
| `TimeOpenHour` | Beginning of the allowed trading window (hour, 0–23). | `8` | Evaluated against the candle close time. |
| `TimeCloseHour` | End of the allowed trading window (hour, 0–23). | `14` | Can be lower than `TimeOpenHour` to span midnight. |
| `CandleType` | Timeframe used for candle subscription. | `1 minute` | Any other `TimeFrame()` value may be provided. |

## Implementation Notes
- The `Shift` indicator is used internally to reproduce the exact historical offsets of the MQL implementation without accessing indicator buffers directly.
- `IsDifferenceSatisfied` encapsulates the point-threshold comparison, keeping the strategy compatible with instruments that have varying tick sizes.
- The trading window check uses candle close times, which is the best approximation of `Hour()` from MetaTrader when only finished candles are processed.
- All comments are written in English and the code relies solely on the high-level API (`SubscribeCandles().Bind(...)`) as required by the project guidelines.

## Differences Compared to the MQL Version
- Signals are evaluated on closed candles, eliminating potential repainting that could occur on intra-bar ticks in MetaTrader.
- Stop-loss and take-profit orders are handled by `StartProtection` instead of being attached manually to every `OrderSend` call.
- The unused `TrailingStop` input was intentionally omitted to avoid exposing a non-functional parameter.
- The time filter supports overnight sessions by design, whereas the original EA implicitly assumed `TimeOpen <= TimeClose`.

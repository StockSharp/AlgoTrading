# Trend Line Strategy

## Summary
The Trend Line strategy replicates the core trade management logic of the original MetaTrader expert by combining a fast and slow linear weighted moving average, a momentum filter and a MACD confirmation. The conversion focuses on high-level StockSharp components and keeps the same systematic approach that waits for momentum bursts in the direction of the trend before entering. Protective stops, profit targets and an optional trailing stop in price steps provide risk management similar to the source expert.

## Trading Logic
1. Subscribe to the configured candle series and calculate the following indicators:
   - Fast linear weighted moving average (LWMA) with the configurable `FastMaPeriod`.
   - Slow LWMA with the configurable `SlowMaPeriod`.
   - Momentum indicator with period `MomentumPeriod`. The most recent three momentum readings are tracked to emulate the multi-bar momentum check present in the MQL version.
   - Moving Average Convergence Divergence (MACD) indicator with standard fast/slow/signal lengths. The strategy stores the MACD and signal values for later use.
2. Enter long when:
   - The fast LWMA is above the slow LWMA.
   - At least one of the last three momentum values is greater than or equal to `MomentumBuyThreshold`.
   - The MACD main line is above the MACD signal line.
   - No open short position exists (short exposure is flattened before opening a long position).
3. Enter short when:
   - The fast LWMA is below the slow LWMA.
   - At least one of the last three momentum values is less than or equal to `MomentumSellThreshold` (threshold should be negative to detect downward acceleration).
   - The MACD main line is below the MACD signal line.
   - No open long position exists (long exposure is flattened before opening a short position).
4. After each entry the strategy places protective stop-loss and take-profit orders by distance in price steps. Both orders are recalculated every time the position changes.
5. A trailing stop can be activated with `TrailingStopSteps` and `TrailingTriggerSteps`. Once the open position gains at least the trigger distance, the stop-loss is moved to `TrailingStopSteps` away from the current close price of the processed candle.

## Parameters
- `CandleType` – data type for the candle series used by every indicator (default 1-hour timeframe).
- `FastMaPeriod` – period of the fast LWMA (default 6).
- `SlowMaPeriod` – period of the slow LWMA (default 85).
- `MomentumPeriod` – number of candles for the momentum calculation (default 14).
- `MomentumBuyThreshold` – minimum positive momentum needed to allow new long positions (default 0.3).
- `MomentumSellThreshold` – maximum (negative) momentum allowed before opening new short positions (default -0.3).
- `MacdFastLength` – fast EMA length of the MACD (default 12).
- `MacdSlowLength` – slow EMA length of the MACD (default 26).
- `MacdSignalLength` – signal EMA length of the MACD (default 9).
- `StopLossSteps` – protective stop distance expressed in instrument steps (default 20).
- `TakeProfitSteps` – protective profit target distance in steps (default 50).
- `TrailingStopSteps` – trailing stop distance in steps (default 40, disabled when zero).
- `TrailingTriggerSteps` – profit in steps required before the trailing stop becomes active (default 40).

## Notes
- Indicator bindings rely on finished candles only; unfinished data is ignored to avoid premature signals.
- `SetStopLoss` and `SetTakeProfit` work with distances in price steps, which keeps the behaviour consistent on instruments with different tick sizes.
- When `MomentumSellThreshold` is kept positive, the default comparison (`<= threshold`) expects that value to be negative. Adjust the sign when optimising the strategy.
- The trailing stop works in end-of-bar mode because it is updated when each finished candle is processed, mirroring the original script that recalculated stops on new bars.
- The conversion intentionally omits manual trend-line drawing and equity-based liquidation rules because they relied on interactive terminal features unavailable in StockSharp. All other core entry and risk rules are preserved.

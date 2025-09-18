# Morning Pullback Corridor Strategy

## Overview
The **Morning Pullback Corridor Strategy** replicates the behaviour of the "3_Otkat_Sys_v1_2" MetaTrader 4 expert advisor. The system trades once per day during the early morning session, evaluating the interaction between the current price and the price corridor formed by candles that are 29 bars apart. It reacts to morning pullbacks after a strong overnight move and immediately attaches asymmetric take-profit levels for long and short positions.

## Trading Logic
1. **Session filter** – orders are considered only within the configured trade hour (default 05:00 platform time) and during the first few minutes of that hour. Mondays and Fridays are excluded in accordance with the original EA.
2. **Price corridor calculations** – for each completed candle the strategy keeps a rolling window of the most recent bars. It compares:
   - the open price 29 bars back with the previous candle close (`Open[29] - Close[1]`),
   - the previous candle close with the open price 29 bars back (`Close[1] - Open[29]`),
   - the distance from the previous close to the lowest low within the 29-bar range,
   - the distance from the highest high in the same range to the previous close.
3. **Entry rules** – if the overnight move exceeds the `CorridorOpenClosePoints` threshold and the latest pullback fits inside the configured `PullbackPoints ± CorridorPullbackPoints` envelope, a market position is opened at the beginning of the morning session:
   - Long entries require either a strong down move with a shallow pullback or an up move with an extended continuation above the corridor.
   - Short entries mirror the logic for bearish setups.
4. **Position management** – each trade receives:
   - a stop-loss at `StopLossPoints * PriceStep` from the entry price,
   - a take-profit at `TakeProfitPoints * PriceStep` for shorts and at `(TakeProfitPoints + LongTakeProfitExtraPoints) * PriceStep` for longs.
5. **Daily exit** – any position still open after the configured closing threshold (default after 22:45) is force-closed to avoid holding overnight.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPoints` | Base take-profit distance in instrument points, applied to short trades. Long trades add `LongTakeProfitExtraPoints`. |
| `StopLossPoints` | Protective stop distance in instrument points. |
| `PullbackPoints` | Desired pullback size around which the strategy evaluates retracements. |
| `CorridorOpenClosePoints` | Minimum distance between prices separated by 29 bars to confirm an overnight impulse. |
| `CorridorPullbackPoints` | Tolerance applied to the pullback threshold to create the entry corridor. |
| `LongTakeProfitExtraPoints` | Additional points added to the long take-profit target. |
| `TradeHour` | Hour (0–23) during which new entries are allowed. |
| `TradeMinuteLimit` | Maximum minute within the trade hour to accept new signals. |
| `CloseHour` | Hour when the strategy starts checking for time-based exits. |
| `CloseMinuteThreshold` | Minute inside `CloseHour` after which any open position is closed. |
| `CandleType` | Time frame used for candle subscriptions (default 1 minute). |

## Implementation Notes
- The strategy relies on `Security.PriceStep` to convert point-based inputs into absolute price distances. If the instrument does not provide a valid price step, the logic falls back to `1.0`.
- Stop-loss and take-profit levels are monitored on every completed candle; the strategy closes positions with market orders once the level is breached inside that candle range.
- The rolling window holds the latest 60 candles to cover the required 29-bar calculations and to mimic the `Lowest/Highest` helpers used in MetaTrader.
- Chart visualisation (candles and own trades) is available automatically when a chart area is created in the host application.

## Usage Tips
- Ensure the trading account volume (`Volume` property) is set before starting the strategy; the EA never scales position size dynamically.
- Keep the data feed aligned with the session time zone expected by the original expert advisor to maintain identical behaviour.
- Optimise the corridor parameters when applying the strategy to markets with different volatility profiles, because the point-based thresholds were tuned for the original instrument.

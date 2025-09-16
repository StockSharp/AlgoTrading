# AOCCI Strategy

## Overview
- Conversion of the MetaTrader 5 expert advisor `AOCCI` to the StockSharp high level API.
- Combines the Awesome Oscillator and Commodity Channel Index with a simple pivot level filter.
- Includes spread protection via "big jump" and "double jump" filters to skip unstable price action.
- Reproduces the original MQL5 logic where the short setup uses the same conditions as the long setup.

## Data and Indicators
- Uses the primary timeframe defined by `CandleType` for signal generation.
- Subscribes to an additional higher timeframe (`HigherCandleType`, default 1 hour) to read the previous close as a trend filter.
- Indicators:
  - `AwesomeOscillator` to detect momentum direction.
  - `CommodityChannelIndex` with configurable period and optional signal shift.
- Computes a pivot level from the candle located at `SignalCandleShift + 1` on the working timeframe: `(High + Low + Close) / 3`.

## Entry Logic
1. Wait until both indicators are fully formed and at least six finished candles are available.
2. Collect CCI values with the configured shift (`SignalCandleShift` for the current comparison and `SignalCandleShift + 1` for the previous bar).
3. Reject the bar when any jump filter triggers:
   - `BigJumpPips` compares consecutive open prices of the last five intervals.
   - `DoubleJumpPips` compares open prices separated by one bar.
4. Long entry when all conditions below are satisfied and there is no active position:
   - Awesome Oscillator is positive on the current bar.
   - Shifted CCI value is greater than or equal to zero.
   - Current close price is above the pivot level.
   - At least one confirmation is bearish on the previous data: prior AO value below zero, prior shifted CCI ≤ 0, or the last higher timeframe close below the pivot.
5. Short entry uses the exact same rule set as the long entry (the original expert contains identical conditions for both directions).

## Exit Logic and Risk Management
- When a trade is opened, optional stop-loss and take-profit levels are assigned using the configured pip distances multiplied by the detected pip size of the instrument.
- On every finished candle the strategy checks for take-profit or stop-loss hits using candle extremes and closes the position at market.
- Trailing stop activates when both `TrailingStopPips` and `TrailingStepPips` are positive:
  - Long trades move the stop to `Close - TrailingStopPips` once price advances by at least `TrailingStopPips + TrailingStepPips` from the entry.
  - Short trades move the stop to `Close + TrailingStopPips` once price falls by the same combined distance.
- If a position is closed (by stop, target, or trailing) the strategy waits until the next candle to evaluate new entries.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | 1 | Base order volume used for market entries. |
| `StopLossPips` | 50 | Distance in pips for the protective stop. Set to 0 to disable. |
| `TakeProfitPips` | 50 | Distance in pips for the take-profit. Set to 0 to disable. |
| `TrailingStopPips` | 5 | Trailing stop distance in pips. Requires `TrailingStepPips` > 0. |
| `TrailingStepPips` | 5 | Additional buffer before the trailing stop is updated. |
| `CciPeriod` | 55 | Period of the Commodity Channel Index. |
| `SignalCandleShift` | 0 | Shift applied when reading the CCI buffer and pivot candle. |
| `BigJumpPips` | 100 | Maximum allowed difference (in pips) between consecutive opens of the last candles. |
| `DoubleJumpPips` | 100 | Maximum allowed difference (in pips) between every second candle open. |
| `CandleType` | 15-minute candles | Working timeframe for the primary signals. |
| `HigherCandleType` | 1-hour candles | Higher timeframe used to fetch the previous close for confirmation. |

## Notes
- The pip size is derived from `Security.PriceStep` and adjusted for instruments quoted with 3 or 5 decimal digits.
- Because the original EA used identical filters for both directions, short trades will occur only if the long condition also passes and the strategy is allowed to sell. Disable short trades externally if not desired.
- Jump filters require at least six completed candles before the first trade is evaluated.

# Freeman Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Freeman is an intraday strategy that layers several momentum filters to scale into trends. It uses two RSI "teachers" driven by moving averages on the trading timeframe together with a higher timeframe moving-average filter. Risk is controlled with ATR-based stop-loss and take-profit targets plus a pip-based trailing stop.

## Strategy Overview

- Works on any timeframe candles selected by the `CandleType` parameter (15 minutes by default).
- Uses an hourly filter (`FilterCandleType`) to qualify trends before signals are accepted.
- Builds long and short signals from two RSI blocks that compare current and prior values in combination with moving-average slopes.
- Allows pyramiding when the market keeps moving, with the option to enlarge the next order after a losing exit.

## Trading Logic

### Long Conditions

1. Higher timeframe filter is optional. When enabled, the hourly moving average must slope upward.
2. RSI Teacher #1 is active when:
   - RSI #1 was below `RsiSellLevel` on the previous bar and turns higher on the current bar.
   - The fast moving average rises.
   - The hourly RSI (period 14) stays below `RsiBuyLevel` to confirm that the higher timeframe is not overbought.
3. RSI Teacher #2 is active when:
   - RSI #2 was below `RsiSellLevel2` and turns upward.
   - The slow moving average rises.
   - The hourly RSI stays below `RsiBuyLevel2`.
4. A long entry is taken when at least one teacher is active and the trend filter (if enabled) agrees.
5. Additional long entries require the closing price to move more than `DistancePips` (converted by the instrument's price step) away from the last long fill. When the last long exit was a loss, the volume is multiplied by `LockCoefficient` to imitate the MT5 locking behaviour.

### Short Conditions

Mirrors the long logic with inverted comparisons:

- The higher timeframe moving average must decline when the filter is enabled.
- RSI Teacher #1 needs RSI #1 above `RsiBuyLevel` turning lower, the fast MA falling, and the hourly RSI above `RsiSellLevel`.
- RSI Teacher #2 needs RSI #2 above `RsiBuyLevel2` turning lower, the slow MA falling, and the hourly RSI above `RsiSellLevel2`.
- Additional short entries follow the same distance and locking rules.

## Position Management

- Stop-loss and take-profit are recalculated for every entry from the current ATR value using `StopLossAtrFactor` and `TakeProfitAtrFactor`.
- The trailing stop activates once price travels beyond `TrailingStopPips + TrailingStepPips` and then locks in profits by keeping the stop `TrailingStopPips` away from the last close.
- Exits are executed with market orders once the candle's high/low breaches the calculated stop or target levels.
- The `PositionsMaximum` parameter limits the total number of filled entries (long plus short). A value of zero removes the cap.

## Time Filters

- Trading on Fridays can be disabled through `TradeOnFriday`.
- `StartHour` and `EndHour` define an optional session window in exchange time; zero values keep the market open all day.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Trading timeframe used for the main signal logic. |
| `FilterCandleType` | Higher timeframe for the moving-average and RSI filter (default 1 hour). |
| `FirstMaPeriod` / `SecondMaPeriod` | Periods for the fast and slow moving averages feeding the RSI teachers. |
| `FilterMaPeriod` | Length of the higher timeframe moving average. |
| `MaType` | Moving-average type (SMA, EMA, SMMA, or WMA). |
| `RsiFirstPeriod` / `RsiSecondPeriod` | Periods of the two RSI teachers. |
| `RsiSellLevel`, `RsiBuyLevel`, `RsiSellLevel2`, `RsiBuyLevel2` | RSI thresholds controlling the teacher blocks. |
| `UseRsiTeacher1`, `UseRsiTeacher2`, `UseTrendFilter` | Toggles for each component. |
| `StopLossAtrFactor`, `TakeProfitAtrFactor` | ATR multipliers for stop-loss and take-profit distances. |
| `TrailingStopPips`, `TrailingStepPips` | Pip offsets for the trailing stop engine. |
| `PositionsMaximum` | Maximum number of combined entries; zero = unlimited. |
| `DistancePips` | Minimum pip distance before adding to a position. |
| `TradeOnFriday` | Enable or disable signals on Fridays. |
| `StartHour`, `EndHour` | Optional trading session limits. |
| `LockCoefficient` | Volume multiplier used after a losing exit when stacking in the same direction. |
| `SignalShift` | Offset applied when reading indicator values (0 = current finished bar). |

## Implementation Notes

- The StockSharp port processes only finished candles, matching the MT5 "Bars Control" behaviour even when the original allowed tick-based trading.
- Price distances expressed in pips are converted using the instrument's `PriceStep`.
- Protective logic (stop, target, trailing) closes positions with market orders because high-level API bindings are used instead of individual MT5 position modifications.
- The strategy keeps aggregate long and short volumes; once a side is closed, loss tracking resets so the next signal behaves like the original locking rules.

Use appropriate risk controls and test thoroughly before deploying to live markets.

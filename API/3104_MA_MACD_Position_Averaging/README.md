# MA MACD Position Averaging

This strategy is a faithful conversion of the MetaTrader expert advisor **"MA MACD Position averaging"**. It combines a weighted moving average filter with a MACD ratio check and adds a martingale-style averaging module that increases the position size whenever price moves adversely by a configurable number of pips. All risk parameters are configured in pip units and internally converted to price offsets using the instrument metadata provided by StockSharp.

## Trading Logic

1. **Indicator preparation**
   - A configurable moving average (`MaPeriod`, `MaMethod`, `MaAppliedPrice`) is sampled on completed candles. The `SignalBar` and `MaShift` parameters emulate MetaTrader's ability to look back by a given number of bars and to plot the moving average with a horizontal offset.
   - A MACD indicator (`MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdAppliedPrice`) is processed on the same candles. The strategy stores the MACD main and signal lines in a small rolling buffer so that historical values can be accessed without calling indicator APIs directly.
2. **Entry conditions**
   - **Long**: both MACD lines are below zero, the ratio `MACDmain / MACDsignal` is greater than or equal to `MacdRatio`, the candle close is above the sampled moving average and the distance between price and the average is at least `IndentPips` pips.
   - **Short**: both MACD lines are above zero, the ratio is above `MacdRatio`, the candle close is below the moving average and the distance between them is at least `IndentPips` pips.
   - New entries are only allowed when the strategy has no exposure. When an averaging cycle is already in progress, the signal logic is skipped and only the averaging rules apply.
3. **Averaging module**
   - When a long position exists and price moves down by at least `StepLossingPips` from the best (lowest) long entry, the strategy opens an additional long trade whose volume equals the last leg volume multiplied by `LotCoefficient` (rounded by the instrument volume step).
   - When a short position exists and price moves up by at least `StepLossingPips` from the best (highest) short entry, a new short leg is added using the same `LotCoefficient` multiplier.
   - If exposure is detected in both directions (should never happen under normal conditions) the strategy immediately closes every leg to restore consistency.
4. **Protective exits**
   - Each leg stores individual stop-loss and take-profit levels expressed in price units (`StopLossPips`, `TakeProfitPips`). On every finished candle the strategy checks whether the candle range crossed any of the stored levels and, if so, exits the leg with a market order.
   - A trailing stop (`TrailingStopPips`, `TrailingStepPips`) is optional. Once price advances in favour of a leg by `TrailingStopPips + TrailingStepPips`, the stop is moved to `TrailingStopPips` pips behind the current close. The stop only tightens if price makes additional progress of at least `TrailingStepPips` pips.
5. **Housekeeping**
   - Volume commands are aligned to the instrument volume step and clipped to the allowed minimum/maximum. The strategy executes only on fully formed candles (`CandleStates.Finished`) to avoid double processing.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Timeframe used for indicator calculations. |
| `OrderVolume` | `decimal` | `0.1` | Base lot size for the initial entry. |
| `StopLossPips` | `int` | `50` | Stop-loss distance in pips (0 disables the stop). |
| `TakeProfitPips` | `int` | `50` | Take-profit distance in pips (0 disables the target). |
| `TrailingStopPips` | `int` | `5` | Trailing stop offset in pips. Must be positive to enable trailing. |
| `TrailingStepPips` | `int` | `5` | Extra pip distance required before the trailing stop moves again. |
| `StepLossingPips` | `int` | `30` | Price setback in pips that triggers a new averaging leg. |
| `LotCoefficient` | `decimal` | `2.0` | Multiplier applied to the previous leg volume when averaging. |
| `SignalBar` | `int` | `0` | Number of completed bars to look back when sampling indicators. |
| `MaPeriod` | `int` | `15` | Moving average length in bars. |
| `MaShift` | `int` | `0` | Horizontal shift (in bars) applied to the moving average values. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | Moving average smoothing algorithm (simple, exponential, smoothed, weighted). |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Candle price used as input for the moving average. |
| `IndentPips` | `int` | `4` | Minimum pip gap required between price and the moving average before entering. |
| `MacdFastPeriod` | `int` | `12` | Fast EMA length of the MACD filter. |
| `MacdSlowPeriod` | `int` | `26` | Slow EMA length of the MACD filter. |
| `MacdSignalPeriod` | `int` | `9` | Signal line length of the MACD filter. |
| `MacdAppliedPrice` | `AppliedPriceType` | `Weighted` | Applied price used for the MACD calculation. |
| `MacdRatio` | `decimal` | `0.9` | Minimum MACD main/signal ratio required to allow trading. |

### Pip conversion

All pip-based settings (`StopLossPips`, `TakeProfitPips`, `TrailingStopPips`, `TrailingStepPips`, `StepLossingPips`, `IndentPips`) are multiplied by the security `PriceStep`. When the instrument has 3 or 5 decimal places the value is further multiplied by 10 to reproduce MetaTrader's "pip" definition for fractional quotes. If no price step is available a fallback of `0.0001` is used.

## Implementation Notes

- The strategy keeps an internal list of position legs because StockSharp operates in netting mode. Each leg tracks its own entry price, stop and take levels so that averaging behaves like the original MetaTrader EA.
- Protective orders are simulated in software: when a candle touches a stop-loss or take-profit level the position is closed with a market order on that bar.
- Averaging is disabled automatically when `StepLossingPips` is zero. Otherwise, every additional leg uses the previous leg volume multiplied by `LotCoefficient` and rounded down to the nearest volume step.
- Trailing stop updates use the candle close as the current price proxy. The stop never moves in the adverse direction and remains inactive until the price progress exceeds `TrailingStopPips + TrailingStepPips`.
- Indicator buffers honour the `SignalBar` and `MaShift` offsets so the decision logic sees exactly the same values that the MetaTrader expert would obtain from its indicator buffers.

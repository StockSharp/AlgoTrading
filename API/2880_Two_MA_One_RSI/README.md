# Two MA One RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader 5 expert "Two MA one RSI" into StockSharp. It combines a fast and slow moving average crossover with an RSI confirmation that is evaluated on the previous closed candle. Flexible switches allow turning each comparison into either a "greater than" or "less than" rule so the setup can be inverted without touching the code.

## Details
- **Entry Criteria**:
  - Long signals require the fast MA to be below the slow MA two bars ago, the fast MA to be above the slow MA on the most recent closed bar, and the RSI from the previous bar to be above the upper threshold. Each comparison can be flipped through boolean parameters.
  - Short signals mirror the logic and check for the opposite MA relationships together with the RSI falling below the lower threshold.
  - Both MAs use the same averaging type; the slow period is always `FastMaPeriod * SlowPeriodMultiplier`. Optional horizontal shifts reproduce the MT5 behaviour where indicator values are read several candles back.
- **Long/Short**: The strategy can open positions in both directions. `CloseOppositePositions` controls whether a new signal forces the opposite side to close before entering.
- **Exit Criteria**:
  - Configurable stop-loss and take-profit in pips.
  - Optional trailing stop that only moves after price advances by at least `TrailingStopPips + TrailingStepPips` beyond the entry.
  - `ProfitClose` monitors floating P&L (using the instrument step price) and closes all positions once the target currency amount is reached.
- **Stops**: When `StopLossPips` is zero the strategy relies purely on the trailing-stop module (if enabled). `TrailingStopPips` requires a positive `TrailingStepPips`, matching the original expert's validation.
- **Default Values**:
  - `FastMaPeriod = 10`, `SlowPeriodMultiplier = 2`.
  - `FastMaShift = 3`, `SlowMaShift = 0`.
  - `RsiPeriod = 10`, `RsiUpperLevel = 70`, `RsiLowerLevel = 30`.
  - `StopLossPips = 50`, `TakeProfitPips = 150`, `TrailingStopPips = 15`, `TrailingStepPips = 5`.
  - `MaxPositions = 10`, `ProfitClose = 100`, `TradeVolume = 1`.
- **Filters**: Six boolean switches (`BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper`, `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower`) let the user instantly change the sense of each comparison.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Time-frame (or any other candle type) used for analysis. |
| `MaType` | Moving-average family (simple, exponential, smoothed, weighted, volume-weighted). |
| `FastMaPeriod` | Period of the fast MA. |
| `SlowPeriodMultiplier` | Slow MA period multiplier (`slow = fast * multiplier`). |
| `FastMaShift`, `SlowMaShift` | Horizontal shifts in candles applied when evaluating MA values. |
| `RsiPeriod` | RSI length (uses the previous finished candle). |
| `RsiUpperLevel`, `RsiLowerLevel` | RSI thresholds for long and short confirmations. |
| `BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper` | Toggle comparisons for long signals. |
| `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower` | Toggle comparisons for short signals. |
| `StopLossPips`, `TakeProfitPips` | Protective stop and target measured in pips (pip size derived from the security's price step). |
| `TrailingStopPips`, `TrailingStepPips` | Trailing-stop distance and minimal improvement. |
| `MaxPositions` | Maximum number of simultaneous entries per direction (`0` = unlimited). |
| `ProfitClose` | Currency profit target that closes all positions when reached. |
| `CloseOppositePositions` | Whether to flatten the opposite side before opening a new trade. |
| `TradeVolume` | Base order size; also synchronises with the strategy `Volume` property. |

## Implementation Notes
- All decisions use finished candles only, matching the MT5 expert's "new bar" logic.
- The pip size equals the instrument price step. If your market uses fractional pip pricing, adjust the security settings accordingly so the pip translation matches the original expert's `digits_adjust` logic.
- Trailing stops only start after the price has advanced by `TrailingStopPips + TrailingStepPips`; the stop is then anchored `TrailingStopPips` away from the close and only moves when it improves by at least `TrailingStepPips`.
- `ProfitClose` calculates floating profit using the security's `PriceStep` and `StepPrice`. Ensure those fields are configured for correct currency results.

# MA Trend 2 Strategy

## Summary
- Converted from MetaTrader 5 expert advisor `MA Trend 2.mq5`.
- Uses a configurable moving average to detect whether price trades above or below the shifted average.
- Positions are managed with optional stop-loss, take-profit, trailing stop, and money management features.

## Strategy Logic
1. Subscribe to the user-selected candle series and calculate the moving average with the chosen method, period, shift, and price source.
2. On each finished candle store the latest moving average value so a shifted sample (previous bar plus `MaShift`) can be compared against the current close price.
3. Generate buy signals when price crosses above the reference average and the direction filter allows long trades. Generate sell signals for the opposite condition. When `ReverseSignals` is enabled these rules are inverted.
4. Before entering a trade check the `OnlyOnePosition` and `CloseOppositePositions` flags. The strategy can either skip entries when the opposite exposure exists or close it in the same order to flip the position.
5. Position sizing uses either a fixed volume or a percent risk model derived from the original EA. The percent mode estimates the required volume so the loss at the configured stop distance matches the risk budget.
6. A trailing stop replicates the original step logic: once profit exceeds `TrailingStopPips + TrailingStepPips` it moves the stop in steps while never loosening it. If price crosses the trailing stop the position is closed at market.
7. Optional stop-loss and take-profit protections are attached through the high-level `StartProtection` helper so the broker model can exit positions between candle updates.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `StopLossPips` | Stop-loss distance in pips. Set to `0` to disable. | `50` |
| `TakeProfitPips` | Take-profit distance in pips. Set to `0` to disable. | `140` |
| `TrailingStopPips` | Base distance for the trailing stop in pips. | `15` |
| `TrailingStepPips` | Minimum additional profit before the trailing stop is tightened. | `5` |
| `LotMode` | `FixedVolume` uses `LotOrRiskValue` directly. `RiskPercent` interprets it as account risk percent. | `RiskPercent` |
| `LotOrRiskValue` | Fixed order size or risk percent depending on `LotMode`. | `3` |
| `MaPeriod` | Moving average period. | `12` |
| `MaShift` | Number of completed candles between the current bar and the moving average sample used for signals. | `3` |
| `MaMethod` | Moving average method (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `MaPrice` | Candle price used by the moving average (close, open, weighted, etc.). | `Weighted` |
| `CandleType` | Candle data type subscribed by the strategy. | `1 minute time frame` |
| `Direction` | Allowed direction (`BuyOnly`, `SellOnly`, `Both`). | `Both` |
| `OnlyOnePosition` | Allow only a single open position. | `false` |
| `ReverseSignals` | Invert buy/sell logic. | `false` |
| `CloseOppositePositions` | Close opposite exposure before opening a new trade. | `false` |

## Money Management
- When `LotMode = RiskPercent`, the strategy converts the stop-loss distance (in pips) into price units using security metadata (`PriceStep`, `StepPrice`).
- Risk is calculated from the portfolio value (`CurrentValue` with a fallback to `BeginValue`).
- The requested volume is rounded up to the nearest `VolumeStep` to avoid exchange rejections.

## Trailing Stop
- Trailing distance and step are expressed in pips; the code derives the actual price distance using the instrument pip size.
- Long positions move the stop up once the close exceeds the entry by at least `TrailingStopPips + TrailingStepPips`. The stop remains fixed if profit retracts.
- Short positions mirror the same logic with symmetrical price checks.

## Conversion Notes
- All trading actions use the high-level `Strategy` API (`BuyMarket`, `SellMarket`, `StartProtection`).
- The strategy keeps only a short moving average history (shift + buffer) to replicate the previous-bar reference without storing large datasets.
- Comments are provided in English to document each major block of logic.

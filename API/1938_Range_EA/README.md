# Range EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading price deviations from a moving average within a fixed range. Opens long or short positions when the price moves a specified distance away from the average. Supports optional trailing stop, step-down averaging, reversal module, and trading session filter.

## Details

- **Entry Criteria**:
  - Long: close price above moving average + `Range`
  - Short: close price below moving average - `Range`
- **Long/Short**: Both
- **Exit Criteria**:
  - Reach `TakeProfit` or `StopLoss`
  - Trailing stop hits when enabled
  - Optional reversal after move of `Turn`
- **Stops**: Fixed value
- **Default Values**:
  - `MaLength` = 21
  - `Range` = 250m
  - `TakeProfit` = 500m
  - `StopLoss` = 250m
  - `UseTrailingStop` = true
  - `TrailingStop` = 250m
  - `UseTurn` = true
  - `Turn` = 250m
  - `LotMultiplicator` = 1.65m
  - `TurnTakeProfit` = 500m
  - `UseStepDown` = false
  - `StepDown` = 150m
  - `UseTradeTime` = false
  - `OpenTradeTime` = 08:00:00
  - `CloseTradeTime` = 21:30:00
  - `OrderVolume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Range
  - Direction: Both
  - Indicators: MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Big Runner Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Big Runner strategy trades when the closing price and a fast SMA both cross in the direction of a slower SMA, indicating strong momentum. Position size is derived from a percentage of portfolio value multiplied by leverage. Optional stop-loss and take-profit levels manage risk.

## Details

- **Entry Criteria**:
  - Buy when close crosses above the fast SMA and the fast SMA crosses above the slow SMA.
  - Sell when close crosses below the fast SMA and the fast SMA crosses below the slow SMA.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - Optional stop loss and take profit based on entry price.
  - Opposite signal closes existing position.
- **Stops**: Configurable stop loss and take profit percentages.
- **Default Values**:
  - `FastLength` = 5
  - `SlowLength` = 20
  - `TakeProfitLongPercent` = 4
  - `TakeProfitShortPercent` = 7
  - `StopLossLongPercent` = 2
  - `StopLossShortPercent` = 2
  - `PercentOfPortfolio` = 10
  - `Leverage` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

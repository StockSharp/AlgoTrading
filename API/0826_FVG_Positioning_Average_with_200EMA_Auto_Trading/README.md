# FVG Positioning Average with 200EMA Auto Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy averages the levels of bullish and bearish fair value gaps (FVG) and combines them with a 200-period EMA. A trade is opened when price crosses these averages in the direction of the trend.

## Details

- **Entry Criteria**:
  - **Long**: Price crosses above the average of bearish FVGs and all averages are above the EMA.
  - **Short**: Price crosses below the average of bullish FVGs and all averages are below the EMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop-loss at recent low/high.
  - Take profit at risk-reward ratio.
- **Stops**: Yes.
- **Default Values**:
  - `FvgLookback` = 30
  - `AtrMultiplier` = 0.25
  - `LookbackPeriod` = 20
  - `EmaPeriod` = 200
  - `RiskReward` = 1.5
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: ATR, EMA, SMA, Highest, Lowest
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

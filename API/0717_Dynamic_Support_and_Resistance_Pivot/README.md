# Dynamic Support and Resistance Pivot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy derives dynamic support and resistance levels from recent pivot highs and lows. It enters long when price crosses above support near the level and enters short when price crosses below resistance. Risk management uses fixed percentage stop-loss and take-profit levels.

## Details

- **Entry Criteria**: Price near support/resistance within `SupportResistanceDistance` percent and cross above support or below resistance.
- **Long/Short**: Both.
- **Exit Criteria**: Fixed take-profit and stop-loss.
- **Stops**: Yes.
- **Default Values**:
  - `PivotLength` = 2
  - `SupportResistanceDistance` = 0.4m
  - `StopLossPercent` = 10.0m
  - `TakeProfitPercent` = 26.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Pivot
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

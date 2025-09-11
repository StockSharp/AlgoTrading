# John Bob Trading Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy combining 50-bar high/low levels with simple fair value gap detection. Opens five scaled orders with ATR-based stop-loss and multiple take-profit levels.

## Details

- **Entry Criteria**:
  - Long: price crosses above 50-bar low or bullish fair value gap appears
  - Short: price crosses below 50-bar high or bearish fair value gap appears
- **Long/Short**: Both
- **Exit Criteria**:
  - Price reaches one of five take-profit levels
  - Price hits ATR-based stop-loss
- **Stops**: ATR multiplier
- **Default Values**:
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, Highest, Lowest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

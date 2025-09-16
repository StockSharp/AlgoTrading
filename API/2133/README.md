# Dots Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from MQL5 "Exp_Dots". The strategy trades reversals when the Dots indicator changes color.
It goes long when the indicator switches from blue to red and short when it switches from red to blue.

## Details

- **Entry Criteria**:
  - Long: Indicator color changes from blue to red.
  - Short: Indicator color changes from red to blue.
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `Length` = 10
  - `Filter` = 0m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend reversal
  - Direction: Both
  - Indicators: Dots (NonLag Moving Average)
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: 4H
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

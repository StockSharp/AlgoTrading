# Covid Statistics Tracker Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading on the growth ratio of confirmed COVID-19 cases.
The strategy sells when case growth accelerates and buys when growth slows.

## Details

- **Entry Criteria**:
  - Long: `growth < 1`
  - Short: `growth > 1`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `Region` = "US"
  - `Lookback` = 2
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()
- **Filters**:
  - Category: Other
  - Direction: Both
  - Indicators: Custom
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

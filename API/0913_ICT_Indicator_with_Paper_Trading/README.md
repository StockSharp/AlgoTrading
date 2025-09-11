# ICT Indicator with Paper Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy stores order block highs and lows and goes long when the close crosses above the latest order block high. The long position is closed when the stored order block low crosses above price.

## Details

- **Entry Criteria**:
  - **Long**: close price crosses above latest order block high.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Exit long when order block low crosses above price.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Price action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

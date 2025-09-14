# Bollinger Bands Automated
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that places buy limit orders at the lower Bollinger Band and sell limit orders at the upper band. Positions close when the price touches the middle band. Pending orders are refreshed at the start of each candle.

## Details

- **Entry Criteria**:
  - Long: buy limit at lower Bollinger Band
  - Short: sell limit at upper Bollinger Band
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: price crosses above middle Bollinger Band
  - Short: price crosses below middle Bollinger Band
- **Stops**: None
- **Default Values**:
  - `BbPeriod` = 20
  - `BbDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

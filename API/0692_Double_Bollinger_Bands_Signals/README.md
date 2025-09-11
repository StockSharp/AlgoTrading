# Double Bollinger Bands Signals Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses two sets of Bollinger Bands. It buys when price crosses above the lower 3 standard deviation band and sells when price crosses below the upper 3 standard deviation band. Positions are closed on opposite 2 standard deviation bands.

## Details

- **Entry Criteria**:
  - Long: close crosses above lower 3 SD band
  - Short: close crosses below upper 3 SD band
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: close crosses above upper 2 SD band
  - Short: close crosses below lower 2 SD band
- **Stops**: None
- **Default Values**:
  - `Length` = 20
  - `Width1` = 2m
  - `Width2` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

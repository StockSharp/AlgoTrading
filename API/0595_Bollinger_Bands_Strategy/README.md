# Bollinger Bands Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading Bollinger Bands breakouts. Buys when the price closes above the upper band and sells when it closes below the lower band. Exits on a simple moving average cross or when a stop loss is hit.

## Details

- **Entry Criteria**:
  - Long: close above upper Bollinger Band
  - Short: close below lower Bollinger Band
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: close below SMA or price hits stop loss
  - Short: close above SMA or price hits stop loss
- **Stops**: Percent from entry price
- **Default Values**:
  - `BbLength` = 120
  - `BbDeviation` = 2m
  - `SmaLength` = 110
  - `StopLossPercent` = 6m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Bollinger Bands, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

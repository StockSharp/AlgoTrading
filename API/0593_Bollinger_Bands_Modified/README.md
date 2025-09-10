# Bollinger Bands Modified Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades Bollinger Bands breakouts with an optional EMA trend filter. Enters long when price crosses above the upper band and short when it crosses below the lower band.

Stop-loss is placed at the recent high or low and take-profit is a multiple of the risk.

## Details

- **Entry Criteria**:
  - Long: price crosses above upper Bollinger Band
  - Short: price crosses below lower Bollinger Band
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: stop at recent low, target at risk * factor
  - Short: stop at recent high, target at risk * factor
- **Stops**: Highest/lowest of last N candles
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 0.38m
  - `EmaLength` = 80
  - `HighestLength` = 7
  - `LowestLength` = 7
  - `TargetFactor` = 1.6m
  - `EmaTrend` = true
  - `CrossoverCheck` = false
  - `CrossunderCheck` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger Bands, EMA, Highest, Lowest
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

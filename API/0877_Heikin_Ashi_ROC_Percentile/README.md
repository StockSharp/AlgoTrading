# Heikin Ashi ROC Percentile Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy converts candles to Heikin Ashi, smooths the close with an SMA and measures its Rate of Change. Percentile bands of recent ROC highs and lows form breakout levels. A cross above the lower band opens or reverses long, while crossing below the upper band flips short.

## Details

- **Entry Criteria**:
  - Long: ROC crosses above the lower percentile line.
  - Short: ROC crosses below the upper percentile line.
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Percent stop.
- **Default Values**:
  - `RocLength` = 100
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `StartDate` = new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Heikin Ashi, RateOfChange, Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

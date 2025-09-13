# Xbug Free V4 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens positions when a moving average of the median price crosses the median price itself. A symmetric take profit and stop loss are placed at a fixed distance from the entry price.

## Details

- **Entry Criteria**:
  - Long: the moving average is above the median price and was below it two candles ago
  - Short: the moving average is below the median price and was above it two candles ago
- **Long/Short**: Both
- **Exit Criteria**:
  - Take profit at `StopPoints` distance above/below entry
  - Stop loss at `StopPoints` distance opposite side
- **Stops**: Yes
- **Default Values**:
  - `MaPeriod` = 19
  - `StopPoints` = 270
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Crossover
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Long-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

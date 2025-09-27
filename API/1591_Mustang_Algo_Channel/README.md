# Mustang Algo Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using an RSI-based global sentiment oscillator smoothed with WMA to trade channel crossovers.

## Details

- **Entry Criteria**: RSI/WMA oscillator crossovers with bounds.
- **Long/Short**: Configurable.
- **Exit Criteria**: Opposite signal or stop/take.
- **Stops**: Percent-based optional.
- **Default Values**:
  - `RsiPeriod` = 14
  - `Smoothing` = 20
  - `MedianPeriod` = 25
  - `UpperBound` = 55
  - `LowerBound` = 48
  - `TradeMode` = Long & Short
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `StopLossPercent` = 4
  - `TakeProfitPercent` = 12
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: RSI, WMA
  - Stops: Percent
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

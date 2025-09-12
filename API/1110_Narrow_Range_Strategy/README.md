# Narrow Range Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts after an inside bar where the latest candle range is narrower than a reference bar `Length` periods ago. Stop orders are placed at the reference high and low with a take profit equal to the reference range and a stop loss set as a percentage of that range.

## Details

- **Entry Criteria**:
  - Long: price breaks above the reference high after a narrow range bar
  - Short: price breaks below the reference low after a narrow range bar
- **Long/Short**: Both
- **Exit Criteria**:
  - Take profit at reference range
  - Stop loss at percentage of range
- **Stops**: Yes
- **Default Values**:
  - `Length` = 4
  - `StopLossPercent` = 0.35m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

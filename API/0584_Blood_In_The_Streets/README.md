# Blood In The Streets Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy buys when the current drawdown from the recent highest high falls below a standard deviation threshold. The position is closed after a fixed number of bars.

## Details

- **Entry Criteria**:
  - Long: drawdown ≤ mean + `StdDevThreshold` × standard deviation
- **Long/Short**: Long only
- **Exit Criteria**: position closed after `ExitBars` bars
- **Stops**: None
- **Default Values**:
  - `LookbackPeriod` = 50
  - `StdDevLength` = 50
  - `StdDevThreshold` = -1m
  - `ExitBars` = 35
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: Highest, SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

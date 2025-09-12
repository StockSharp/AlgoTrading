# TTP Intelligent Accumulator
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that accumulates long positions when RSI falls below its mean by one standard deviation and distributes them when RSI rises above the same threshold.

## Details

- **Entry Criteria**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **Long/Short**: Long only
- **Exit Criteria**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) and profit above `MinProfit`
- **Stops**: No
- **Default Values**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: RSI, MA, StdDev
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

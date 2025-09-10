# Backtesting Module
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the default behaviour of the TradingView "Backtesting Module". It trades a simple moving average crossover: a long position is opened when the 50-period SMA crosses above the 200-period SMA, and a short position is opened when the opposite crossover occurs. Trading is allowed only between the specified start and end times.

## Details

- **Entry Criteria**: 50-period SMA crossing 200-period SMA.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite crossover or leaving the time interval.
- **Stops**: None.
- **Default Values**:
  - `FastLength` = 50
  - `SlowLength` = 200
  - `StartTime` = 1 Jan 1980
  - `EndTime` = 31 Dec 2050
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Variable
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

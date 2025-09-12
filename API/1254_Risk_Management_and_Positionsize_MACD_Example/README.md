# Risk Management and Positionsize - MACD example
[Русский](README_ru.md) | [中文](README_cn.md)

The **Risk Management and Positionsize - MACD example** strategy demonstrates dynamic position sizing based on current equity. It relies on MACD crossovers from a higher timeframe combined with a moving average trend filter.

## Details
- **Entry Criteria**: MACD line crosses above/below signal line with trend confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite MACD crossover.
- **Stops**: None.
- **Default Values**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD, Moving Average
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Chande Kroll Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that uses Chande Kroll stop with an SMA trend filter. A long position is opened when the close crosses above the lower stop and is above the SMA. The position is closed when the close falls below the upper stop. Position size is based on the lowest close over 1560 bars and the risk multiplier.

## Details

- **Entry Criteria**:
  - Long: `previous close <= previous low stop && Close > low stop && Close > SMA`
- **Long/Short**: Long only
- **Exit Criteria**:
  - Long: `Close < high stop`
- **Stops**: Chande Kroll stop (Donchian extremes ± ATR)
- **Default Values**:
  - `CalcMode` = CalcMode.Exponential
  - `RiskMultiplier` = 5m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `StopLength` = 21
  - `SmaLength` = 21
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ATR, Donchian, SMA, Lowest
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Setup: Smooth Gaussian + Adaptive Supertrend (Manual Vol)
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long when the close is above a double-smoothed moving average ("Gaussian" trend).
Exits when price closes below the trend line. A simple manual volatility filter can restrict entries.

## Details

- **Entry Criteria**: Close above trend line and (volatility filter disabled or volatility is 2 or 3).
- **Long/Short**: Long only.
- **Exit Criteria**: Close below trend line.
- **Stops**: None.
- **Default Values**:
  - `TrendLength` = 75
  - `Volatility` = 2
  - `EnableVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

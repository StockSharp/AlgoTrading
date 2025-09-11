# Zero-lag Volatility-Breakout EMA Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout system using zero-lag EMA difference with Bollinger bands and an EMA trend filter. Optionally holds positions until an opposite signal.

## Details

- **Entry Criteria**: Dif crosses above upper band with EMA slope filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Optional exit on mid-band cross.
- **Stops**: No explicit stops.
- **Default Values**:
  - `EmaLength` = 200
  - `StdMultiplier` = 2m
  - `UseBinary` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, Bollinger Bands
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

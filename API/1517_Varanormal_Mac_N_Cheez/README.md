# Varanormal Mac N Cheez Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

SMA crossover strategy with trailing stop and daily profit target.

## Details

- **Entry Criteria**:
  - **Long**: Fast SMA crosses above slow SMA.
  - **Short**: Fast SMA crosses below slow SMA.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Trailing stop or fixed stop loss.
  - Daily profit target closes all positions.
- **Stops**: Yes, fixed and trailing.
- **Default Values**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `DailyTarget` = 200
  - `StopLossAmount` = 100
  - `TrailOffset` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

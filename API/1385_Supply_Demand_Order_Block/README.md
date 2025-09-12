# Supply Demand Order Block
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy using Donchian support and resistance levels with EMA trend filter and volume spike confirmation. Positions are protected by stop loss and trailing stop.

## Details

- **Entry Criteria**: Breakout of Donchian channel with trend and volume filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or trailing stop.
- **Stops**: Yes, fixed and trailing.
- **Default Values**:
  - `Length` = 20
  - `StopLossTicks` = 1000
  - `TrailingStartTicks` = 2000
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian, EMA, SMA
  - Stops: Fixed & Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

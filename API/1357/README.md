# Strategy1
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from TradingView script "strategy1". The strategy trades Bollinger channel rebounds. It enters long after the price falls below the lower band and then closes above it. Exits are triggered by crossing above the middle band, touching the upper band, or stop-loss below the channel.

## Details

- **Entry Criteria**: Price was below the lower band and then closes above it.
- **Long/Short**: Long only.
- **Exit Criteria**: Cross above middle band, touch of upper band, or stop-loss below channel.
- **Stops**: Yes, fixed stop below channel.
- **Default Values**:
  - `Length` = 20
  - `BufferFactor` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Variable
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

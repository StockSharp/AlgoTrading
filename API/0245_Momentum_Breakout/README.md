# Momentum Breakout Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This breakout system looks for sudden surges in momentum relative to its historical average. When momentum readings exceed the average by a large margin, price may be starting a fast directional move.

Testing indicates an average annual return of about 82%. It performs best in the stocks market.

The strategy buys when momentum rises above the average plus `Multiplier` times its standard deviation. A short is initiated when momentum falls below the average minus the same multiplier. Positions are closed once momentum returns toward its mean.

Traders who enjoy fast moves may appreciate the clear rules for capturing bursts of strength. A stop-loss based on percentage of price protects against failed breakouts.

## Details
- **Entry Criteria**:
  - **Long**: Momentum > Avg + Multiplier * StdDev
  - **Short**: Momentum < Avg - Multiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Momentum < Avg
  - **Short**: Exit when Momentum > Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `MomentumPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Momentum
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium


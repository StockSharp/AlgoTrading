# Combo 2/20 EMA Bandpass Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a fast and slow EMA crossover with a bandpass filter. Long positions are opened when the fast EMA is above the slow EMA and the bandpass value breaks above the sell zone. Short positions are opened when the fast EMA is below the slow EMA and the bandpass value falls below the buy zone. Positions are closed if signals disappear or before the start date.

Testing indicates an average annual return of around 47%. It performs best in the crypto market.

## Details
- **Entry Criteria**:
  - **Long**: Fast EMA > Slow EMA and bandpass > sell zone
  - **Short**: Fast EMA < Slow EMA and bandpass < buy zone
- **Long/Short**: Both sides
- **Exit Criteria**: Close position when signals disappear
- **Stops**: No
- **Default Values**:
  - `FastEmaLength` = 2
  - `SlowEmaLength` = 20
  - `BpfLength` = 20
  - `BpfDelta` = 0.5m
  - `BpfSellZone` = 5m
  - `BpfBuyZone` = -5m
  - `StartDate` = new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA Bandpass Filter
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

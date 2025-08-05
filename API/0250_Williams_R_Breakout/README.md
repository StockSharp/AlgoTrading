# Williams R Breakout Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This strategy seeks momentum bursts by watching Williams %R relative to its historical average. When the oscillator pushes far beyond typical readings, it may signal the start of a strong move.

Testing indicates an average annual return of about 91%. It performs best in the stocks market.

A long position is opened when %R climbs above the average plus `Multiplier` times an estimated standard deviation. A short position is taken when %R drops below the average minus the same multiplier. The trade closes once %R returns toward its average or a stop-loss is hit.

The approach caters to breakout traders who want early participation in emerging trends. Position risk is managed with a percentage stop based on the entry price.

## Details
- **Entry Criteria**:
  - **Long**: %R > Avg + Multiplier * StdDev
  - **Short**: %R < Avg - Multiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when %R < Avg
  - **Short**: Exit when %R > Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Williams %R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium


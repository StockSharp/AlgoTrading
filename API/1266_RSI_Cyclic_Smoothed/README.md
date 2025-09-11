# RSI Cyclic Smoothed
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the cyclic smoothed RSI indicator. It calculates dynamic percentile bands and trades reversals when the oscillator crosses them.

## Details

- **Entry Criteria**: CRSI crossing above the lower band or below the upper band.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite band cross.
- **Stops**: Yes.
- **Default Values**:
  - `DominantCycleLength` = 20
  - `Vibration` = 10
  - `Leveling` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium

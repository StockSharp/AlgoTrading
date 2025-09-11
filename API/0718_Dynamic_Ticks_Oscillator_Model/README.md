# Dynamic Ticks Oscillator Model (DTOM)
[Русский](README_ru.md) | [中文](README_cn.md)

The **Dynamic Ticks Oscillator Model** uses the rate of change of the NYSE Down Ticks index. When the ROC drops below a dynamic threshold based on standard deviation, the strategy opens a long position. The position is closed once the ROC rises above a positive threshold.

## Details
- **Entry Criteria**: `ROC < -StdDev * EntryStdDevMultiplier`
- **Long/Short**: Long only.
- **Exit Criteria**: `ROC > StdDev * ExitStdDevMultiplier`
- **Stops**: No.
- **Default Values**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: RateOfChange, StandardDeviation
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium


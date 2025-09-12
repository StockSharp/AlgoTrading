# Ticker Pulse Meter + Fear EKG Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines short and long lookbacks to spot oversold conditions and recoveries.
It buys when the combined percentile crosses the upper trigger and exits on a profit take cross.

## Details

- **Entry Criteria**: percentile crosses above `EntryThresholdHigh` or below `OrangeEntryThreshold`
- **Long/Short**: Long only
- **Exit Criteria**: cross below `ProfitTake`
- **Stops**: No
- **Default Values**:
  - `LookbackShort` = 50
  - `LookbackLong` = 200
  - `ProfitTake` = 95
  - `EntryThresholdHigh` = 20
  - `EntryThresholdLow` = 40
  - `OrangeEntryThreshold` = 95
- **Filters**:
  - Category: Oscillator
  - Direction: Long
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

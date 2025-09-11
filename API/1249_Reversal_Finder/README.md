# Reversal Finder Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Reversal Finder looks for large range candles making new extremes and closing back toward the other side of the bar.
It buys when price washes out to a new low but finishes near the high, and sells when price spikes to a new high but closes near the low.

## Details

- **Entry Criteria**: range expansion with close near opposite extreme after new high/low
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `Lookback` = 20
  - `SmaLength` = 20
  - `RangeMultiple` = 1.5
  - `RangeThreshold` = 0.5
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: SMA, Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium


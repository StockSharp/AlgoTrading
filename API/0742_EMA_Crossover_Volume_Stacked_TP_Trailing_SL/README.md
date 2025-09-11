# EMA Crossover with Volume + Stacked TP & Trailing SL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades EMA crossovers filtered by volume. It sets two ATR-based profit targets and trails the remaining position once price moves favorably.

## Details

- **Entry Criteria**:
  - Fast EMA crosses above/below slow EMA.
  - Volume > average volume * `VolumeMultiplier`.
- **Long/Short**: Long and Short.
- **Exit Criteria**:
  - First take profit at `TP1Multiplier * ATR` (33% of position).
  - Second take profit at `TP2Multiplier * ATR` (another 33%).
  - Trailing stop activates after price moves `TrailTriggerMultiplier * ATR` and trails at `TrailOffsetMultiplier * ATR`.
- **Stops**: Trailing stop only.
- **Default Values**:
  - `FastLength` = 21
  - `SlowLength` = 55
  - `VolumeMultiplier` = 1.2
  - `AtrLength` = 14
  - `Tp1Multiplier` = 1.5
  - `Tp2Multiplier` = 2.5
  - `TrailOffsetMultiplier` = 1.5
  - `TrailTriggerMultiplier` = 1.5
  - `CandleType` = 5m
- **Filters**:
  - Category: Trend following
  - Direction: Long/Short
  - Indicators: EMA, ATR, Volume
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

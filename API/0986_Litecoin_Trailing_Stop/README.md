# Litecoin Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Litecoin Trailing Stop Strategy** uses the Kaufman Adaptive Moving Average (KAMA) to detect bullish and bearish trends. It opens long positions when KAMA is rising and short positions when it is falling. After a configurable delay, a percentage-based trailing stop protects profits.

## Details
- **Entry Criteria**: KAMA slope with cooldown between entries.
- **Long/Short**: both directions.
- **Exit Criteria**: trailing stop.
- **Stops**: trailing stop after delay.
- **Default Values**:
  - `KamaLength = 50`
  - `BarsBetweenEntries = 30`
  - `TrailingStopPercent = 12`
  - `DelayBars = 50`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: KAMA
  - Stops: Trailing stop
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

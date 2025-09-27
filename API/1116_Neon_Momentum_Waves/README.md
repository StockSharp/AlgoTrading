# Neon Momentum Waves Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Neon Momentum Waves Strategy uses MACD histogram crossings to trade in both directions. The strategy goes long when the histogram crosses above the entry level (default zero) and goes short when it crosses below. Positions are closed when the histogram reaches configured exit levels.

## Details

- **Entry Criteria**: MACD histogram crosses entry level.
- **Long/Short**: Both directions.
- **Exit Criteria**: Histogram crosses long/short exit levels.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 20
  - `EntryLevel` = 0
  - `LongExitLevel` = 11
  - `ShortExitLevel` = -9
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

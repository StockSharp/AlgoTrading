# Honest Volatility Grid
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on multiple Keltner Channel levels to build a volatility grid. It scales into long and short positions across predefined bands and exits via opposite levels or a raw stop.

## Details

- **Entry Criteria**: Price reaches configured Keltner channel levels.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite channel or raw stop.
- **Stops**: Optional raw stop.
- **Default Values**:
  - `EmaPeriod` = 200
  - `Multiplier` = 1.0
  - `LEntry1Level` = -2
  - `SEntry1Level` = 2
  - `RawStopLevel` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

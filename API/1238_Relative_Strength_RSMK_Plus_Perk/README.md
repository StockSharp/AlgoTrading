# Relative Strength RSMK Plus Perk
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Markos Katsanos' Relative Strength (RSMK) indicator.

The strategy compares the asset to a reference market index using RSMK. A long position opens when RSMK crosses above its signal line, and a short position when it crosses below. The opposite crossover closes the position.

## Details

- **Entry Criteria**: RSMK crossing its signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `Period` = 90
  - `Smooth` = 3
  - `SignalPeriod` = 20
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSMK, EMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

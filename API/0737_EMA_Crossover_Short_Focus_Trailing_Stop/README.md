# EMA Crossover Short Focus Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy goes long when the 13 EMA is above the 33 EMA and no short position exists. It goes short when the 13 EMA is below the 33 EMA and no long position is open. Positions exit when the 13 EMA crosses the opposite EMA and a trailing stop follows recent extremes.

## Details
- **Entry Criteria:**
  - **Long:** 13 EMA ≥ 33 EMA and position ≤ 0.
  - **Short:** 13 EMA ≤ 33 EMA and position ≥ 0.
- **Long/Short:** both.
- **Exit Criteria:** long exits when 13 EMA < 33 EMA; short exits when 13 EMA > 25 EMA.
- **Stops:** trailing stop with distance `TrailDistance` and offset `TrailOffset`.
- **Default Values:** short EMA = 13, mid EMA = 25, long EMA = 33, trail distance = 10, trail offset = 2.

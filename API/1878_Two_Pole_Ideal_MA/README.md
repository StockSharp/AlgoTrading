# Two-Pole Ideal MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Crossover system approximating the "2pb Ideal MA" expert by comparing a fast EMA with a slow TEMA.

## Details

- **Entry Criteria**: Fast EMA crossing slow TEMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse on opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 30
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, TEMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Swing (H4)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

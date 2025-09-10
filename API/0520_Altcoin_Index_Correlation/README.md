# Altcoin Index Correlation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy compares EMA trends on the traded instrument and a reference index. It opens long when both fast EMAs are above their slow EMAs, and short when both are below. Optional inverse logic allows trading against the index trend or skipping the index completely.

## Details

- **Entry Criteria**:
  - Fast EMA above slow EMA on both instruments (or opposite if inverse).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover condition.
- **Stops**: None.
- **Default Values**:
  - `FastEmaLength` = 47
  - `SlowEmaLength` = 50
  - `IndexFastEmaLength` = 47
  - `IndexSlowEmaLength` = 50
  - `SkipIndexReference` = false
  - `InverseSignal` = false
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# Omega Galsky
[Русский](README_ru.md) | [中文](README_cn.md)

EMA crossover strategy with break-even stop logic.

## Details

- **Entry Criteria**: Fast EMA crosses slow EMA with price confirmation from EMA89.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss, take profit, or opposite signal.
- **Stops**: Yes.
- **Default Values**:
  - `Ema8Period` = 8
  - `Ema21Period` = 21
  - `Ema89Period` = 89
  - `FixedRiskReward` = 1.0m
  - `SlPercentage` = 0.001m
  - `TpPercentage` = 0.0025m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

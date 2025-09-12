# Monthly Performance Table Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades when ADX lies between +DI and -DI and both differences from ADX exceed configurable thresholds.

## Details

- **Entry Criteria**:
  - Long when |+DI - ADX| ≥ `LongDifference` and |-DI - ADX| ≥ `LongDifference` with ADX between +DI and -DI.
  - Short when |+DI - ADX| ≥ `ShortDifference` and |-DI - ADX| ≥ `ShortDifference` with ADX between -DI and +DI.
- **Long/Short**: Both.
- **Exit Criteria**: Reverse signal.
- **Stops**: No.
- **Default Values**:
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX, DMI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

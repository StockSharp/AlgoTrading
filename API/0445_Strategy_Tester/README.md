# Strategy Tester Sample Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This example illustrates how momentum and trend strength can be combined to
form a basic discretionary system. A linear regression slope measures short
term momentum while the Average Directional Index gauges the persistence of a
move. Two independent rules trigger entries: a momentum pivot accompanied by a
drop in ADX, or a new ADX high with momentum turning up from negative values.

The strategy is intentionally simple and focuses on long positions. It is meant
as a template for testing ideas such as ATR‑based risk levels and optional exit
controls. Developers can expand the exit logic or add stop‑loss handling to
turn it into a full trading model.

## Details

- **Entry Criteria**:
  - Momentum pivot high and ADX declining.
  - ADX pivot high with momentum rising from below zero.
- **Long/Short**: Long only by default.
- **Exit Criteria**:
  - Momentum pivot high (if momentum exit is enabled).
  - Custom strategy exit placeholder.
- **Stops**: None; ATR values are available for external use.
- **Default Values**:
  - Momentum length = 20, DI length = 14.
  - ADX key level = 25, ATR length = 14.
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Linear Regression, ADX, ATR
  - Stops: No
  - Complexity: Low
  - Timeframe: Short/medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes (momentum pivots)
  - Risk level: Medium

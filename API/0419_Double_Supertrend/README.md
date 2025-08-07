# Double Supertrend
[Русский](README_ru.md) | [中文](README_cn.md)

Double Supertrend employs two ATR‑based moving averages with different periods
and multipliers. The first line sets the trade direction, while the second can
act as a target or trailing exit. This combination allows flexible trend
following with defined profit and risk parameters.

When price moves above both lines and the strategy is set to trade long, a
position is opened. For short trades the conditions are mirrored. Exits depend on
the selected take‑profit type or a percentage stop loss.

## Details
- **Data**: Price candles.
- **Entry Criteria**: Price crosses supertrend lines in the allowed `Direction`.
- **Exit Criteria**: Opposite line break, take‑profit (`TPType`/`TPPercent`) or stop‑loss (`SLPercent`).
- **Stops**: Percentage stop based on `SLPercent`.
- **Default Values**:
  - `ATRPeriod1` = 10
  - `Factor1` = 3.0
  - `ATRPeriod2` = 20
  - `Factor2` = 5.0
  - `Direction` = "Long"
  - `TPType` = "Supertrend"
  - `TPPercent` = 1.5
  - `SLPercent` = 10.0
- **Filters**:
  - Category: Trend following
  - Direction: Configurable
  - Indicators: ATR‑based Supertrend
  - Complexity: Advanced
  - Risk level: Medium

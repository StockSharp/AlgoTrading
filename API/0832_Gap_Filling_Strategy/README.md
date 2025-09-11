# Gap Filling Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Gap Filling Strategy looks for overnight price gaps at the start of a new session. When a gap appears, the strategy either fades it expecting a move back to the previous day's price or, if inverted, trades in the direction of the gap with a stop at the gap level.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: New session and down gap (or up gap if inverted).
  - **Short**: New session and up gap (or down gap if inverted).
- **Exit Criteria**:
  - Gap fill price reached (profit target) or, when inverted, price hits the gap level stop.
- **Stops**: Uses the previous session price as target/stop.
- **Default Values**:
  - `CandleType` = 1 minute
  - `Invert` = false
  - `CloseWhen` = NewSession
- **Filters**:
  - Category: Gap trading
  - Direction: Long & Short
  - Indicators: None
  - Complexity: Simple
  - Risk level: Medium

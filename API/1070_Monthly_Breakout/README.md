# Monthly Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades breakouts of the current month's high or low only during selected calendar months. The direction is chosen via `EntryOption`, and positions are closed after a fixed number of bars.

## Details

- **Entry Criteria**:
  - Depend on `EntryOption` and selected months (e.g., long when close crosses above monthly high).
- **Long/Short**: Configurable.
- **Exit Criteria**: Close after `HoldingPeriod` bars.
- **Stops**: No.
- **Default Values**:
  - `EntryOption` = LongAtHigh
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Breakout
  - Direction: Configurable
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# London BreakOut Classic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of the London session using the Asian range. The high and low between 00:00 and 06:55 UTC form a box. After 07:00 UTC a breakout above the high opens a long position and a breakout below the low opens a short position. Stop loss is placed at the midpoint of the box and take profit uses a configurable risk-reward factor.

## Details

- **Entry Criteria**:
  - Long: price crosses above the Asian session high.
  - Short: price crosses below the Asian session low.
- **Exit Criteria**:
  - Stop loss or take profit.
  - End of trading window.
- **Stops**: Yes.
- **Default Values**:
  - Asian session: 00:00–06:55 UTC.
  - Trading session: 07:00–16:00 UTC.
  - CRV = 1.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

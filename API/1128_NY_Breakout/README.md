# NY Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades breakouts of the range formed between 13:00 and 13:30 UTC. After the window closes, the strategy enters when price breaks the session high or low, targeting twice the range and placing a stop on the opposite side.

## Details

- **Entry Criteria**:
  - First bar after 13:30 UTC closes above the session high -> long.
  - First bar after 13:30 UTC closes below the session low -> short.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Profit target at `RewardRisk` times the range.
  - Stop at the opposite range boundary.
- **Stops**: Yes.
- **Default Values**:
  - `RewardRisk` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

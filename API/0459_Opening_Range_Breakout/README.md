# Opening Range Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Opening Range Breakout strategy tracks the highest and lowest prices during the first minutes of a trading session. After the range ends, breakout orders are placed beyond the range with a configurable buffer. Targets are derived from a reward-to-risk ratio while stops are set on the opposite side of the range.

## Details

- **Entry Criteria**:
  - After the opening range, go long when price closes above the high plus buffer.
  - Go short when price closes below the low minus buffer.
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop and target based on range and reward-to-risk ratio.
- **Stops**: Yes
- **Default Values**:
  - `RangeMinutes` = 15
  - `RewardRisk` = 2.0
  - `EntryBuffer` = 0.0001
  - `SessionStart` = 08:00
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

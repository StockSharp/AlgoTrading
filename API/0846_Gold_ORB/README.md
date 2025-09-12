# Gold ORB Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy captures the high and low of the Asia session and trades breakouts during the following hours. Stops and targets are derived from the range size with a reward multiplier.

## Details

- **Entry Criteria**:
  - During the trade window, go long when the price closes above the recorded Asia high.
  - Go short when the price closes below the recorded Asia low.
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop and target based on the range size and reward multiplier.
- **Stops**: Yes
- **Default Values**:
  - `AsiaStart` = 00:00
  - `AsiaEnd` = 06:00
  - `TradeStart` = 06:00
  - `TradeEnd` = 10:00
  - `RewardMultiplier` = 2.0
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Low
  - Timeframe: 5m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium


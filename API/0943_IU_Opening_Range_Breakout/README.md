# IU Opening Range Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The IU Opening Range Breakout strategy monitors the high and low of the first bar of each session and trades breakouts in either direction. Stops use the previous bar's extreme and targets are derived from a configurable risk-to-reward ratio. All positions are closed at a user-defined end time.

## Details

- **Entry Criteria**:
  - Go long when the close crosses above the first bar's high.
  - Go short when the close crosses below the first bar's low.
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop at the previous bar's low/high.
  - Target based on risk-to-reward ratio.
  - Close all positions at `EndTime`.
- **Stops**: Yes
- **Default Values**:
  - `RiskReward` = 2.0
  - `MaxTrades` = 2
  - `EndTime` = 15:00
  - `CandleType` = 1 minute
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

# 5 EMA No-Touch Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The 5 EMA No-Touch Breakout strategy waits for a candle that remains entirely on one side of the 5-period EMA. When price later breaks the extreme of that setup candle, the strategy enters in the breakout direction. The stop-loss is placed at the opposite extreme and the take-profit is set at a multiple of the risk.

## Details

- **Entry Criteria**:
  - Candle high below EMA → prepare long; enter when price breaks above that candle's high.
  - Candle low above EMA → prepare short; enter when price breaks below that candle's low.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Stop at setup candle extreme.
  - Target at `RewardRisk` × risk.
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 5
  - `RewardRisk` = 3.0
- **Filters**:
  - Category: Breakout
  - Direction: Long/Short
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Low
  - Timeframe: 5-minute
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

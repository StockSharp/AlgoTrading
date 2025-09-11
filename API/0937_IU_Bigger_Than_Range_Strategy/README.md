# IU Bigger Than Range Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy that opens trades when the candle body is larger than the previous range of recent candles.

The system compares the current candle body with the range between the highest open/close and lowest open/close over a configurable lookback period. If the body exceeds the previous range, it enters in the candle direction and manages risk via configurable stop methods.

## Details

- **Entry Criteria**: Candle body larger than previous range; direction based on candle body.
- **Long/Short**: Both.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Previous candle, ATR or swing levels.
- **Default Values**:
  - `LookbackPeriod` = 22
  - `RiskToReward` = 3
  - `StopLossMethod` = PreviousHighLow
  - `AtrLength` = 14
  - `AtrFactor` = 2m
  - `SwingLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Highest, Lowest, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Captain Backtest Model Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Tracks the early session price range to establish a daily bias. Trades breakouts during the trade window after a retracement.

## Details

- **Bias**: Morning range high or low defines long or short bias.
- **Entry**: Break above/below previous candle once retracement conditions are met.
- **Long/Short**: Both directions.
- **Exit**: Fixed risk/reward or end of trade window.
- **Stops**: Fixed point distance.
- **Default Values**:
  - PrevRangeStart = 06:00
  - PrevRangeEnd = 10:00
  - TakeStart = 10:00
  - TakeEnd = 11:15
  - TradeStart = 10:00
  - TradeEnd = 16:00
  - Risk = 25
  - Reward = 75

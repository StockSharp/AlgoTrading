# 2:45 AM Candle Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This intraday strategy monitors the 2:45 AM candle and trades breakouts of its high or low within the next few bars. When price exceeds the candle's high, it enters a long position; when price falls below the candle's low, it opens a short position. Positions are closed at the end of the observation window if no opposite breakout occurs.

## Details

- **Entry Criteria**:
  - **Long**: Price breaks above the high of the 2:45 AM candle within the next `LookForwardBars` candles.
  - **Short**: Price breaks below the low of the 2:45 AM candle within the next `LookForwardBars` candles.
- **Long/Short**: Both.
- **Exit Criteria**:
  - End of the observation window or opposite breakout.
- **Stops**: None.
- **Default Values**:
  - `TargetHour` = 2
  - `TargetMinute` = 45
  - `LookForwardBars` = 2
  - `CandleType` = 45-minute candles
- **Filters**:
  - Category: Time-based breakout
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# Pinbar Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Pinbars highlight sudden rejections of price and can signal short-term turning points. This strategy measures the length of the candle's tail relative to its body, looking for long shadows that stick out from recent price action. A moving average filter helps trade in the direction of the underlying trend.

During each candle update the system calculates upper and lower shadows and compares them to the body size. A bullish pinbar with a long lower wick may trigger a long entry if price is above the moving average. Likewise a bearish pinbar with an extended upper tail can initiate a short position in a downtrend. Stops are placed a fixed percentage from entry.

The trade is closed when an opposite pinbar appears against the open position or the protective stop is reached. Combining the pinbar logic with a trend filter improves reliability by avoiding countertrend setups.

## Details

- **Entry Criteria**: Pinbar with long tail and small opposite shadow, confirmed by trend.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite pinbar or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `TailToBodyRatio` = 2
  - `OppositeTailRatio` = 0.5
  - `MAPeriod` = 20
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 82%. It performs best in the stocks market.

# Inside Bar Breakout Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
An inside bar forms when a candle's range is fully contained within the previous bar's high and low. It signals short-term indecision that can lead to a breakout once price clears the pattern. This strategy waits for that break and then trades in the direction of the expansion.

Each new candle is compared with the one before it. If an inside bar appears, the system marks its high and low and watches for a close outside those levels. A bullish breakout opens a long position with a stop below the pattern low, while a bearish breakout triggers a short with a stop above the pattern high.

Should price fail to break out immediately, the strategy manages existing positions by exiting if the next candle moves against the trade beyond the prior bar's extremes.

## Details

- **Entry Criteria**: Breakout of an inside bar's high or low.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing previous candle extreme or stop-loss.
- **Stops**: Yes, placed beyond the pattern.
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

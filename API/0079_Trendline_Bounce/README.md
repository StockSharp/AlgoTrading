# Trendline Bounce Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Markets often respect trendlines drawn across prior swing highs or lows. This strategy automatically fits regression lines to recent price action and looks for candles that bounce from those lines in the direction of the dominant trend.

Recent candles are stored to calculate upward or downward sloping support and resistance lines. When price nears a trendline and a candle confirms the bounce while staying on the correct side of a moving average, the system enters a trade. The stop is set using a percentage of price and an exit occurs on a cross of the moving average.

By only trading in the prevailing direction and waiting for a clear reaction at support or resistance, the method attempts to capture continuation moves without chasing breakouts.

## Details

- **Entry Criteria**: Price touches calculated trendline and candle closes in trend direction above/below MA.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing moving average or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `TrendlinePeriod` = 20
  - `MAPeriod` = 20
  - `BounceThresholdPercent` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MA, Trendlines
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# Three-Bar Reversal Up Strategy

This pattern catches quick bullish turns after a short decline. It requires two consecutive down candles followed by a strong up candle that closes above the prior bar's high. The logic optionally checks that price was trending lower beforehand.

The strategy keeps the last three candles in memory. Once the sequence matches the criteria and any downtrend filter is satisfied, a long position is opened. A volatility stop below the pattern low caps risk on the trade.

After entry the system waits for either a stop hit or the appearance of another setup in the opposite direction. This simple approach suits markets prone to sharp bounces from oversold conditions.

## Details

- **Entry Criteria**: Two bearish candles with lower lows then a bullish candle closing above the middle bar's high.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop-loss or next pattern.
- **Stops**: Yes, below pattern low.
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendLength` = 5
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

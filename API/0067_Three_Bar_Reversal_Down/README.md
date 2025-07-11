# Three-Bar Reversal Down Strategy

A mirror image of the bullish version, this setup looks for quick bearish reversals. After two strong up candles that push to new highs, a decisive bearish candle closes below the prior bar's low. A brief uptrend beforehand helps confirm buyer exhaustion.

The algorithm tracks a rolling window of three candles. When the pattern appears and any uptrend requirement is met, a short position is taken with the stop above the pattern high. The rules are straightforward so signals occur immediately at candle close.

The trade is exited on the protective stop or when another pattern forms. Because it plays short-term pullbacks within a potential down swing, it works best in volatile markets.

## Details

- **Entry Criteria**: Two bullish candles with higher highs then a bearish candle closing below the middle bar's low.
- **Long/Short**: Short only.
- **Exit Criteria**: Stop-loss or next pattern.
- **Stops**: Yes, above pattern high.
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendLength` = 5
- **Filters**:
  - Category: Pattern
  - Direction: Short
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# Bollinger Band Reversal Strategy

Price extremes outside the Bollinger Bands often snap back toward the middle band. This approach fades those extensions, buying dips below the lower band when the candle finishes green and selling rallies above the upper band after a red candle.

The algorithm calculates Bollinger Bands on each bar and checks whether the close breaches the outer band. If a bullish candle closes below the lower band a long is opened; if a bearish candle closes above the upper band a short is taken. The stop relies on an ATR multiple while exits occur when price returns to the middle band.

Mean reversion trades typically last only a few bars, making this setup suitable for short-term volatility contractions.

## Details

- **Entry Criteria**: Close below lower band with bullish candle or close above upper band with bearish candle.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing middle band or stop-loss.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Bollinger Bands, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

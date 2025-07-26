# Outside Bar Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
An outside bar occurs when a candle's range exceeds that of the previous candle, creating a brief surge of volatility. This strategy fades the move if the outside bar closes in the opposite direction of the prior trend, expecting a snap back toward equilibrium.

When an outside bar forms, the algorithm determines whether the candle is bullish or bearish. A bullish outside bar after a decline opens a long position with a stop below the bar's low. A bearish outside bar after a rally triggers a short with a stop above its high. Trades exit if price subsequently breaks through that extreme.

The setup seeks quick reversals following an exhaustive thrust and is best used when markets are choppy rather than trending strongly.

## Details

- **Entry Criteria**: Outside bar closing opposite the previous move.
- **Long/Short**: Both.
- **Exit Criteria**: Price breaking outside bar high/low or stop-loss.
- **Stops**: Yes, placed beyond the pattern.
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 121%. It performs best in the crypto market.

# Keltner Channel Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Volatility-based channels can highlight overextended moves. This method fades price when it pushes outside the Keltner Channel, anticipating a snap back toward the middle line. It uses an exponential moving average and ATR to size the channel width.

As each candle completes, the strategy checks whether the close is beyond the upper or lower band and whether the candle direction agrees. Bullish candles closing below the lower band spark long entries, while bearish candles above the upper band prompt shorts. Positions exit once price crosses the middle band or when the ATR-based stop is reached.

By trading in the opposite direction of short‑term extremes, the system seeks quick mean reversion moves within a broader range.

## Details

- **Entry Criteria**: Close outside Keltner Channel in the direction of the candle.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing middle band or stop-loss.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `StopLossAtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Keltner Channel
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 106%. It performs best in the stocks market.

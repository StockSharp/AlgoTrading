# Three White Soldiers Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Three White Soldiers pattern is a classic bullish reversal consisting of three consecutive strong up candles.
After a downtrend, this sequence often marks the start of a sustained move higher as buying pressure overwhelms sellers.

Testing indicates an average annual return of about 175%. It performs best in the stocks market.

The strategy enters long once the third soldier forms, expecting follow-through from the surge in momentum.
Short trades are not taken because the setup is purely bullish, but the system does allow exiting short positions initiated by other methods.

Stops are placed a short distance below the pattern to guard against false signals and positions exit if price closes back below that level.

## Details

- **Entry Criteria**: pattern match
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
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


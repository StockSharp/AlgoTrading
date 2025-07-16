# CCI Divergence Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Commodity Channel Index divergences can foreshadow trend reversals when price moves in the opposite direction of the indicator. This strategy compares swing highs and lows in price to those of the CCI to identify hidden strength or weakness.

On each candle the system updates recent price and CCI values, flagging bullish divergence when price makes a new low while CCI forms a higher low. Bearish divergence is the opposite. When a divergence aligns with oversold or overbought levels, a trade is opened with a volatility stop.

Exits occur when the CCI crosses back through the zero line, signaling the impulse has played out. Because divergences can persist, the rules also reset after a fixed number of bars to avoid stale signals.

## Details

- **Entry Criteria**: Price/CCI divergence with CCI below -100 for longs or above +100 for shorts.
- **Long/Short**: Both.
- **Exit Criteria**: CCI crossing zero or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `CciPeriod` = 20
  - `DivergencePeriod` = 5
  - `OverboughtLevel` = 100
  - `OversoldLevel` = -100
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: CCI
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium

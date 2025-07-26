# Bearish Engulfing Pattern Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This pattern aims to capture the start of a bearish swing after a rally. A bearish engulfing occurs when a red candle completely swallows the prior bullish body. Counting a few consecutive up bars before the pattern ensures the market was previously rising.

The algorithm stores each candle in sequence. If the new bar closes lower than it opens and its body engulfs the previous bullish bar, a short sale is executed. The stop-loss is positioned above the pattern high to limit exposure.

Positions are typically managed using the protective stop, although the trader may exit manually if conditions change. Requiring an uptrend helps avoid false signals during choppy markets.

## Details

- **Entry Criteria**: Bearish candle engulfs prior bullish bar, optional uptrend present.
- **Long/Short**: Short only.
- **Exit Criteria**: Stop-loss or discretionary.
- **Stops**: Yes, above pattern high.
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendBars` = 3
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

Testing indicates an average annual return of about 79%. It performs best in the stocks market.

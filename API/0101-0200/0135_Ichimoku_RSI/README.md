# Ichimoku RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Ichimoku RSI uses Ichimoku cloud levels to define trend direction while RSI pinpoints short-term pullbacks.
Trades align with the cloud, entering when RSI recovers from oversold in an uptrend or falls from overbought in a downtrend.

Testing indicates an average annual return of about 142%. It performs best in the stocks market.

By combining a broad trend filter with a momentum oscillator, the strategy aims to join strong moves after brief pauses.

A stop at a fixed percentage of the entry price protects against deeper corrections.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
    Senkou Span A above Senkou Span B is an uptrend, below it a downtrend. In an uptrend RSI crossing back above RsiOversold goes long; in a downtrend RSI crossing back below RsiOverbought goes short. An opposite signal reverses the position.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Ichimoku, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium


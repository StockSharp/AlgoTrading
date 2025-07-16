# Bullish Engulfing Pattern Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This setup looks for a sharp bullish reversal when a candle completely engulfs the prior bearish bar. Such a formation often ends a short-term decline and hints at renewed upward momentum. The optional downtrend filter counts consecutive red candles to confirm sellers are exhausted.

During live operation the algorithm watches each incoming candle and keeps track of the previous bar. If the new candle closes higher than it opens and its body wraps around the prior bar, a long entry is triggered. The stop is placed just below the pattern low to cap risk.

Trades remain open until the stop is hit or another signal suggests manual exit. Because confirmation from earlier down bars strengthens the setup, the strategy avoids chasing weak reversals.

## Details

- **Entry Criteria**: Bullish candle engulfs prior bearish bar, optional downtrend present.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop-loss or discretionary.
- **Stops**: Yes, below pattern low.
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendBars` = 3
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

# Bollinger Winner Pro
[Русский](README_ru.md) | [中文](README_cn.md)

Bollinger Winner Pro expands on the Lite version by adding modular filters and
risk controls. It still looks for price closing outside the Bollinger Bands, but
trades are executed only when optional confirmations agree.

Traders may enable RSI, Aroon and moving‑average filters to confirm momentum
and trend direction. An integrated stop‑loss can also be activated to cap risk.
This flexibility lets the strategy adapt to different markets or testing needs.

The approach targets mean reversion: once price re‑enters the bands or touches
the opposite side, the position is closed or the stop is hit. Because multiple
filters can be stacked, signals are less frequent but higher in quality.

## Details
- **Data**: Price candles.
- **Entry Criteria**: Candle closes outside a band and all enabled filters agree.
- **Exit Criteria**: Return to middle/opposite band or stop‑loss if `UseSL` is true.
- **Stops**: Optional stop‑loss controlled by `UseSL`.
- **Default Values**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filters**:
  - Category: Mean reversion with confirmations
  - Direction: Long & Short
  - Indicators: Bollinger Bands, RSI, Aroon, Moving Average
  - Complexity: Advanced
  - Risk level: Medium

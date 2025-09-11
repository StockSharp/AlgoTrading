# Modular Range-Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy targets range-bound markets using two modules that cannot be active at the same time. The first module relies on MACD momentum confirmation with RSI and Bollinger Bands mean reversion. The second module buys or sells extremes when price bounces back inside the Bollinger Bands with RSI oversold or overbought levels. ATR-based stops and optional exits via Bollinger Bands or RSI reversals manage risk.

## Details

- **Entry Criteria**:
  - **Logic 1 Long**: ADX below threshold, MACD crosses above signal, RSI above its SMA, price below middle Bollinger band.
  - **Logic 1 Short**: ADX below threshold, MACD crosses below signal, RSI below its SMA, price above middle Bollinger band.
  - **Logic 2 Long**: ADX below threshold, price crosses back above lower band, RSI below oversold level.
  - **Logic 2 Short**: ADX below threshold, price crosses back below upper band, RSI above overbought level.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - ATR stop loss.
  - Optional Bollinger or RSI signals depending on active logic.
- **Stops**: ATR multiples.
- **Default Values**: Bollinger 20/2, RSI 14, MACD 12/26/9, ATR 14, ADX 14.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Complex
  - Timeframe: Medium-term

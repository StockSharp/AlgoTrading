# XAUUSD 10-Minute Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades XAUUSD on 10-minute candles using MACD, RSI and Bollinger Bands signals. It opens long positions when bullish conditions appear and short positions when bearish signals trigger. The system applies ATR-based stop-loss and take-profit levels adjusted for a fixed spread.

## Details

- **Entry Criteria**:
  - **Long**: MACD line crosses above signal, RSI below oversold or price below lower Bollinger Band.
  - **Short**: MACD line crosses below signal, RSI above overbought or price above upper Bollinger Band.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Position closed on opposite signal, stop-loss or take-profit.
- **Stops**: ATR stop-loss at `3 * ATR`, take-profit at `5 * ATR`.
- **Default Values**:
  - MACD fast/slow/signal: `12/26/9`.
  - RSI period: `14`, overbought `65`, oversold `35`.
  - Bollinger length `20`, width `2`.
  - ATR period `14`.
  - Spread `38` ticks.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Intraday

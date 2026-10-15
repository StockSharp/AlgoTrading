# Three Candle Bullish Engulfing Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy looks for a three-candle bullish or bearish engulfing pattern. It supports optional RSI breakout entries and a trailing stop with time-based exits.

## Details

- **Entry Criteria**:
  - **Long**: Bullish candle, small doji, and bullish engulfing candle.
  - **Short**: Bearish candle, small doji, and bearish engulfing candle.
- **Long/Short**: Both (long only mode available).
- **Exit Criteria**:
  - Trailing stop, opposite candle break or session end.
- **Stops**: Yes.
- **Default Values**:
  - `TrailPerc` = 1.5
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `RsiLength` = 14
  - `RsiLevel` = 80
  - `StopLossPerc` = 5

# Bullish Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that searches for classic bullish reversal candlestick formations. When any of these patterns appears below a 50-period simple moving average the strategy opens a long position. An optional trailing stop protects open profits.

## Patterns
- **Abandoned Baby** – two consecutive bearish candles followed by a bullish candle that closes above the first candle's open while the second candle gaps lower.
- **Morning Doji Star** – a bearish candle, a doji or small-bodied candle, then a bullish candle closing back into the first candle's body.
- **Three Inside Up** – a bearish candle, a smaller bullish candle within its range, then a bullish candle closing above the first candle's open.
- **Three Outside Up** – a bearish candle followed by a larger bullish candle engulfing it and a third bullish candle confirming the reversal.
- **Three White Soldiers** – three consecutive bullish candles with rising closes.

## Details
- **Entry Criteria**: any listed pattern and the last candle opened below the moving average
- **Long/Short**: Long
- **Exit Criteria**: optional trailing stop
- **Stops**: Trailing
- **Default Values**:
  - `MaPeriod` = 50
  - `TrailingStop` = 50
  - `UseTrailingStop` = true
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: SMA
  - Stops: Trailing
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

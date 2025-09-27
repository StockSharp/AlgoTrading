# TMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

TMA Strategy uses multiple smoothed moving averages and candlestick patterns to trade in the direction of the 200-period trend. It combines 3-line strike and engulfing signals with a session filter.

## Details

- **Entry Criteria**: bullish engulfing or 3-line strike in uptrend / bearish engulfing or 3-line strike in downtrend with EMA(2) above/below SMA(200) and optional session filter
- **Long/Short**: Both
- **Exit Criteria**: EMA(2) crossing SMA(200)
- **Stops**: No
- **Default Values**:
  - `CandleType` = 5-minute candles
  - `FastLength` = 21
  - `MidLength` = 50
  - `Mid2Length` = 100
  - `SlowLength` = 200
  - `UseSession` = false
  - `SessionStart` = 08:30
  - `SessionEnd` = 12:00
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, EMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

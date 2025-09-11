# Refined MA + Engulfing (M5 + Confirmed Structure Break)
[Русский](README_ru.md) | [中文](README_cn.md)

Refined MA + Engulfing combines two simple moving averages, engulfing candles, and structure break confirmation. A trade is placed when at least two confluence factors align and a cooldown has passed.

## Details

- **Entry Criteria**: After a confirmed bullish or bearish structure break, price above or below both SMAs, and at least two of four confluences (engulfing, structure break, MA filter, fib placeholder) with cooldown satisfied.
- **Long/Short**: Both.
- **Exit Criteria**: None.
- **Stops**: No.
- **Default Values**:
  - `Ma1Length` = 66
  - `Ma2Length` = 85
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: SMA, Engulfing, Structure Break
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: 5-minute
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# ZapTeam Pro Strategy v6 — EMA
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified version using EMA21/EMA50 crossover with EMA200 trend filter. Buys on bullish crossover and sells on bearish crossover (optional shorts).

## Details

- **Entry Criteria**: EMA21 crosses EMA50 with trend filter
- **Long/Short**: Both (shorts optional)
- **Exit Criteria**: Opposite crossover
- **Stops**: No
- **Default Values**:
  - `Ema21Length` = 21
  - `Ema50Length` = 50
  - `Ema200Length` = 200
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# Flex ATR
[Русский](README_ru.md) | [中文](README_cn.md)

Flex ATR dynamically selects EMA, RSI, and ATR periods based on the current timeframe. A long trade opens when the fast EMA crosses above the slow EMA and RSI exceeds 50. A short trade triggers on the opposite crossover with RSI below 50. Exits use ATR-based stops or an optional trailing stop.

## Details

- **Entry Criteria**: Fast EMA vs slow EMA cross with RSI filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop or target, optional trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `AtrStopMult` = 3
  - `AtrProfitMult` = 1.5
  - `EnableTrailingStop` = true
  - `AtrTrailMult` = 1
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, RSI, ATR
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

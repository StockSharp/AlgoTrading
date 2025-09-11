# EMA Cross MACD Session Start Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when a fast EMA crosses above a slow EMA and the MACD histogram is positive. It enters short on the opposite cross with a negative histogram. If these conditions are already true at the first bar of a trading session, a position is opened immediately. Positions close on an opposite crossover or when the session ends.

## Details

- **Entry Criteria**:
  - Fast EMA crosses above slow EMA with positive MACD histogram.
  - Or on the first session bar when fast EMA is above slow EMA and MACD histogram is positive.
- **Exit Criteria**:
  - Opposite EMA cross or session end.
- **Indicators**: EMA, MACD.
- **Type**: Trend following.
- **Timeframe**: 5 minutes (default).

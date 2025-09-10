# Backward Number of Bars Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy keeps a long position only during the most recent bars counted back from the current time. It demonstrates how to restrict trading to a moving historical window.

## Details

- **Entry Criteria**: Candle time is within the last *N* bars from the start time.
- **Exit Criteria**: Candle time falls outside this window.
- **Long/Short**: Long only.
- **Stops**: None.
- **Default Values**:
  - `Bar count` = 50
  - `Candle type` = 1-minute candles
- **Filters**:
  - Category: Time based
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Simple
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low

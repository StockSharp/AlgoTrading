# Daytrading ES Wick Length Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters a long position when the total wick length of a candle exceeds its moving average plus an offset and exits after holding the position for a fixed number of bars.

## Details

- **Entry Criteria**: Total wick length greater than moving average with offset.
- **Exit Criteria**: Position closed after holding for `Hold periods` bars.
- **Long/Short**: Long only.
- **Stops**: None.
- **Default Values**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = 1-minute candles
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: Moving Average, Wick length
  - Stops: No
  - Complexity: Simple
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

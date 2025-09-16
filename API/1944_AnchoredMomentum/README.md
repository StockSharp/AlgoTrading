# Anchored Momentum
[Русский](README_ru.md) | [中文](README_cn.md)

The Anchored Momentum strategy calculates the ratio between EMA and SMA of candle closing prices. When momentum rises above an upper threshold it opens long positions, and when it falls below a lower threshold it opens short positions. Opposite signals close current positions.

## Details

- **Entry Criteria**: Momentum crosses above `UpLevel` to go long, below `DownLevel` to go short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal closes the position.
- **Stops**: No.
- **Default Values**:
  - `SmaPeriod` = 8
  - `EmaPeriod` = 6
  - `UpLevel` = 0.025m
  - `DownLevel` = -0.025m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

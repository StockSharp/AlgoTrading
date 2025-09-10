# Adaptive HMA Plus
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive Hull Moving Average strategy that adjusts its period based on volatility or volume. It opens long or short positions when the HMA slope points in the trend direction during active market conditions.

## Details

- **Entry Criteria**: Signals based on adaptive HMA, ATR or volume.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `MinPeriod` = 172
  - `MaxPeriod` = 233
  - `AdaptPercent` = 0.031m
  - `FlatThreshold` = 0m
  - `UseVolume` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, ATR, Volume
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium


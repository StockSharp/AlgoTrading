# Supertrend Nitin
[Русский](README_ru.md) | [中文](README_cn.md)

Supertrend strategy by Nitin on 5-minute candles.

## Details

- **Entry Criteria**: Direction change upward.
- **Long/Short**: Long only.
- **Exit Criteria**: Direction change downward.
- **Stops**: No.
- **Default Values**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ATR, Supertrend
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Supertrend 5m
[Русский](README_ru.md) | [中文](README_cn.md)

Supertrend strategy on 5-minute candles.

## Details

- **Entry Criteria**: Price crossing above Supertrend.
- **Long/Short**: Long only.
- **Exit Criteria**: Price crossing below Supertrend.
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

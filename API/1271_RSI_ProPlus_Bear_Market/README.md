# RSI Pro+ Bear Market Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys when the RSI crosses above a specified level and exits at a fixed percentage from the entry price. It is designed for bearish market conditions expecting quick rebounds.

## Details

- **Entry Criteria**: RSI crossing above level
- **Long/Short**: Long
- **Exit Criteria**: Take profit at percentage from entry
- **Stops**: No
- **Default Values**:
  - `RSI Period` = 11
  - `RSI Level` = 8
  - `Take Profit %` = 0.11
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

# Grover Llorens Activator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive trailing strategy based on ATR that switches direction when price crosses the internal activator line.

It buys when the difference between price and the trailing line crosses above zero. It sells when it crosses below zero.

## Details

- **Entry Criteria**: Price crossing trailing stop calculated from ATR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Length` = 480
  - `Multiplier` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

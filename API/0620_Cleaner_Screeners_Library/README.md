# Cleaner Screeners Library
[Русский](README_ru.md) | [中文](README_cn.md)

Simple screener strategy that evaluates RSI across multiple symbols and prints buy or sell ratings. It serves as a foundation for building custom multi-asset screeners.

## Details

- **Entry Criteria**: RSI values are checked against thresholds for each symbol.
- **Long/Short**: None (signals only)
- **Exit Criteria**: None
- **Stops**: None
- **Default Values**:
  - `RsiLength` = 14
  - `StrongThreshold` = 70m
  - `WeakThreshold` = 60m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Screener
  - Direction: N/A
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: N/A

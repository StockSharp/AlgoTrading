# Custom Signal Oscillator
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using the difference between two price signals. It goes long when the oscillator crosses above zero and short when it crosses below zero. When long-only mode is enabled, negative crossings close the position.

## Details

- **Entry Criteria**: Oscillator crossing zero.
- **Long/Short**: Both directions or long only.
- **Exit Criteria**: Opposite signal or zero cross in long-only mode.
- **Stops**: No.
- **Default Values**:
  - `LongOnly` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Reflex Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses John Ehlers' Reflex Oscillator. It enters long when the oscillator crosses above an upper threshold and enters short when it crosses below a lower threshold. Positions are closed when the oscillator returns to the zero line.

## Details

- **Entry Criteria**:
  - **Long**: oscillator crosses above `UpperLevel`.
  - **Short**: oscillator crosses below `LowerLevel`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Long position: oscillator crosses below zero.
  - Short position: oscillator crosses above zero.
- **Stops**: No.
- **Default Values**:
  - `ReflexPeriod` = 20.
  - `SuperSmootherPeriod` = 8.
  - `PostSmoothPeriod` = 33.
  - `UpperLevel` = 1.
  - `LowerLevel` = -1.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

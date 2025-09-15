# Wlx BW5 Zone Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bill Williams' Awesome Oscillator (AO) and Accelerator Oscillator (AC) to identify strong momentum sequences. A buy (sell) signal appears when both oscillators move higher (lower) for five consecutive bars. The system reverses or opens positions accordingly.

## Details

- **Entry Criteria**:
  - **Long**: `AO` and `AC` rising for five consecutive bars.
  - **Short**: `AO` and `AC` falling for five consecutive bars.
- **Long/Short**: Both sides.
- **Exit Criteria**: Reverse position on opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Timeframe` = 4 hours.
  - `Direct` = true.
  - `SignalBar` = 1.
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

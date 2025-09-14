# Forecast Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy adapts the classical Forecast Oscillator indicator to StockSharp. It combines a linear regression baseline with Tillson T3 smoothing to highlight trend reversals. A buy signal appears when the oscillator crosses above its smoothed line while the smoothed line remains below zero. A sell signal is produced on the opposite conditions.

The algorithm follows the original MQL implementation and supports enabling or disabling position opening and closing separately.

## Details

- **Entry Criteria**:
  - **Long**: Oscillator crosses above T3 and the T3 is negative.
  - **Short**: Oscillator crosses below T3 and the T3 is positive.
- **Long/Short**: Both directions are supported.
- **Exit Criteria**:
  - Opposite signals if the corresponding close options are enabled.
- **Stops**: None.
- **Filters**: None.

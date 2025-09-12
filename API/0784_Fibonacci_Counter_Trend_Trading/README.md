# Fibonacci Counter-Trend Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses a Volume Weighted Moving Average (VWMA) and standard deviation to build Fibonacci bands. It enters long when price drops below the selected lower band and short when price rises above the upper band. Optionally positions close when price crosses the VWMA basis.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses below the chosen lower band.
  - **Short**: Close crosses above the chosen upper band.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Basis**: Optional exit when price crosses VWMA.
  - **Reverse**: Opposite band signal reverses position.
- **Stops**: None.
- **Indicators**: VolumeWeightedMovingAverage, StandardDeviation.

# Weight Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines RSI, Money Flow Index, Williams %R and DeMarker into a weighted oscillator smoothed by a simple moving average. Positions are opened or reversed when the oscillator crosses configurable high or low levels. The trend mode determines whether trades follow or fade the oscillator signals.

## Details

- **Entry Criteria**:
  - **Trend = Direct**:
    - **Long**: oscillator drops below the low level.
    - **Short**: oscillator rises above the high level.
  - **Trend = Against**:
    - **Long**: oscillator rises above the high level.
    - **Short**: oscillator falls below the low level.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite crossing reverses the position.
- **Stops**: No explicit stops.
- **Filters**: Weighted oscillator with SMA smoothing.

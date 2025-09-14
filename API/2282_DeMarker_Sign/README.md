# DeMarker Sign Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the DeMarker oscillator to detect potential trend reversals. On each completed candle (4-hour timeframe by default), the DeMarker value is compared to configurable upper and lower thresholds. When the oscillator rises above the lower threshold (0.3 by default), the strategy enters a long position and closes any short position. When the oscillator falls below the upper threshold (0.7 by default), it enters a short position and closes any long position. Positions are held until an opposite signal appears.

## Details

- **Entry Criteria**:
  - **Long**: DeMarker crosses upward through the lower level.
  - **Short**: DeMarker crosses downward through the upper level.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: None by default.
- **Filters**: None.

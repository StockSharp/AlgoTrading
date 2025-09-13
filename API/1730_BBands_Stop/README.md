# BBands Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the BBands Stop indicator derived from Bollinger Bands to follow market trends. When the stop line flips upward, it closes any short position and opens a long one. A downward flip closes long positions and opens short. Parameters control Bollinger period, deviation, risk offset and permissions for entering or exiting longs and shorts.

## Details

- **Entry Criteria**:
  - **Long**: Uptrend stop line is active.
  - **Short**: Downtrend stop line is active.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite stop signal.
- **Stops**: Trailing stop from Bollinger Bands.
- **Filters**: None.

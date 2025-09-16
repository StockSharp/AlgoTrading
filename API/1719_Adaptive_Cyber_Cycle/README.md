# Adaptive Cyber Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy utilizes the Adaptive Cyber Cycle oscillator by John Ehlers. It calculates a smoothed price cycle and uses the previous value as a trigger line. A long position is opened when the cycle crosses above the trigger, and a short position is opened on a downward cross.

## Details

- **Entry Criteria**:
  - **Long**: cycle > previous cycle.
  - **Short**: cycle < previous cycle.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal closes and reverses the position.
- **Stops**: None by default; protection can be enabled separately.
- **Default Values**:
  - `Alpha` = 0.07
  - `Candle Type` = 1-minute time frame
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Adaptive Cyber Cycle
  - Stops: Optional
  - Complexity: Moderate
  - Timeframe: Intraday

# Aroon Oscillator Sign Alert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Aroon Oscillator to generate trading signals when the oscillator crosses predefined levels. A long position is opened when the oscillator crosses above the down level (default -50). A short position is opened when it crosses below the up level (default +50). Opposite signals close or reverse the position.

## Details

- **Entry Criteria:**
  - **Long**: Aroon oscillator crosses upward through the down level.
  - **Short**: Aroon oscillator crosses downward through the up level.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal automatically exits or reverses the current position.
- **Stops**: None.
- **Filters**: None.
- **Timeframe**: Default 4-hour candles (configurable).

## Parameters

- `AroonPeriod` – lookback period for the Aroon oscillator (default 9).
- `UpLevel` – upper threshold for sell signals (default +50).
- `DownLevel` – lower threshold for buy signals (default -50).
- `CandleType` – candle timeframe used for calculations (default 4 hours).

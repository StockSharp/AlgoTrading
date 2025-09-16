# Elliott Wave Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies the Elliott Wave Oscillator (EWO) on candle closes. The EWO is calculated as the difference between a fast and a slow Simple Moving Average (default 5 and 35 periods). Trading logic looks for turning points in the oscillator to capture potential trend reversals.

A long position is opened when the oscillator forms a local trough and starts rising. A short position is opened when the oscillator forms a local peak and starts falling. Existing positions are reversed accordingly. Optional percentage based take‑profit and stop‑loss are supported through `StartProtection`.

## Details

- **Indicator**: Elliott Wave Oscillator = SMA(fast) − SMA(slow).
- **Entry Criteria**:
  - **Long**: oscillator value was falling then turns upward.
  - **Short**: oscillator value was rising then turns downward.
- **Long/Short**: Both.
- **Exit Criteria**: Position reverses on opposite signal or exits via stop or take‑profit.
- **Stops**: Percentage stop‑loss and take‑profit.
- **Filters**: None.

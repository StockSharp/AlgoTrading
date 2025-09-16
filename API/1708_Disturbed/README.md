# Disturbed Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This hedging strategy opens simultaneous long and short market orders and manages them based on the current spread. Once price moves by one spread against either side, that position is closed. The remaining position then targets a profit or loss equal to a configurable multiple of the spread.

## Details

- **Entry Criteria**:
  - On start, place both buy and sell market orders.
- **Long/Short**: Both simultaneously.
- **Exit Criteria**:
  - Close the side that loses one spread.
  - Close the remaining side at `gainMultiplier * spread` profit or loss.
- **Stops**: Implicit via spread-based levels.
- **Filters**: None.

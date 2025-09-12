# Range Filter Strategy with ATR TP/SL
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that enters when price crosses the range filter bands and exits using ATR-based take-profit and stop-loss levels.

## Details

- **Entry Criteria**: Price crosses above upper band for long, below lower band for short.
- **Long/Short**: Both.
- **Exit Criteria**: ATR-based take profit or stop loss.
- **Stops**: ATR-based, fixed when trade opens.
- **Default Values**:
  - `RangeFilterLength` = 20
  - `RangeFilterMultiplier` = 1.5
  - `AtrLength` = 14
  - `TakeProfitMultiplier` = 1.5
  - `StopLossMultiplier` = 1.5
- **Filters**: none.
- **Complexity**: medium.
- **Timeframe**: configurable.

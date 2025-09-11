# Random ATR Strategy - Bybit
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy generates a deterministic random signal based on recent price ranges and the current date. It enters long when the signal is 1 and short when it is 0. Risk management uses ATR-based stop loss and take profit levels.

## Details

- **Entry Criteria**:
  - **Long**: random signal equals 1.
  - **Short**: random signal equals 0.
- **Long/Short**: Both sides.
- **Exit Criteria**: stop loss or take profit.
- **Stops**: `SlAtrRatio * ATR` for stop loss, take profit at `SlAtrRatio * TpSlRatio * ATR`.
- **Default Values**:
  - `AtrLength` = 14
  - `SlAtrRatio` = 3
  - `TpSlRatio` = 1
- **Filters**: none.
- **Complexity**: simple.
- **Timeframe**: configurable.

# Stop Loss Take Profit Money Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters long when a short-term SMA crosses above a long-term SMA and shorts on the opposite crossover. Positions are closed once profit or loss reaches predefined money amounts.

## Details

- **Entry Criteria**: SMA(14) crosses SMA(28)
- **Long/Short**: Both
- **Exit Criteria**: Profit or loss in money hits the target
- **Stops**: Yes
- **Default Values**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `TakeProfitMoney` = 200
  - `StopLossMoney` = 100

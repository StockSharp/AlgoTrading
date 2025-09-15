# Cm Manual Grid
[Русский](README_ru.md) | [中文](README_cn.md)

Cm Manual Grid places a configurable grid of stop and limit orders around the current price. Each new order increases volume by a fixed increment. The strategy can close long or short positions separately when profit targets are reached and includes a trailing profit mechanism.

## Details

- **Type**: grid trading with pending orders
- **Orders**: Buy Stop, Sell Stop, Buy Limit, Sell Limit
- **Volume**: start volume `Lot` with increment `LotPlus`
- **Profit Management**:
  - `CloseProfitB` closes long positions
  - `CloseProfitS` closes short positions
  - `ProfitClose` closes all positions
  - `TralStart` and `TralClose` manage trailing profit
- **Defaults**:
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 steps
  - `StepBuyStop` = 10
  - `StepSellStop` = 10
  - `StepBuyLimit` = 10
  - `StepSellLimit` = 10
  - `Lot` = 0.1
  - `LotPlus` = 0.1
  - `CloseProfitB` = 10
  - `CloseProfitS` = 10
  - `ProfitClose` = 10
  - `TralStart` = 10
  - `TralClose` = 5

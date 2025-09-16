# Cm Manual Grid
[Русский](README_ru.md) | [English](README.md)

Cm Manual Grid 在当前价格周围放置可配置的止损和限价订单网格。每个新订单的数量按固定值增加。策略可以在达到目标利润时分别关闭多头或空头仓位，并包含跟踪利润机制。

## 详情

- **类型**：网格交易，使用挂单
- **订单**：Buy Stop、Sell Stop、Buy Limit、Sell Limit
- **数量**：初始 `Lot`，增量 `LotPlus`
- **利润管理**：
  - `CloseProfitB` 关闭多头仓位
  - `CloseProfitS` 关闭空头仓位
  - `ProfitClose` 关闭所有仓位
  - `TralStart` 和 `TralClose` 控制跟踪利润
- **默认值**：
  - `OrdersBuyStop` = 5
  - `OrdersSellStop` = 5
  - `OrdersBuyLimit` = 5
  - `OrdersSellLimit` = 5
  - `FirstLevel` = 5 步
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

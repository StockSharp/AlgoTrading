# ColorJFatl Digit ReOpen 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用Jurik移动平均线（JMA）来识别趋势方向。当JMA向上转折时开多并平掉所有空单；当JMA向下转折时开空并平掉所有多单。价格每朝持仓方向移动固定点数时会追加新的仓位，直至达到最大数量。

## 详情

- **入场**：
  - JMA向上转折 → 开多并平空。
  - JMA向下转折 → 开空并平多。
- **加仓**：
  - 首次建仓后，每当价格按`PriceStep`点向持仓方向移动时加仓，直到达到`MaxPositions`。
- **离场**：
  - JMA反向转折时平掉当前仓位。
- **参数**：
  - `JmaLength` – JMA周期。
  - `PriceStep` – 加仓所需的点数移动。
  - `MaxPositions` – 同方向最大仓位数。
  - `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – 控制各操作。
  - `CandleType` – 计算所用时间框架。
- **指标**：Jurik Moving Average。
- **类型**：趋势跟随。
- **周期**：默认4小时。

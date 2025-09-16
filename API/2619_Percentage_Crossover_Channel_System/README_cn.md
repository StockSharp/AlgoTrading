# 百分比通道系统策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 专家顾问 *Exp_PercentageCrossoverChannel_System* 的直接移植版。策略追踪价格与自定义“Percentage Crossover Channel”指标的互动，当蜡烛在突破后重新回到通道内部时做出反应。所有信号流程均按照 StockSharp 的高层 API 重新实现。

## 交易逻辑

1. **指标构建**
   - Percentage Crossover Channel 构建一条跟随价格的自适应中线，中线的偏离速度不会超过设定百分比 (`Percent`)。
   - 上轨与下轨在中线上方/下方按相同百分比绘制。
   - 每根收盘的蜡烛都会根据 `Shift` 根之前的通道位置被赋予颜色：
     - 颜色 `3` / `4`：收盘价位于上轨之上（分别表示阴/阳线）。
     - 颜色 `0` / `1`：收盘价位于下轨之下（分别表示阴/阳线）。
     - 颜色 `2`：收盘价位于通道内部。

2. **入场与出场**
   - 评估最近 `SignalBar` 根蜡烛及其前一根蜡烛，完全复刻 MQL 中的 `CopyBuffer` 调用。
   - **多头序列**（`olderColor > 2`）：市场最近在通道上方收盘。如果最新蜡烛重新回到通道内（`recentColor < 3`），则：
     - 在启用 `SellPositionsClose` 时平掉所有空头仓位。
     - 在仓位为空且 `BuyPositionsOpen` 启用的情况下开多。
   - **空头序列**（`olderColor < 2`）：市场最近在通道下方收盘。如果最新蜡烛回到通道内（`recentColor > 1`），则：
     - 在启用 `BuyPositionsClose` 时平掉所有多头仓位。
     - 在仓位为空且 `SellPositionsOpen` 启用的情况下开空。
   - 策略因此等待“突破 + 回踩”组合后顺势入场。

3. **风险控制**
   - 可选的止损与止盈以价格步长为单位设置，并基于蜡烛最高价/最低价触发。
   - 一旦保护性指令触发，策略立即离场，并在同一根蜡烛内忽略新的进场信号，模拟原始 EA 中经纪商侧止损优先执行的行为。

## 参数说明

| 参数 | 说明 |
| ---- | ---- |
| `Percent` | 通道宽度，单位为百分比，对应 MQL 指标参数。 |
| `Shift` | 用于比较突破的回溯蜡烛数量。 |
| `SignalBar` | 信号评估所使用的偏移量（以蜡烛数计），默认值 1 表示上一根蜡烛。 |
| `BuyPositionsOpen` / `SellPositionsOpen` | 是否允许开多/开空。 |
| `BuyPositionsClose` / `SellPositionsClose` | 是否允许在出现反向信号时强制平仓。 |
| `StopLoss` | 止损距离，以 `Security.PriceStep` 的倍数表示。0 表示不使用。 |
| `TakeProfit` | 止盈距离，同样以价格步长表示。0 表示不使用。 |
| `CandleType` | 使用的蜡烛类型（时间框架），默认对应四小时周期 `PERIOD_H4`。 |

## 实现细节

- 由于 StockSharp 没有自带 Percentage Crossover Channel 指标，算法在策略内部重写，包括中线递推、上下轨以及颜色判定，步骤与 MQL 代码一致。
- 持仓管理遵循原始的 `BuyPositionOpen` / `SellPositionOpen` 等辅助函数：先平掉反向仓位，再尝试开新仓，并在存在反向持仓时跳过信号。
- MQL 附件中的资金管理、`Deviation` 滑点参数以及不同保证金模式的手数计算未被移植。请通过 StockSharp 的常规属性或外部平台配置下单量。
- 止损/止盈被解释为“价格步长”的倍数，对应 MetaTrader 中的“点数”。请确认所连接的标的提供有效的 `PriceStep`。

## 使用建议

- 若希望复制 MetaTrader 的表现，请在高质量的四小时数据上运行该策略；也可以调整 `CandleType` 用于日内交易测试。
   
- 信号需要至少两根带有效颜色信息的已完成蜡烛，因此初始化时应确保历史数据不少于 `Shift + SignalBar + 1` 根。

- `Percent` 对策略灵敏度影响显著：数值越小，通道越贴近价格、交易越频繁；数值越大，则仅关注强势突破。

- 策略始终保持单仓结构，只会在多头、空头或空仓三种状态之间切换，进行组合风控时需考虑这一点。


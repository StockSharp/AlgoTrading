# 均线与价格交叉策略

## 概述

该目录提供了 `MQL/50198` 中两个 MetaTrader 5 示例的 C# 移植版本：

* **`MovingAveragePriceCrossStrategy`** —— 简洁的均线与收盘价交叉策略，仅持有一笔仓位。
* **`MovingAverageMartingaleStrategy`** —— 在保持相同信号逻辑的同时，引入亏损后加倍的马丁加仓机制。

两种策略都基于 StockSharp 高级 API，使用蜡烛线订阅来计算信号，并以 MetaTrader “点” 的方式设置止损和止盈距离。

## 文件

| 文件 | 说明 |
| --- | --- |
| `CS/MovingAveragePriceCrossStrategy.cs` | 固定仓位与静态保护的基础均线交叉实现。 |
| `CS/MovingAverageMartingaleStrategy.cs` | 亏损后放大仓位和保护距离的马丁版本。 |

## 交易逻辑

### MovingAveragePriceCrossStrategy

1. 订阅指定周期的蜡烛线，并计算简单移动平均线 (`SMA`)。
2. 仅在蜡烛线收盘后触发逻辑，与 MT5 专家顾问保持一致。
3. 使用最近两根已完成蜡烛的收盘价与 SMA 位置判断交叉：
   * **做空**：当 SMA 从下方穿越到价格上方（价格跌破均线）。
   * **做多**：当 SMA 从上方穿越到价格下方（价格突破均线）。
4. 若当前无持仓，则按信号提交市价单。
5. 通过 `StartProtection` 将以点为单位的止损、止盈转换为绝对价格偏移并自动下单。

### MovingAverageMartingaleStrategy

1. 与基础策略共用蜡烛数据与 SMA 信号判断。
2. 在仓位平仓后记录实现盈亏，保存最近一次交易结果。
3. 当出现新的交叉信号且没有持仓时：
   * 最近一笔为**亏损**：按 `VolumeMultiplier` 放大下一笔仓位（不超过 `MaxVolume`），并将止损、止盈距离按 `TargetMultiplier` 扩大。
   * 最近一笔为**盈利**：将仓位和保护距离恢复为初始值。
4. 在发送市价单之前，根据当前距离重新调用 `StartProtection`。
5. 始终只维持一笔仓位，等同于原版专家的 `PositionsTotal()` 检查。

## 风险控制

* 止损与止盈距离以 MT5 “点”为单位，根据 `PriceStep` 自动转换为价格偏移；对于 3/5 位小数的外汇品种会额外乘以 10。
* 马丁版本对距离倍数做了上限限制，避免无限扩张。
* 下单量会根据 `VolumeStep`、`MinVolume` 以及可选的 `MaxVolume` 自动对齐，确保报单有效。

## 参数

### 通用参数

| 参数 | 策略 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | 两者 | `1 minute` | 用于计算信号的蜡烛类型。 |
| `MaPeriod` | 两者 | `50` | 简单移动平均线的周期。 |

### MovingAveragePriceCrossStrategy

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | `1` | 每次下单的基础仓位，会按交易品种步长对齐。 |
| `TakeProfitPoints` | `150` | 止盈距离（点），0 表示不设置。 |
| `StopLossPoints` | `150` | 止损距离（点），0 表示不设置。 |

### MovingAverageMartingaleStrategy

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `StartingVolume` | `1` | 盈利后恢复的初始仓位。 |
| `MaxVolume` | `5` | 在加倍后允许的最大仓位。 |
| `TakeProfitPoints` | `100` | 初始止盈距离（点）。 |
| `StopLossPoints` | `300` | 初始止损距离（点）。 |
| `VolumeMultiplier` | `2` | 亏损后用于放大下一笔仓位的倍数。 |
| `TargetMultiplier` | `2` | 亏损后用于放大止损与止盈距离的倍数。 |

## 使用提示

* MT5 的“点”通常等于一个 `PriceStep`，策略会自动识别是否需要乘以 10 以匹配外汇报价。
* 策略在持仓期间忽略所有新信号，与 `PositionsTotal()` 判断相一致。
* 可以在 StockSharp 设计器中对暴露的参数进行优化，以复现 MT5 的参数调优流程。

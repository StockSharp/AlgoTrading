# GoldWarrior02b 策略
[English](README.md) | [Русский](README_ru.md)

本策略是 MetaTrader 4 专家顾问 *GoldWarrior02b*（目录 `MQL/7694`）的 StockSharp 移植版本。
它结合了商品通道指数（CCI）、自定义“日冲量”指标的替代方案以及 ZigZag 摆动结构，
并且只在每个 15 分钟周期结束前的最后几秒内评估开仓信号。该实现遵循 StockSharp
的净持仓模式，因此不会复制原版中的多层对冲网格。

## 核心特点

- **冲量过滤**：用烛线开盘价与收盘价之差的移动平均值代替 `DayImpuls` 指标，数值按价格最小变动单位归一化。
- **ZigZag 结构**：重建最近的高低点，用于判别市场方向。
- **时间过滤**：仅当当前 K 线在第 14、29、44 或 59 分钟的最后 15 秒内收盘时才允许进场。
- **风险控制**：包含止损、止盈、可选的追踪止损以及以账户货币计量的总盈利目标。
  默认参数与原始 EA 保持一致（1000 点止损、150 点止盈、追踪止损关闭）。
- **净头寸管理**：StockSharp 只维护净头寸，未实现 MT4 中的加仓与对冲分层逻辑。

## 交易流程

### 信号准备

1. 订阅 `CandleType` 指定的 K 线（默认 5 分钟）。
2. 使用 `ImpulsePeriod`（默认 21）计算 CCI 与冲量平均值。
3. 当价格偏离超过 `ZigZagDeviation` 点并满足深度/回溯条件时更新 ZigZag 方向。
4. 保存指标的上一笔数值，以模拟 EA 中的 `cci0/cci1` 与 `imp/nimp` 缓冲区。

### 入场条件

仅在当前无仓位、距离上次交易已超过 15 秒且 `AllowEntryTime` 返回 `true` 时才会评估信号。

**做多：**
- 最新 ZigZag 转折指向下方（新低低于前低）。
- 满足以下任一条件：
  - 当前 CCI 高于上一笔，上一笔 CCI < -50，当前 CCI < -30，冲量由负转正且上一笔冲量为负；
  - 当前 CCI < -200，上一笔 CCI 更低，冲量低于 `ImpulseBuyThreshold` 且强于上一笔冲量。

**做空：**
- 最新 ZigZag 转折指向上方（新高高于前高）。
- 满足以下任一条件：
  - 当前 CCI 低于上一笔，上一笔 CCI > 50，当前 CCI > 30，冲量由正转负且上一笔冲量为正；
  - 当前 CCI > 200，上一笔 CCI 更高，冲量高于 `ImpulseSellThreshold` 且弱于上一笔冲量。

若上一笔冲量值位于 `ImpulseSellThreshold` 与 `ImpulseBuyThreshold` 之间，则忽略信号。

### 离场管理

- **止损**：当价格相对入场价反向移动 `StopLossPoints`（默认 1000 点）时离场。
- **止盈**：价格达到 `TakeProfitPoints`（150 点）后平仓。
- **追踪止损**：可选项；当浮盈达到 `TrailingStopPoints + TrailingStepPoints` 时启动，随后按 `TrailingStopPoints` 跟踪。
- **盈利目标**：根据 `PriceStep` 与 `StepPrice` 将浮动盈亏换算为账户货币，超过 `ProfitTarget`（默认 300）时立即离场。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `BaseVolume` | 开仓手数。 | `0.1` |
| `StopLossPoints` | 止损距离（点）。 | `1000` |
| `TakeProfitPoints` | 止盈距离（点）。 | `150` |
| `TrailingStopPoints` | 追踪止损距离（0 表示关闭）。 | `0` |
| `TrailingStepPoints` | 激活追踪止损所需的额外距离。 | `0` |
| `ImpulsePeriod` | CCI 与冲量指标的周期。 | `21` |
| `ZigZagDepth` | ZigZag 转折之间的最少 K 线数。 | `12` |
| `ZigZagDeviation` | 确认新转折所需的最小价格偏移。 | `5` |
| `ZigZagBackstep` | 相邻转折之间的最少 K 线数。 | `3` |
| `ProfitTarget` | 浮盈目标（账户货币）。 | `300` |
| `ImpulseSellThreshold` | 触发做空所需的冲量下限。 | `-30` |
| `ImpulseBuyThreshold` | 触发做多允许的冲量上限。 | `30` |
| `CandleType` | 使用的时间周期。 | `5 分钟` |

## 与原版 EA 的差异

- MT4 脚本通过 `GlobalVariableSet` 及多级挂单实现加仓与对冲。本移植仅保留时间节流，不实现网格。
- 根据高层 API 要求，订单全部使用市价指令（`BuyMarket`、`SellMarket`）。
- `DayImpuls` 指标通过开盘/收盘差值的移动平均进行近似，当前与上一笔读数分别替代 `imp`、`nimp` 两个缓冲区。

## 使用建议

- 请将 `CandleType` 设置为优化时使用的时间周期（原策略为 M5）。
- 确认标的提供 `PriceStep` 与 `StepPrice` 信息，以便正确换算点值。
- 回测时加入合理的滑点与延迟，验证收盘窗口过滤器的可执行性。

## 免责声明

本策略仅用于学习和演示，请在实盘前进行充分的历史及前向测试。

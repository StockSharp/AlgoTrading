# Master Mind Triple WPR 策略

## 概述
- 移植自 MetaTrader 4 专家顾问 `MasterMind3CE`（位于 `MQL/8458`）。
- 同时使用 26、27、29、30 四个周期的 Williams %R 指标，筛选出极端超买/超卖状态。
- 属于均值回归思路：在剧烈下跌后买入，在过度上涨后做空。
- 提供以品种价格跳动（PriceStep）为单位的止损、止盈与可选移动止损设置。
- 支持任意时间框架，默认处理 15 分钟 K 线。

## 交易逻辑
### 指标
- `WilliamsR(26)` —— 极短周期振荡指标。
- `WilliamsR(27)` —— 较快周期，提供确认信号。
- `WilliamsR(29)` —— 中等周期，用于平滑信号。
- `WilliamsR(30)` —— 较慢周期，要求多个回溯窗口都处于极值。

四个指标都完成计算后才会参与判断。订阅只处理收盘完结的 K 线，以对应原始顾问 `TradeAtCloseBar = true` 的行为。

### 入场条件
- **多头入场**：四个 Williams %R 均小于等于 `OversoldLevel`（默认 `-99.99`）。策略目标净头寸为 `TradeVolume`。若存在空头，会在一次市价单中平掉空单并翻多。
- **空头入场**：四个 Williams %R 均大于等于 `OverboughtLevel`（默认 `-0.01`）。策略目标净头寸为 `TradeVolume` 的空头，若持有多单则先行平仓后翻空。

### 离场条件
- **信号反向**：当持有多单且出现空头条件（或反之）时，策略会平仓或反手。
- **止损**：可选的价格跳动距离，基于平均入场价设置。若当根 K 线的最高/最低触及该价位，将以市价离场。
- **止盈**：可选的价格跳动目标，到价后平仓。
- **移动止损**：价格在有利方向运行 `TrailingStopSteps + TrailingStepSteps` 后启动，止损始终跟随在当前收盘价下方/上方 `TrailingStopSteps` 的距离，且仅当提升幅度不少于 `TrailingStepSteps` 时才更新。

## 风险管理
所有距离均以合约的价格跳动（PriceStep）表示。例如 `PriceStep = 0.0001` 且 `StopLossSteps = 2000` 时，止损距离为 0.2000。若同方向加仓，策略会按加权方式重新计算平均入场价，保证止损/止盈位置保持一致。当 `TrailingStopSteps` 与 `TrailingStepSteps` 中任一为 0 时，移动止损功能关闭。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 目标净头寸规模（手数或合约数）。 | `1` |
| `OversoldLevel` | 判断超卖的 Williams %R 阈值。 | `-99.99` |
| `OverboughtLevel` | 判断超买的 Williams %R 阈值。 | `-0.01` |
| `StopLossSteps` | 以 PriceStep 表示的止损距离，设为 `0` 则禁用。 | `2000` |
| `TakeProfitSteps` | 以 PriceStep 表示的止盈距离，设为 `0` 则禁用。 | `0` |
| `TrailingStopSteps` | 以 PriceStep 表示的移动止损距离，需要 `TrailingStepSteps > 0`。 | `0` |
| `TrailingStepSteps` | 每次更新移动止损所需的最小改进幅度（以 PriceStep 计）。 | `1` |
| `CandleType` | 策略订阅的 K 线类型/周期。 | `TimeFrame(15m)` |

## 移植说明
- 原策略中的警报、声音、日志文件与邮件功能被移除，可改用 StockSharp 的日志系统。
- 原顾问允许在 K 线收盘前下单，本移植版沿用默认的“收盘交易”模式，只在完结 K 线后处理。
- MetaTrader 专用的魔术号、重复下单、图表对象等功能在 StockSharp 中无对应实现，因此省略。
- 止损/止盈的调整由策略内部管理，而非持续修改订单；每根 K 线都会重新评估是否触发。

## 使用建议
1. 选择需要交易的品种与时间框架，尽量与原策略所使用的图表一致。
2. 若品种波动特征不同，请调整阈值或风险控制参数。
3. 启动策略后，它会订阅指定的 K 线，监控 Williams %R 极值并按照规则管理仓位。

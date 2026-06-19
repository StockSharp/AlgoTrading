# Macd Pattern Trader DoubleTop Strategy

## 概述

本策略移植自 MetaTrader 4 顾问 **MacdPatternTraderv04cb**。它在选定的时间框架上监控 MACD 主线，寻找看跌双顶和看涨双底结构。
当第二个波峰/波谷比第一个更弱、且 MACD 仍处于正/负触发阈值之外时，策略会顺势开仓以捕捉反转。
止损和止盈分别固定为 100 与 300 点，与原版 EA 完全一致。

## 交易规则

1. 订阅配置的蜡烛序列（默认 30 分钟）并按参数计算 MACD 主线，默认周期为 5、13、1。
2. 保存最近三个已完成的 MACD 数值。若 MACD 高于 `TriggerLevel` 且形成局部高点后回落，则进入看跌预警；
   如果下一次高点低于记录的高点，并且 MACD 仍高于触发阈值，则立即市价卖出。
3. 当 MACD 低于 `-TriggerLevel` 并出现更高的第二个低点时，生成对称的看涨信号并市价买入。
4. 一旦 MACD 返回到 `[-TriggerLevel, TriggerLevel]` 区间内，就清空已记录的峰值和谷值，避免在动能不足时继续寻找形态。
5. 下单量由 `TradeVolume` 控制；当需要反向开仓时，会先覆盖相反方向的持仓，然后建立新的头寸。
6. 在 `OnStarted` 中调用一次 `StartProtection`，将 100 点止损和 300 点止盈转换为价格步长并交由平台托管。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `FastPeriod` | MACD 快速 EMA 周期。 |
| `SlowPeriod` | MACD 慢速 EMA 周期。 |
| `SignalPeriod` | MACD 信号线平滑周期。 |
| `TriggerLevel` | 激活双顶/双底检测所需的 MACD 绝对值。 |
| `StopLossPips` | 止损距离（点数，默认 100）。 |
| `TakeProfitPips` | 止盈距离（点数，默认 300）。 |
| `TradeVolume` | 每次开仓的基准手数。 |
| `CandleType` | 用于计算指标的蜡烛类型。 |

## 说明

- 止损与止盈在传递给 `StartProtection` 之前会从点数转换为交易品种的最小步长，保证行为与原始 EA 保持一致。
- 代码中的注释全部采用英文，满足仓库的统一要求。

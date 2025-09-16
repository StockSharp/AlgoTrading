# Universal MA Cross 策略

## 概述
**Universal MA Cross Strategy** 将原始的 MQL5 专家顾问 “UniversalMACrossEA” 移植到 StockSharp 的高级策略框架。该策略比较一条快速与一条慢速移动平均线，并允许为每条均线分别设置周期、平滑方法和价格类型。附加参数可控制信号确认方式、是否立即反向、风险管理以及允许交易的时间窗口。

## 交易逻辑
### 指标计算
* 在所选的 K 线序列上计算两条移动平均线。每条均线都可以拥有自己的周期、平滑方法（SMA、EMA、SMMA 或 LWMA）以及价格类型（收盘价、开盘价、最高价、最低价、中价、典型价或加权价）。
* **MinCrossDistance** 要求在产生交叉的那个 K 线上，两条均线之间的距离至少达到指定的价差。
* 启用 **ConfirmedOnEntry** 时，交叉信号使用前两个已经完成的 K 线进行验证（对应原始 EA 中的索引 2 与 1）。关闭该选项时，当前完成的 K 线与上一根 K 线比较，以模拟 MQL 中的“实时”模式。
* **ReverseCondition** 会交换做多与做空的规则，不需要修改任何指标设置。

### 入场规则
1. 当快速均线向上穿越慢速均线，且差值不少于 **MinCrossDistance** 时开多；向下穿越且差值足够时开空。
2. 若启用了 **StopAndReverse**，在收到相反信号时会先平掉当前仓位，再考虑新的订单。
3. 当 **OneEntryPerBar** 为 `true` 时，策略会记录最近一次入场的 K 线时间，在同一根 K 线内拒绝再次开仓。
4. 每笔交易的下单数量由 **Volume** 参数决定。

### 仓位管理
* 止损与止盈以绝对价格距离表示，在 **PureSar** 模式下会被忽略，这与原专家中的 “Pure SAR” 设置一致。
* 当价格相对入场价运行 **TrailingStop + TrailingStep** 之后启动移动止损；之后每当价格额外前进至少 **TrailingStep**，止损就会向盈利方向收紧 **TrailingStop** 的距离。在 **PureSar** 模式下不会启用移动止损。
* 每根已完成的 K 线都会检查保护水平。如果该 K 线的高低区间触及止损或止盈，仓位会以市价单平仓。

### 交易时段过滤
* 当 **UseHourTrade** 启用时，只在 K 线开盘时间位于 **StartHour** 与 **EndHour**（包含边界）之间时才允许开仓。即使在时段外，移动止损仍会更新，但不会触发新的入场或“止损反手”。

## 参数说明
| 参数 | 说明 |
|------|------|
| `FastMaPeriod`, `SlowMaPeriod` | 快速与慢速移动平均线的周期。 |
| `FastMaType`, `SlowMaType` | 均线类型：简单、指数、平滑（RMA）或线性加权。 |
| `FastPriceType`, `SlowPriceType` | 输入到均线的价格类型。 |
| `StopLoss`, `TakeProfit` | 以价格单位表示的止损与止盈，设为 0 表示关闭。 |
| `TrailingStop`, `TrailingStep` | 移动止损的偏移量，以及再次移动前所需的额外行程。 |
| `MinCrossDistance` | 交叉时两条均线之间的最小距离。 |
| `ReverseCondition` | 交换多空条件。 |
| `ConfirmedOnEntry` | 仅使用已完成的 K 线确认信号。 |
| `OneEntryPerBar` | 每根 K 线最多只允许一次入场。 |
| `StopAndReverse` | 在相反信号出现时先平仓再反向开仓。 |
| `PureSar` | 关闭止损、止盈与移动止损逻辑。 |
| `UseHourTrade`, `StartHour`, `EndHour` | 交易时间过滤（0–23 小时制）。 |
| `Volume` | 每次下单的数量。 |
| `CandleType` | 订阅并用于计算的 K 线类型。 |

## 转换说明
* 由于 StockSharp 的高级策略基于完成的 K 线运行，保护性订单通过检测 K 线的最高价与最低价来模拟，从而在不使用低级 API 的情况下再现原始 EA 的行为。
* 移动止损的调整与 MQL 实现一致：只有在价格运行了 **TrailingStop + TrailingStep** 之后才会移动止损。
* 按照要求，此次转换未提供 Python 版本。

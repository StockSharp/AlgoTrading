# AMA Trader 2 策略

## 概述
AMA Trader 2 策略复刻了 Vladimir Karputov 的 MetaTrader 专家顾问。策略使用考夫曼自适应均线 (AMA) 识别趋势方向，并结合 RSI 振荡指标确认极值。当收盘价高于 AMA 且 RSI 落入超卖区域时加仓做多；当收盘价低于 AMA 且 RSI 进入超买区域时加仓做空。所有加仓订单都使用固定手数，并可通过风险参数（最大加仓次数、最小价格间距、止损/止盈/跟踪止损等）进行约束。

## 市场假设
- **适用品种**：以点差较小的外汇或差价合约为主，也可用于任何流动性较高且允许网格/加仓的标的。
- **数据频率**：基于完整的时间 K 线运行，时间框架可通过 `CandleType` 设置（默认 1 分钟）。
- **交易时段**：可选的日内时间过滤器；启用 `UseTimeWindow` 后，仅在 `StartTime`–`EndTime`（UTC）之间交易。

## 指标
1. **Kaufman Adaptive Moving Average (AMA)**：通过可调的快/慢平滑常数和均线长度确定趋势方向。
2. **Relative Strength Index (RSI)**：确认动量极值；`StepLength` 控制需要检查的 RSI 最近值数量（0 被视为 1，与原始 EA 一致）。

## 交易逻辑
1. 仅处理已完成的 K 线，并确保策略处于在线且允许交易的状态。
2. 如果启用了时间过滤器，则在窗口之外跳过信号。
3. 更新 RSI 队列，同时根据当前仓位更新跟踪止损位置。
4. **多头条件**：收盘价高于 AMA 且最近的 RSI 值中至少有一个低于 `RsiLevelDown`。若当前多头浮亏，会先发送一笔加仓单，再执行常规入场。**空头条件** 相反 (`RsiLevelUp`)。
5. 入场前检查 `MaxPositions`、`MinStep` 和 `OnlyOnePosition`。如果 `CloseOpposite` 为真，会先平掉对向仓位，待成交确认后再考虑新的方向交易。
6. 每次入场都可以附加固定的止损、止盈以及由 `TrailingActivation`、`TrailingDistance`、`TrailingStep` 控制的跟踪止损。

## 风险管理
- **固定手数**：所有订单使用 `LotSize` 指定的数量。
- **加仓限制**：`MaxPositions` 限制同方向的最大加仓次数。
- **价格间距**：`MinStep` 要求相邻入场价格之间的最小距离，避免在同一价位密集建仓。
- **防护措施**：可选的止损、止盈和跟踪止损与原版 EA 的保护逻辑一致。
- **对向仓位处理**：`CloseOpposite` 启用后会先平掉对向仓，`OnlyOnePosition` 则完全禁止同时持有双向头寸。

## 参数
| 参数 | 说明 |
|------|------|
| `CandleType` | 指标和信号使用的 K 线类型/时间框架。 |
| `LotSize` | 每次市价单的成交量。 |
| `RsiLength` | RSI 平滑周期。 |
| `StepLength` | 需要检查的 RSI 最近值数量（0→1）。 |
| `RsiLevelUp` | 空头信号的 RSI 超买阈值。 |
| `RsiLevelDown` | 多头信号的 RSI 超卖阈值。 |
| `AmaLength` | AMA 平滑长度。 |
| `AmaFastPeriod` | AMA 快速平滑常数。 |
| `AmaSlowPeriod` | AMA 慢速平滑常数。 |
| `StopLoss` | 固定止损距离（价格单位，0 表示禁用）。 |
| `TakeProfit` | 固定止盈距离（价格单位，0 表示禁用）。 |
| `TrailingActivation` | 启动跟踪止损所需的浮盈。 |
| `TrailingDistance` | 跟踪止损与价格之间的距离。 |
| `TrailingStep` | 每次收紧跟踪止损所需的最小改进幅度。 |
| `MaxPositions` | 同方向最大加仓次数（0 表示无限制）。 |
| `MinStep` | 相邻入场的最小价格距离。 |
| `CloseOpposite` | 入场前是否先平掉对向仓位。 |
| `OnlyOnePosition` | 是否禁止在持仓状态下再次开仓。 |
| `UseTimeWindow` | 是否启用日内时间过滤。 |
| `StartTime` | 交易开始时间（UTC）。 |
| `EndTime` | 交易结束时间（UTC）。 |

## 实现细节
- 仅使用 StockSharp 的高层 API：通过 `SubscribeCandles` 订阅 K 线，利用 `.Bind` 直接获得指标结果，避免访问禁止的指标缓冲区。
- 独立维护多头与空头的持仓量和均价，从而在不调用聚合查询的情况下判断当前浮盈/浮亏。
- 跟踪止损通过更新策略级别的 stop-loss 距离实现，无需手动管理订单生命周期。
- 记录每个方向最近一次成交所在的 K 线，避免同一根 K 线上重复开仓。

## 与 MetaTrader 版本的差异
- 省略了 magic number、允许滑点、冻结区间等 MetaTrader 专用参数；这些功能由 StockSharp 环境或上层基础设施处理。
- 止损/止盈价格使用收盘价推算，而非逐笔报价，这是 K 线驱动策略在 StockSharp 中的标准方式。
- 原策略基于账户风险百分比动态计算手数，本移植版本保持固定 `LotSize`，风险管理交由账户层处理。

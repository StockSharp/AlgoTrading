# MA Crossover 多时间框架策略

该策略复现了 MetaTrader 4 平台上的 **MA Crossover** 专家顾问。它比较来自两个时间框架的移动平均线：当快速均线上穿慢速均线时开多单，下穿时开空单。可以通过参数控制允许的交易方向、可交易时段以及权益保护。止损、止盈和跟踪止损在策略内部执行，以模拟 MQL 版本中的“隐藏”保护逻辑。

## 交易逻辑

1. 订阅两个蜡烛序列（当前与上一时间框架），并计算指定类型的移动平均线。
2. 在比较之前对移动平均线应用配置的柱数偏移。
3. 忽略未完成的蜡烛，仅在两个指标都形成后才处理信号。
4. 超出设定的日期/时间窗口或触发权益保护时不进行交易。
5. 当出现多头交叉时：
   - 若 `ClosePositionsOnCross = true`，先平掉空头头寸。
   - 如允许做多则开多仓。
6. 当出现空头交叉时：
   - 若 `ClosePositionsOnCross = true`，先平掉多头头寸。
   - 如允许做空则开空仓。
7. 根据入场价的百分比执行止损、止盈与跟踪止损。

## 参数

| 参数 | 说明 |
|------|------|
| `AllowedDirection` | 交易方向限制（`LongOnly`、`ShortOnly`、`LongAndShort`）。 |
| `ClosePositionsOnCross` | 新信号出现前是否先平掉反向仓位。 |
| `MaType` | 移动平均线类型（`Simple`、`Exponential`、`Smoothed`、`Weighted`）。 |
| `CurrentMaPeriod` | 快速均线周期。 |
| `PreviousPeriodAddition` | 慢速均线的额外周期数（`PreviousMaPeriod = CurrentMaPeriod + addition`）。 |
| `CurrentShift` / `PreviousShift` | 对移动平均线值应用的柱数偏移。 |
| `CurrentCandleType` / `PreviousCandleType` | 计算快慢均线所需的蜡烛数据类型。 |
| `StopLossPercent` | 以入场价百分比表示的止损距离（内部执行）。 |
| `TrailingStopPercent` | 按最佳价格计算的跟踪止损百分比。 |
| `TakeProfitPercent` | 以入场价百分比表示的止盈距离（内部执行）。 |
| `StartDay` / `EndDay` | 允许交易的星期范围。 |
| `StartTime` / `EndTime` | 每日可开仓的时间窗口。 |
| `ClosePositionsOnMinEquity` | 触发权益保护时是否平掉所有仓位。 |
| `MinimumEquityPercent` | 相对于初始权益的最低允许百分比。 |

## 风险控制

- 止损、止盈和跟踪止损均在策略内部以市价单执行，不会在交易所挂出保护单。
- `MinimumEquityPercent` 会记录启动时的投资组合价值，当权益跌破阈值时触发强制平仓。
- 使用 `Strategy.Volume` 设置下单数量，默认值为 `1`。

## 使用说明

- 请确保连接器能够提供两个时间框架的蜡烛数据。
- 即使两个时间框架相同，策略仍会建立两条订阅以保持逻辑对称。
- 所有风险退出都在蜡烛收盘时通过市价单执行，因此外部不可见。
- 为了兼容 StockSharp 的高层 API，原始 MQL 程序中的加仓与对冲选项未实现，权益保护也基于组合价值而非保证金。


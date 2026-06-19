# 可调节移动均线策略

该策略使用 StockSharp 高级 API 复刻 MetaTrader 中的“Adjustable Moving Average”专家顾问。两条相同算法但不同周期的均线跟踪它们之间的距离。当快速均线以不少于设定阈值的幅度穿越慢速均线时，策略会平掉相反方向的仓位，并在允许的情况下顺势开仓。会话过滤、保护性止损/止盈以及可选的移动止损让移植版本保持与原程序一致的灵活性。

## 交易逻辑

- 快速均线与慢速均线使用同一种算法。较小的周期自动作为快速均线，较大的周期作为慢速均线。
- 只有当两条均线都已经形成，且它们的绝对距离超过按价格步长换算后的 `MinGapPoints` 阈值时才会生成信号。
- 当快速均线高于慢速均线并超过阈值时，内部信号转为多头；当慢速均线高于快速均线并超过阈值时，信号转为空头。
- 信号翻转时，如果当前时间处于交易会话之内或启用了 `CloseOutsideSession`，策略会平掉已有持仓。之后根据 `Mode`（仅做多、仅做空或双向）以及手数模式开立新仓。
- 每根完成的K线都会检查保护条件：
  - 止损与止盈距离以品种点数表示，并与当前K线的最高/最低价比较。
  - 当价格向有利方向移动至少 `TrailStopPoints` 点时启动移动止损。只有在会话允许或启用了 `TrailOutsideSession` 时才会继续收紧止损；一旦止损被拉起，即使会话结束也保持有效。

## 仓位规模

- 当 `EnableAutoLot = false` 时使用 `FixedLot` 作为下单数量，并自动匹配交易所的步长、最小和最大手数限制。
- 当 `EnableAutoLot = true` 时，根据组合市值近似计算手数：`(PortfolioValue / 10,000) * LotPer10kFreeMargin`，并四舍五入到一位小数，再按交易所限制调整。

## 参数

| 名称 | 类型 / 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | `TimeFrame` = 5 分钟 | 计算均线所用的K线周期。 |
| `FastPeriod` | `int` = 3 | 快速均线周期，必须与 `SlowPeriod` 不同。 |
| `SlowPeriod` | `int` = 9 | 慢速均线周期，必须与 `FastPeriod` 不同。 |
| `MaMethod` | `MovingAverageMethod` = Exponential | 均线算法（Simple、Exponential、Smoothed、Weighted）。 |
| `MinGapPoints` | `decimal` = 3 | 快慢均线之间的最小距离（点）。根据价格步长换算。 |
| `StopLossPoints` | `decimal` = 0 | 止损距离（点），0 表示关闭。 |
| `TakeProfitPoints` | `decimal` = 0 | 止盈距离（点），0 表示关闭。 |
| `TrailStopPoints` | `decimal` = 0 | 移动止损距离（点），0 表示关闭。 |
| `Mode` | `EntryMode` = Both | 允许的建仓方向（Both、BuyOnly、SellOnly）。 |
| `SessionStart` | `TimeSpan` = 00:00 | 会话开始时间（平台时间）。 |
| `SessionEnd` | `TimeSpan` = 23:59 | 会话结束时间，可通过 `SessionEnd < SessionStart` 支持跨夜交易。 |
| `CloseOutsideSession` | `bool` = true | 为 true 时，即使不在会话时间内也会平掉相反仓位。 |
| `TrailOutsideSession` | `bool` = true | 为 true 时，会话结束后仍继续更新移动止损。 |
| `FixedLot` | `decimal` = 0.1 | 关闭自动手数时的下单数量。 |
| `EnableAutoLot` | `bool` = false | 是否启用基于组合市值的自动手数。 |
| `LotPer10kFreeMargin` | `decimal` = 1 | 自动手数模式下，每 10,000 资金对应的手数。 |
| `MaxSlippage` | `int` = 3 | 从 MQL 保留的占位参数；StockSharp 的市价单不支持直接设置滑点。 |
| `TradeComment` | `string` = "AdjustableMovingAverageEA" | 下单时写入日志的注释文本。 |

## 说明

- 原始 EA 通过修改订单来设置止损、止盈和移动止损，移植版本改为在收盘K线中检测突破并用相反方向的市价单平仓。
- 由于无法获得 MetaTrader 的 `AccountFreeMargin()`，此处使用组合市值近似自由保证金。
- 如果品种没有有效的 `PriceStep`，涉及点数的计算（间距、止损、移动止损）将保持不激活状态。

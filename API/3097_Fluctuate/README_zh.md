# Fluctuate 策略

**Fluctuate Strategy** 是 MetaTrader 顾问 "Fluctuate" 的 StockSharp 版本。整套逻辑基于高层 API：通过 `SubscribeCandles` 监听蜡烛，使用 `BuyMarket` / `SellMarket` 完成入场，并通过止损单布置补仓订单。为了模拟 MetaTrader 的对冲账户，策略内部分别维护多头与空头敞口，而在 StockSharp 中显示的始终是净头寸。

## 核心思路

1. 每根新蜡烛收盘时比较最近两根的收盘价。若当前收盘高于上一根，则市价买入；若更低，则市价卖出；相等时不操作。
2. 每笔成交都会设置固定点数的止损与止盈，同时记录成交价以及该笔交易带来的新增手数。
3. 成交后会在距离成交价 `StepPips`（再加一个极小的价差缓冲）的位置挂出**反向**止损单。其手数来源于上一笔成交和 `LotCoefficient`，在 `MultiplyLotCoefficient = true` 时则按总敞口计算。
4. 当止损单被触发时，旧的挂单会被取消，内部敞口统计随之更新，并立刻在反方向放置新的补仓止损单，复现原 EA 的网格 / 马丁循环。
5. 当价格朝有利方向至少运行 `TrailingStopPips + TrailingStepPips` 点时，保护性移动止损被激活，将止损紧跟在 `TrailingStopPips` 的距离之外，对应 MQL 版本的阶梯式 trailing stop。

## 工作流程

- **信号判定。** 仅处理 `CandleStates.Finished` 的收盘数据，超出 `[StartHour, EndHour)` 的交易窗口或触及权益保护时暂停建仓。
- **初始仓位。** `PositionSizingMode = FixedVolume` 时使用固定手数；`RiskPercent` 模式下，按照权益的百分比风险除以止损产生的货币亏损来计算手数，点值与货币的换算依赖 `PriceStep` 与 `StepPrice`。
- **敞口跟踪。** 分别维护多头与空头的手数、均价以及自建仓以来的最高/最低价，从而在净值账户中模拟对冲持仓。
- **补仓止损。** 每次成交后重新计算下一张止损单的手数：
  - `MultiplyLotCoefficient = false` 时为 `LastVolume × LotCoefficient`；
  - `true` 时按当前总绝对敞口乘以 `LotCoefficient`；
  - 手数会按交易所限制（步长/最小/最大）归一化，并在超过 `MaxTotalVolume` 或达到 `MaxPositions`（持仓方向数量 + 活动止损单）时被拒绝。
- **止盈与权益保护。** 通过 `PriceStep` / `StepPrice` 把价差换算为货币，得到整体未实现盈亏；当达到 `ProfitTarget` 时，立即平掉所有仓位并取消挂单。若权益低于初始余额的 `MinEquityPercent`%，策略仅允许平仓，暂停新建仓。
- **移动止损。** 多头记录入场后的最高价，若超过均价 `TrailingStopPips + TrailingStepPips` 点，则把止损上移到最高价减去 `TrailingStopPips`；空头逻辑对称。

## 风险控制

- **止损 / 止盈。** 将相应点值设为 `0` 可关闭功能。新增仓位时会重新计算对应方向的整体水平。
- **MaxPositions。** 统计当前开启的方向（多/空各算 1）以及有效的补仓止损单，达到上限后不再提交新的止损单。
- **MaxTotalVolume。** 限制当前绝对持仓量与补仓订单手数之和。
- **CloseAllAtStart。** 启动策略前可选择性地平掉所有仓位并取消挂单。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于产生信号的主时间框架。 | 1 分钟 K 线 |
| `StopLossPips` | 入场价到止损的距离（点）。`0` 表示关闭。 | 50 |
| `TakeProfitPips` | 入场价到止盈的距离（点）。`0` 表示关闭。 | 50 |
| `TrailingStopPips` | 移动止损的基础距离（点）。需与 `TrailingStepPips` 同时为正。 | 5 |
| `TrailingStepPips` | 每次推进移动止损所需的额外盈利（点）。 | 5 |
| `StepPips` | 最近一笔成交到反向补仓止损的距离（点）。 | 30 |
| `LotCoefficient` | 补仓止损单的手数系数。 | 2.0 |
| `MultiplyLotCoefficient` | 若为 `true`，对总敞口而非上一笔成交应用系数。 | `false` |
| `MaxPositions` | 同时允许的方向数量 + 活动止损单。 | 9 |
| `MaxTotalVolume` | 绝对持仓量加补仓止损手数的上限。 | 50 |
| `ProfitTarget` | 未实现利润（账户货币）达到该值时全部平仓。`0` 表示关闭。 | 50 |
| `MinEquityPercent` | 权益低于初始余额百分比阈值时暂停建仓。 | 30 |
| `CloseAllAtStart` | 启动时是否先清空仓位和挂单。 | `false` |
| `StartHour` | 交易窗口起始小时（含）。 | 10 |
| `EndHour` | 交易窗口结束小时（不含）。 | 20 |
| `PositionSizingMode` | `FixedVolume` 为固定手数，`RiskPercent` 为按权益百分比。 | `FixedVolume` |
| `VolumeOrRisk` | 固定手数或风险百分比，取决于 `PositionSizingMode`。 | 1.0 |

## 实现细节

- 止损单价格额外加入最小价差（优先使用 `PriceStep`），以贴近 MetaTrader 对 freeze-level 的要求。
- 每次成交后，旧的补仓止损单都会被取消，与原 EA 的行为一致。
- 由于 StockSharp 账户为净额模式，对冲持仓通过内部变量模拟，实际提交给经纪商的是净手数。
- `RiskPercent` 模式需要品种提供有效的 `PriceStep` 与 `StepPrice`。

## 使用建议

1. 选择与原 EA 回测时间框架相匹配的 `CandleType`（常见为 M5 或 M15）。
2. 确认交易所手数限制允许放置补仓单；若归一化后的手数为 0，策略将停止扩展网格。
3. 在 `RiskPercent` 模式下务必保持投资组合权益数据最新，否则会退回到固定手数模式。
4. 如需额外保护，可配合 `StartProtection()` 启用 StockSharp 内置的账户级风控。

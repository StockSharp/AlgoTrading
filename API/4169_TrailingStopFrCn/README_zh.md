# TrailingStopFrCn 策略

## 概述

`TrailingStopFrCnStrategy` 是 MetaTrader 智能交易系统 **TrailingStopFrCn.mq4** 的 StockSharp 版本。原始脚本负责管理已有持仓的止损，支持固定距离、比尔·威廉姆斯分形以及最近 K 线高低点三种跟踪方式。本策略在保持原有灵活性的同时，使用 StockSharp 的高级 API 订阅蜡烛图与 Level 1 行情，监控当前净持仓，并自动更新保护性止损订单。

策略只关注风险控制，不会创建新的开仓订单。它跟踪 `Strategy.Security` 的净持仓，当持仓方向反转时取消旧的止损单，并为全部净持仓发送一个新的止损单，从而复现原始 EA 的管理逻辑。

## 跟踪逻辑

1. **固定距离跟踪**：当 `TrailingStopPips` 大于零时，策略与原版 EA 的 `TrailingStop` 参数一致。多头止损放在 `bestBid - distance`，空头止损放在 `bestAsk + distance`，其中 `distance = TrailingStopPips × 点值`。
2. **分形跟踪**：当 `TrailingStopPips = 0` 且 `TrailingMode = Fractals` 时，策略会识别五根 K 线的比尔·威廉姆斯分形。每根完成的蜡烛都会存入缓存，当历史足够时，向前数两根的蜡烛会被评估为潜在分形。最近一次且与当前价格至少相差 `MinStopDistancePips` 的分形价位将成为新的止损候选。
3. **K 线高低点跟踪**：当 `TrailingStopPips = 0` 且 `TrailingMode = Candles` 时，策略会在最近 99 根已完成蜡烛中查找第一个与当前价格间隔不少于 `MinStopDistancePips` 的低点（多头）或高点（空头）。

候选止损算出后，策略继续执行与原始 MQL 代码一致的保护规则：

- **OnlyProfit**：只有当新的止损价位能够锁定利润时才会移动止损（多头止损高于入场价，空头止损低于入场价）。
- **OnlyWithoutLoss**：一旦当前止损已经保证不亏损，便停止继续跟踪。
- 止损只会沿着有利方向移动：多头向上、空头向下。

由于 StockSharp 以净仓方式管理头寸，止损订单的数量等于 `Math.Abs(Position)`，不会分拆到单独的交易票据。

## 参数

| 参数 | 说明 |
|------|------|
| `OnlyProfit` | 仅在新的止损价能够锁定利润时移动止损，对应原 EA 的 `OnlyProfit`。 |
| `OnlyWithoutLoss` | 当当前止损已处于入场价或更有利位置时停止继续跟踪，对应原 EA 的 `OnlyWithoutLoss`。 |
| `TrailingStopPips` | 固定跟踪距离（以点数表示）。设为 0 时启用分形或 K 线高低点模式。 |
| `MinStopDistancePips` | 当前价格与止损之间的最小距离（点数）。用于模拟经纪商的 `MODE_STOPLEVEL` 限制。 |
| `TrailingMode` | 当 `TrailingStopPips = 0` 时选择跟踪来源：`Fractals`（五根 K 线分形）或 `Candles`（最近高低点）。 |
| `CandleType` | 用于计算分形或摆动点的蜡烛类型，默认为 1 小时时间框架。 |

## 行为说明

- 策略订阅 Level 1 行情以获得最优买价与最优卖价。固定距离跟踪会在价格更新时立即反应；分形/蜡烛模式在新蜡烛完成时更新候选止损。
- 当持仓方向发生变化时，现有的止损订单会先被取消，再根据新方向重新下单。
- 如果暂时找不到合适的止损候选（例如历史数据不足），策略会保持当前止损不变。
- 当经纪商没有最小止损距离要求时，可将 `MinStopDistancePips` 设置为 0。

## 与 MetaTrader 版本的差异

- StockSharp 只跟踪净持仓，因此不会区分 MetaTrader 中的单个票据；一个止损订单覆盖全部仓位。
- 原脚本中的 `Magic` 过滤不再需要，策略天然只作用于自身的证券对象。
- 跟踪更新由完成的蜡烛和 Level 1 行情驱动，而不是每秒轮询一次。
- 不再绘制 MetaTrader 中的图形对象；若需要可使用 StockSharp 自带的图表工具。

## 使用建议

1. 将本策略与任何负责开仓的策略同时运行，只要目标证券上出现净持仓，TrailingStopFrCn 就会自动附加止损。
2. 根据需要调整 `CandleType`。较高周期带来更平滑的止损轨迹，较低周期响应更快。
3. 按经纪商的最小止损限制校准 `MinStopDistancePips`。过低的数值可能导致订单被拒绝。
4. 在历史回测时请确保蜡烛数据与 Level 1 行情可用，否则止损逻辑可能无法触发。

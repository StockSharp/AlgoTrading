# FiveMinutesScalpingEA v1.1（StockSharp 版本）

## 概述
**FiveMinutesScalpingEaV11Strategy** 是 MetaTrader 4 智能交易系统 *5MinutesScalpingEA v1.1* 的 StockSharp 高级 API 移植版本。策略保留了原始 EA 的核心思路：结合两条 Hull 移动平均线、一个 Fisher 变换动量过滤器、ATR 突破判定以及一个趋势 Fisher 过滤器，在 5 分钟周期上捕捉短线机会。所有信号都在蜡烛收盘后计算，策略只维护单一净头寸。

## 指标结构
| 组件 | StockSharp 实现 | 作用 |
|------|-----------------|------|
| `i1` Hull MA | `HullMovingAverage`，周期 `Period1`（默认 30） | 通过 Hull 斜率识别快速趋势方向。 |
| `i2` Hull MA | `HullMovingAverage`，周期 `Period2`（默认 50） | 验证更慢的趋势方向，正常模式下两条 Hull 必须一致。 |
| `i3` Fisher 动量 | `FisherTransform`，周期 `Period3` | 作为动量振荡器，数值 > 0 看多，< 0 看空。 |
| `i4` ATR 突破 | `AverageTrueRange`，周期 `Period4`，结合三根蜡烛比较 | 当当前高点/低点超过前两根高点/低点并超出一个 ATR 时给出突破信号。 |
| `i5` Fisher 趋势 | `FisherTransform`，周期 `Period5` | 平滑趋势过滤，与原 EA 的趋势柱状图类似。 |

每个指标都会存储历史数值，以便读取 `IndicatorShift` 根之前的值，从而对应 MQL4 中的 `IndicatorsShift` 参数。可以通过 `UseIndicator1`~`UseIndicator5` 单独启用/禁用各个过滤器。

## 交易逻辑
1. 订阅 `CandleType` 定义的蜡烛序列（默认 5 分钟）。
2. 在每根蜡烛收盘时更新所有指标，并读取 `IndicatorShift` 根之前的蜡烛数据进行判定。
3. **Normal 模式**：所有启用的过滤器都显示多头条件时开多；全部显示空头条件时开空。
4. **Reverse 模式**：交换多头和空头条件。
5. 当出现新信号时，更新内部 `_lastSignal` 状态。如果 `CloseOnSignal` 为真，将在进场前平掉相反方向的持仓。
6. `UseTimeFilter` 可以限制交易时间段，处理方式与原始 EA 一致（支持跨日区间 `[StartHour, EndHour)`）。

## 风险控制
- **止损/止盈**：若启用，会按 `StopLossPips`、`TakeProfitPips` 的距离设置虚拟止损止盈并在每根蜡烛检查触发情况。
- **追踪止损**：启用 `UseTrailingStop` 时，维护一个跟踪锚点，价格每推进 `TrailingStepPips` 就把止损移动到距离锚点 `TrailingStopPips` 的位置。
- **保本**：启用 `UseBreakEven` 且价格达到 `BreakEvenPips + BreakEvenAfterPips` 时，将止损提升到距离入场 `BreakEvenPips` 的水平。
- 所有平仓都通过市场单 (`SellMarket` / `BuyMarket`) 关闭全部仓位。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | M5 | 信号周期。 |
| `IndicatorShift` | 1 | 指标回溯的完成蜡烛数量。 |
| `SignalMode` | Normal | 正常或反向信号模式。 |
| `UseIndicator1`..`UseIndicator5` | true | 是否启用各个过滤器。 |
| `Period1`..`Period5` | 30, 50, 10, 14, 18 | Hull、Fisher、ATR 的周期。 |
| `PriceMode3` | HighLow | 保留的兼容参数。当前实现始终使用默认蜡烛价格驱动 Fisher。 |
| `CloseOnSignal` | false | 新信号出现时是否先平掉反向仓位。 |
| `UseTimeFilter`、`StartHour`、`EndHour` | false, 0, 0 | 交易时段限制。 |
| `UseTakeProfit`、`TakeProfitPips` | true, 10 | 止盈设置。 |
| `UseStopLoss`、`StopLossPips` | true, 10 | 止损设置。 |
| `UseTrailingStop`、`TrailingStopPips`、`TrailingStepPips` | false, 1, 1 | 追踪止损设置。 |
| `UseBreakEven`、`BreakEvenPips`、`BreakEvenAfterPips` | false, 4, 2 | 保本策略设置。 |
| `TradeVolume` | 0.01 | 开仓量。 |

## 与原始 EA 的差异
- 未实现篮子平仓 (`UseBasketClose`, `CloseInProfit`, `CloseInLoss`)，因为该策略仅维护一个净头寸。
- 未实现自动仓位管理和点差检查，需要在外部控制下单量和滑点。
- `PriceMode3` 仅用于兼容，当前 Fisher 指标仍使用默认价格源；如需其它价格，需要扩展指示器。
- 风险管理基于收盘数据执行，与 `IndicatorsShift ≥ 1` 时的 EA 行为一致。

## 使用建议
1. 在流动性好、点差小的品种上运行（原策略针对 EUR/USD M5）。
2. 根据资金管理调整 `TradeVolume`。
3. 可适当调节各指标周期或关闭部分过滤器以适应自己的交易风格。
4. 配合时间过滤器规避流动性低的时段。
5. 在实盘之前请先在 StockSharp 测试器中验证策略效果。

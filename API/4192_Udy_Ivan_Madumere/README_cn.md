# Udy Ivan Madumere 策略

## 概览
Udy Ivan Madumere 是一个典型的日内时段策略：每天只在指定小时评估一次信号，并且始终保持最多一笔持仓。StockSharp 版本完整复制了这一行为，通过订阅蜡烛序列、对比历史开盘价，并在目标蜡烛收盘后立即执行交易指令，从而在 API 环境中再现 MetaTrader 4 的运行方式。

策略要点：

- 仅在 `TradeHour` 指定的小时检查一次信号，不会重复开仓。
- 使用 `Open[FirstLookback]` 与 `Open[SecondLookback]` 之间的差值决定做空或做多。
- 继承原版的自动加仓梯度，当 `UseAutoVolume = true` 时按照账户余额调整基础手数。
- 为多头与空头分别设置止损、止盈，并且只对空头头寸启用追踪止损。
- 每笔交易都会受到 `MaxHoldingHours` 的时间限制，即使止盈/止损尚未触发也会强制平仓。

## 执行流程
1. 订阅参数 `CandleType` 对应的蜡烛序列，并忽略所有未完成的蜡烛，确保逻辑与 MetaTrader 相同。
2. 维护最近若干根蜡烛的开盘价：
   - `Open[FirstLookback] - Open[SecondLookback]` 大于空头阈值 → 做空。
   - `Open[SecondLookback] - Open[FirstLookback]` 大于多头阈值 → 做多。
3. 当最新蜡烛的开盘时间等于 `TradeHour` 时：
   - 若空头差值超过 `ShortDeltaPoints * PriceStep`，发送市价卖单；
   - 否则若多头差值超过 `LongDeltaPoints * PriceStep`，发送市价买单。
4. 成功下单后将 `canTrade` 置为 `false`，并在该小时之后重新允许下一次交易，从而确保一天只有一次尝试。
5. 开仓瞬间重新计算手数：
   - 自动手数模式会根据账户余额在预设阶梯中选择合适的基础手数；
   - 如果当前余额低于上一笔交易时的快照，则将结果乘以 `BigLotMultiplier`，模拟原策略的「放大手数」机制。
6. 持仓期间在每根完成的蜡烛上执行退出逻辑：
   - 根据入场价评估止盈和止损；
   - 空头同时检查追踪止损（`TrailingStopPoints`）；
   - 若持仓时间超过 `MaxHoldingHours`，立即按市价平仓。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H1` | 用于生成信号的蜡烛类型。 |
| `TradeHour` | `int` | `18` | 每日评估信号的小时（0-23）。 |
| `FirstLookback` | `int` | `6` | `Open[FirstLookback]` 引用的历史蜡烛数量。 |
| `SecondLookback` | `int` | `2` | `Open[SecondLookback]` 引用的历史蜡烛数量。 |
| `LongDeltaPoints` | `decimal` | `6` | 做多所需的最小开盘价差（MetaTrader “点”）。 |
| `ShortDeltaPoints` | `decimal` | `21` | 做空所需的最小开盘价差。 |
| `TakeProfitLongPoints` | `decimal` | `39` | 多头止盈距离（点）。 |
| `StopLossLongPoints` | `decimal` | `147` | 多头止损距离（点）。 |
| `TakeProfitShortPoints` | `decimal` | `200` | 空头止盈距离（点）。 |
| `StopLossShortPoints` | `decimal` | `267` | 空头止损距离（点）。 |
| `TrailingStopPoints` | `decimal` | `30` | 空头追踪止损的距离（点）。 |
| `BaseVolume` | `decimal` | `0.01` | 自动调整之前的初始手数。 |
| `UseAutoVolume` | `bool` | `true` | 是否启用余额梯度自动手数。 |
| `BigLotMultiplier` | `decimal` | `1` | 当余额下降时额外放大的倍数。 |
| `MaxHoldingHours` | `int` | `504` | 最大持仓时间（小时）。0 表示不限制。 |

## 实现细节
- 所有阈值均以价格最小变动单位 `PriceStep` 进行换算，以适配不同报价精度。
- 开盘价缓冲区仅保存 `max(FirstLookback, SecondLookback) + 1` 个值，既满足信号需求又避免多余的内存开销。
- 空头追踪止损会记录最佳价格，并在出现更优的候选值时才更新止损位置。
- 账户余额基于 `Portfolio.CurrentValue`（若无则退化到 `BeginValue`），确保回测和实时连接中的表现一致。
- 代码注释全部使用英文，方便审核与后续维护。

## 使用建议
- 将 `CandleType` 设置为与原 MT4 策略相同的周期（默认假定为 1 小时）。
- 如果交易品种采用微型合约，请调整 `BaseVolume` 以及自动梯度，以匹配交易所/经纪商的合约规模。
- 结合 `DrawCandles` 与 `DrawOwnTrades` 进行图表展示，可直观确认每天仅在目标时刻出现一次交易。

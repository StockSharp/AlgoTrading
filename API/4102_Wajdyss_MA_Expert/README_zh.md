# Wajdyss MA Expert 策略

## 概览
**Wajdyss MA Expert 策略** 是 MetaTrader 4 专家顾问 “wajdyss MA expert v3” 的 C# 版本。它比较两条可分别设置周期、算法、偏移以及价格类型的移动平均线。当快线向上穿越慢线时开多单，向下穿越时开空单。移植版本完整保留了原始 EA 的资金管理、可选的反向仓位自动平仓以及日末/周末强制平仓功能。

## 交易逻辑
1. 订阅所选的 `CandleType`（默认 15 分钟）并按各自的 `MovingAverageMethod` 与 `PriceSource` 计算快慢移动平均线。
2. 仅对已完成的 K 线保存指标数值。当快线（包含其 `Shift` 设置）在上一根收盘价上方且在两根之前位于慢线下方时触发看多信号；反之触发看空信号。
3. 新仓位之间必须间隔至少一个完整周期，与 MT4 版本使用全局变量控制节奏的逻辑一致。
4. 启用 **AutoCloseOpposite** 时，会先撤销挂单并在一笔市价单中反转仓位，订单数量包含当前相反方向的持仓，以确保立即翻仓。
5. 应用日内与周五收盘过滤器。到达 `DailyCloseHour`/`DailyCloseMinute` 或 `FridayCloseHour`/`FridayCloseMinute` 后会平掉所有仓位并禁止新交易，直到下一交易日。

## 风险与资金管理
- **TakeProfitPips**、**StopLossPips** 与 **TrailingStopPips** 以完整点数表示，程序根据合约的最小报价步长转换为价格并通过 `StartProtection` 触发市价止盈止损/跟踪止损，复制原版的保护机制。
- **UseMoneyManagement** 复刻 MT4 的手数计算：`volume = (account_balance / BalanceReference) * InitialVolume`，并根据交易所的最小增量、最小/最大手数自动调整。
- 关闭资金管理后，直接使用 **InitialVolume** 作为下单数量。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `FastPeriod` | `int` | `10` | 快速移动平均线的周期。 |
| `FastShift` | `int` | `0` | 快速均线在比较时的偏移条数。 |
| `FastMethod` | `MovingAverageMethod` | `Ema` | 快速均线的算法（`Sma`、`Ema`、`Smma`、`Lwma`）。 |
| `FastPriceType` | `PriceSource` | `Close` | 快速均线使用的价格（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 |
| `SlowPeriod` | `int` | `20` | 慢速移动平均线的周期。 |
| `SlowShift` | `int` | `0` | 慢速均线的偏移条数。 |
| `SlowMethod` | `MovingAverageMethod` | `Ema` | 慢速均线的算法。 |
| `SlowPriceType` | `PriceSource` | `Close` | 慢速均线使用的价格类型。 |
| `TakeProfitPips` | `decimal` | `100` | 止盈距离（点），`0` 表示禁用。 |
| `StopLossPips` | `decimal` | `50` | 止损距离（点），`0` 表示禁用。 |
| `TrailingStopPips` | `decimal` | `0` | 跟踪止损距离（点），`0` 表示禁用。 |
| `AutoCloseOpposite` | `bool` | `true` | 新信号出现前是否自动平掉反向仓位。 |
| `InitialVolume` | `decimal` | `0.1` | 资金管理前的基础下单手数。 |
| `UseMoneyManagement` | `bool` | `true` | 是否启用按余额缩放手数。 |
| `BalanceReference` | `decimal` | `1000` | 计算动态手数时的余额分母。 |
| `DailyCloseHour` | `int` | `23` | 每日强制平仓的小时（0-23）。 |
| `DailyCloseMinute` | `int` | `45` | 每日强制平仓的分钟。 |
| `FridayCloseHour` | `int` | `22` | 周五停止交易的小时（0-23）。 |
| `FridayCloseMinute` | `int` | `45` | 周五停止交易的分钟。 |
| `CandleType` | `DataType` | `15m` 时间框架 | 用于指标计算与冷却计时的 K 线类型。 |

## 说明
- 策略完全基于 StockSharp 的高级 API：使用 `SubscribeCandles` 处理 K 线、指标绑定计算移动平均线，并借助 `StartProtection` 管理止盈止损及跟踪止损。
- 平仓操作采用市价单，模拟 MT4 EA 中立即关闭反向挂单与持仓的方式。
- 本目录仅包含 C# 实现，不提供 Python 版本。

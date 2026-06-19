# RandomT 策略

## 概述
该策略是 MetaTrader 4 专家顾问 “RandomT” 的 StockSharp 移植版。原始 EA 在 ZigZag 摆动与已确认的分形同时出现时触发，并通过 MACD 主线与信号线的比较来过滤信号。移植后的版本沿用了相同思路：监控可配置数量的K线（`BarWatch`），确认五根K线构成的分形就是最近的摆动极值，然后在同一历史K线上比较 MACD 主线与信号线。

## 交易逻辑
- 在所选时间框架（`CandleType`）的每根完成K线上更新滚动窗口并计算 MACD 指标。
- 回看 `Shift` 根K线，检测该位置是否形成上、下分形（左右各两根K线参与判断）。
- 使用 `BarWatch` 长度的窗口验证该分形是否也是局部 ZigZag 极值：高点必须是该窗口内的最高价，低点必须是最低价。
- 做空条件：分形为顶部且同一K线上 MACD 主线高于信号线；做多条件：分形为底部且主线低于信号线。
- 当出现信号时，策略提交一笔市场单，在开仓前会自动抵消可能存在的反向持仓。

## 跟踪止损管理
- 仅在 `UseTrailingProfit` 为真且浮动收益（通过 `PriceStep` 和 `StepPrice` 换算）超过 `MinProfit` 时启用跟踪止损。
- 跟踪距离以点数表示。当 `AutoStopLevel` 为真时使用 `StartStopLevelPoints`，否则使用 `StopLevelPoints`。
- 多头的止损跟随 `ClosePrice - 距离`，空头的止损跟随 `ClosePrice + 距离`。若K线触碰该价位，则通过市场单退出。

## 参数
| 参数 | 说明 |
|------|------|
| `TradeVolume` | 每笔交易使用的基础手数。 |
| `BarWatch` | 用于验证分形是否为 ZigZag 极值的K线数量。 |
| `Shift` | 回溯检查信号的K线数量，经典分形通常使用 2。 |
| `UseTrailingProfit` | 是否启用跟踪止损。 |
| `AutoStopLevel` | 使用 `StartStopLevelPoints` 作为跟踪距离。 |
| `StartStopLevelPoints` | 备选的跟踪距离（点数）。 |
| `StopLevelPoints` | 默认的跟踪距离（点数）。 |
| `MinProfit` | 启用跟踪止损所需的最低浮动收益（账户货币）。 |
| `CandleType` | 用于构建K线和计算指标的时间框架。 |
| `MacdFastLength` | MACD 快速 EMA 的周期。 |
| `MacdSlowLength` | MACD 慢速 EMA 的周期。 |
| `MacdSignalLength` | MACD 信号 EMA 的周期。 |

## 说明
- 策略在内部计算分形（左右各两根K线）并复用结果进行 ZigZag 验证，从而贴近 MQL 中对指标缓冲区的读取方式。
- ZigZag 确认通过检查 `BarWatch` 范围内的最高价和最低价来实现，无需直接调用 MetaTrader 指标，便于在 StockSharp 中保持确定性。
- 跟踪止损的收益计算依赖于品种的 `PriceStep` 与 `StepPrice`，在实盘运行前请确认它们的取值正确。

# EuroSurge Simplified 策略

## 概述
- 将 MetaTrader 4 智能交易系统 **“EuroSurge Simplified”** 迁移到 StockSharp 高层 API。
- 仅处理收盘 K 线，通过 MA、RSI、MACD、布林带与随机指标的组合来筛选信号。
- 强制执行可配置的交易冷却时间，并以价格步长设置止盈与止损。
- 提供固定手数、账户余额百分比、账户权益百分比三种仓位管理模式。

## 信号逻辑
1. **均线趋势**（可选）：20 周期快线需高于/低于可配置的慢线。
2. **RSI 滤网**（可选）：RSI 小于多头阈值允许做多，大于空头阈值允许做空。
3. **MACD 确认**（可选）：MACD 主线需高于/低于信号线。
4. **布林带过滤**（可选）：收盘价向下/向上突破布林带下轨/上轨。
5. **随机指标过滤**（可选）：%K 与 %D 同时低于 50（多头）或高于 50（空头）。

所有启用的条件均满足后才会下单。若存在反向仓位，会先平仓再开新仓，与原始 EA 的替换逻辑保持一致。

## 风险控制
- 止盈与止损距离以价格步长（MetaTrader 的 “点”）定义。
- 进场后立即通过 `SetStopLoss` 和 `SetTakeProfit` 设置保护单。
- 自上次成交以来未达到设定的分钟间隔前不会重新交易。

## 仓位管理
- **FixedSize**：直接使用 `FixedVolume` 指定的手数。
- **BalancePercent**：按照账户初始余额的百分比估算资金，并用最新收盘价换算手数。
- **EquityPercent**：同上，但基于当前权益计算。
- 最终手数会按交易品种的最小/最大手数限制进行裁剪，并对齐到交易步长。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `TradeSizeType` | 仓位模式（固定、余额百分比、权益百分比）。|
| `FixedVolume` | 固定仓位模式下的手数。|
| `TradeSizePercent` | 百分比仓位模式使用的比例。|
| `TakeProfitPoints` / `StopLossPoints` | 止盈/止损的价格步长。|
| `MinTradeIntervalMinutes` | 连续交易之间的冷却时间。|
| `MaPeriod` | 慢速均线长度（快速均线固定为 20）。|
| `RsiPeriod`, `RsiBuyLevel`, `RsiSellLevel` | RSI 周期与阈值。|
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD 参数。|
| `BollingerLength`, `BollingerWidth` | 布林带周期与宽度。|
| `StochasticLength`, `StochasticK`, `StochasticD` | 随机指标参数。|
| `UseMa`, `UseRsi`, `UseMacd`, `UseBollinger`, `UseStochastic` | 独立开关。|
| `CandleType` | 计算所用的 K 线类型。|

## 与原 EA 的差异
- 原 EA 在下单前对手数做严格校验，移植版通过最小/最大手数以及步长对齐来复现这一行为。
- 止盈止损不再手动计算价格，而是借助 StockSharp 的 `SetStopLoss`/`SetTakeProfit` 将“点数”转换成价格。
- 通过 `BindEx` 接收指标输出，完全避免直接调用 `GetValue`。 

## 使用建议
1. 绑定好账户与交易品种，并通过 `CandleType` 设定时间框架。
2. 根据需求启用或关闭各个指标开关，以还原或精简原策略。
3. 如需减少交易次数可增大 `MinTradeIntervalMinutes`，反之则减小。
4. 确认 `TakeProfitPoints` 与 `StopLossPoints` 符合标的的最小跳动。

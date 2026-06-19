# Iin MA Signal 策略

## 概述
本策略完整复刻了经典的 **Iin MA Signal** MQL5 智能交易系统。它跟踪快慢均线的交叉情况，并按照 `SignalBar` 指定的历史K线索引触发信号，与原始程序通过 `CopyBuffer` 读取指标缓冲区的方式一致。当出现多头交叉时，策略可以开多并选择性平掉已有空单；出现空头交叉时则相反。此外，还可以利用 StockSharp 的仓位保护功能自动附加止损和止盈。

## 交易逻辑
1. 订阅由 `CandleType` 指定的单一K线序列（默认 1 小时K线）。
2. 根据 `FastMaType`/`FastPeriod` 与 `SlowMaType`/`SlowPeriod` 创建两条移动平均线。支持 SMA、EMA、SMMA（RMA）与 LWMA，以覆盖 MQL 版本提供的全部选项。
3. 维护一个滚动窗口保存均线数值，以便在 `SignalBar` 对应的K线上评估交叉情况，从而模拟原策略的缓冲区读取。
4. 当窗口中上一根K线的快线低于慢线，而信号K线的快线穿越到慢线上方且当前趋势并非多头时，判定为多头交叉；反之则判定为空头交叉。
5. 每次确认交叉后更新内部趋势标志，避免重复进场，同时对应 MQL 指标中的 `trend` 变量作用。
6. 当 `IsFormedAndOnlineAndAllowTrading()` 返回 true 时，再根据入场/出场开关发送市场订单。

## 入场规则
- **做多**：在检测到多头交叉且 `AllowLongEntries` 为 true 时触发，当前仓位必须为空或做空。若 `CloseShortOnSignal` 为 true，则会先行平掉持有的空单。
- **做空**：在检测到空头交叉且 `AllowShortEntries` 为 true 时触发，当前仓位必须为空或做多。若 `CloseLongOnSignal` 为 true，则会先平掉持有的多单。

## 出场规则
- 根据 `CloseLongOnSignal` 与 `CloseShortOnSignal` 的设置，反向信号可以强制平仓。
- 当 `StopLossPoints` 或 `TakeProfitPoints` 大于 0 时，策略会调用 `StartProtection`，按绝对价格距离自动设置止损与止盈，并使用市价单执行。

## 参数说明
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于计算的K线数据类型。 | 1 小时时间框架 |
| `FastPeriod` | 快速均线周期。 | 10 |
| `FastMaType` | 快速均线类型（`Sma`、`Ema`、`Smma`、`Lwma`）。 | `Ema` |
| `SlowPeriod` | 慢速均线周期。 | 22 |
| `SlowMaType` | 慢速均线类型（`Sma`、`Ema`、`Smma`、`Lwma`）。 | `Sma` |
| `SignalBar` | 用于判断交叉的已完成K线数量（1 对应 MQL 默认值）。 | 1 |
| `AllowLongEntries` | 是否允许多头入场。 | `true` |
| `AllowShortEntries` | 是否允许空头入场。 | `true` |
| `CloseLongOnSignal` | 触发空头信号时是否平掉多单。 | `true` |
| `CloseShortOnSignal` | 触发多头信号时是否平掉空单。 | `true` |
| `StopLossPoints` | 止损的绝对价格距离（0 表示关闭）。 | 1000 |
| `TakeProfitPoints` | 止盈的绝对价格距离（0 表示关闭）。 | 2000 |

## 实现细节
- 全程使用 StockSharp 高级 API：`SubscribeCandles` 订阅行情，`Bind` 直接将均线数值传入策略，无需手动维护历史数据。
- 通过 `CreateMa` 工厂函数将枚举值映射到对应的均线指标，避免重新实现均线算法。
- 滚动缓冲区只保留 `SignalBar + 2` 个样本，足以比较信号K线与其前一根K线。
- 仅当止损或止盈距离大于 0 时才启动仓位保护，从而与原 MQL 模板中可选的资金管理模块保持一致。
- 代码注释全部为英文，符合仓库要求。

## 使用方法
1. 编译解决方案（`dotnet build AlgoTrading.sln`）以生成新的策略类。
2. 在应用程序中实例化 `IinMaSignalStrategy`，配置参数，并绑定所需的连接器、证券和投资组合，然后启动策略。
3. 如需可视化，可将策略附着到图表上，以显示快慢均线及成交记录。
4. 可根据不同市场优化均线周期、信号K线与风险参数。

## 与原始 MQL 专家的差异
- 本实现依赖高级订阅与指标绑定机制，不再手动读取指标缓冲区。
- `TradeAlgorithms.mqh` 中的资金管理函数由 `StartProtection` 替代，实现相同的止损/止盈自动化。
- 策略默认避免对冲：在相反仓位仍持有时不会开新仓，除非用户禁用相应的平仓开关。
- 图表渲染采用 StockSharp 的辅助方法，并未尝试绘制原指标中的箭头缓冲区。

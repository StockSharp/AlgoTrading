# AnyRange Cloud Tail System Tm Plus 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 高级 API 中重现 **Exp_i-AnyRangeCldTail_System_Tm_Plus.mq5** 专家顾问的行为。它在两个自定义时间之间构建日内区间，等待价格突破该区间，然后按照与 MQL 原版相同的延迟逻辑（`SignalBar` 位移）在突破后的指定根 K 线执行订单。

策略支持双向交易，并暴露了控制突破权限、止损/止盈点数、持仓时间以及指标计算窗口的参数。此外，时间退出机制会在持仓超过设定分钟数后自动平仓，对应原专家顾问中对头寸的巡检逻辑。

## 交易逻辑

1. **区间构建**
   - 使用 `RangeStartTime` 和 `RangeEndTime` 定义计算参考区间的会话窗口。
   - 对于每一个已完成的交易日，策略记录该窗口内的最高价与最低价。如果 `RangeStartTime` 晚于 `RangeEndTime`，窗口会自动跨越午夜，与原始指标一致。
   - 在新的日内区间生成前，将持续使用最近一次完成的区间。

2. **突破识别**
   - 每根已完成 K 线都会与存储的区间比较。
   - 收盘价高于区间上沿的 K 线被赋予与 MQL 指标相同的颜色编码（2 或 3），收盘价低于区间下沿的 K 线被赋予编码 0 或 1，区间内部的 K 线使用编码 4。
   - `SignalBar` 参数控制信号的偏移量：策略检查距离当前 `SignalBar + 1` 根的历史 K 线，并确认更近一根 K 线（`SignalBar`）没有重复相同的颜色。这样可以复现原专家顾问在突破后的下一根 K 线执行订单的时序。

3. **入场**
   - **做多**：当 `AllowBuyEntry` 为真，并且信号 K 线出现多头颜色（2 或 3）且后一根 K 线不再保持该颜色时触发。
   - **做空**：当 `AllowSellEntry` 为真，并且信号 K 线出现空头颜色（0 或 1）且后一根 K 线不再保持该颜色时触发。
   - 如果当前持有反向头寸，会在新的市价单中叠加其仓位数量，实现立即反手，与 `TradeAlgorithms.mqh` 中的辅助函数行为一致。

4. **离场**
   - **反向信号**：若 `AllowBuyExit` 为真，信号 K 线出现空头颜色（0 或 1）时平掉多头；若 `AllowSellExit` 为真，信号 K 线出现多头颜色（2 或 3）时平掉空头。
   - **时间退出**：当 `UseTimeExit` 为真且持仓时间超过 `ExitAfterMinutes` 时自动平仓，对应原专家中基于 `nTime` 的时间限制。
   - **止损/止盈**：`StopLossPoints` 与 `TakeProfitPoints` 以价格步长为单位配置止损/止盈，值为 0 时表示禁用。

5. **风险控制**
   - 所有订单使用 `OrderVolume` 指定的基础手数。若需要反手，系统会自动加上原有仓位数量。
   - 调用 `StartProtection` 后即刻注册止损/止盈保护，与 StockSharp 的 OCO 机制集成。

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|-------|
| `OrderVolume` | 新建仓位的基础手数。 | `0.1` |
| `AllowBuyEntry` | 允许在上沿突破时开多。 | `true` |
| `AllowSellEntry` | 允许在下沿突破时开空。 | `true` |
| `AllowBuyExit` | 允许在下沿突破时平多。 | `true` |
| `AllowSellExit` | 允许在上沿突破时平空。 | `true` |
| `UseTimeExit` | 启用时间退出机制。 | `true` |
| `ExitAfterMinutes` | 持仓时间上限（分钟）。 | `1500` |
| `StopLossPoints` | 止损点数（按价格步长计算），0 表示禁用。 | `1000` |
| `TakeProfitPoints` | 止盈点数（按价格步长计算），0 表示禁用。 | `2000` |
| `SignalBar` | 信号偏移量，对应 MQL 中的 `SignalBar`。 | `1` |
| `RangeLookbackDays` | 搜索已完成区间时回溯的最大天数，设为 0 表示仅使用最近的区间。 | `1` |
| `RangeStartTime` | 区间开始时间（TimeSpan）。 | `02:00` |
| `RangeEndTime` | 区间结束时间（TimeSpan）。 | `07:00` |
| `CandleType` | 用于计算的 K 线类型/周期。 | `30 分钟` |

## 实现要点

- 使用 `SubscribeCandles` + `WhenNew` 处理完成的 K 线，完全对齐原策略基于 `IsNewBar` 的逻辑。
- 区间值保存在轻量结构体中，并通过手动循环计算最大/最小，避免对整个集合使用 LINQ，以满足项目规范。
- 时间退出分别跟踪多头和空头的建仓时间，逻辑与 MQL 版本逐笔检查头寸一致。
- `OrderVolume` 同步写入基类 `Strategy.Volume` 属性，方便在界面中观察和调整。
- 代码包含英文注释，便于再次开发或移植。

## 使用建议

- 请确保行情源提供与 `CandleType` 对齐的 K 线数据；策略基于已完成 K 线判断突破，不应用于未完成或 Tick 级别数据。
- 不同市场的交易时段各异，可根据品种调整 `RangeStartTime`/`RangeEndTime`，选择最能代表震荡区间的时间段。
- 对于价格步长不规则的品种，建议在图表或成交明细中检查实际止损/止盈价格，确认点数换算无误。
- 在更短周期运行策略时，可适当降低 `ExitAfterMinutes`，避免持仓时间过长。

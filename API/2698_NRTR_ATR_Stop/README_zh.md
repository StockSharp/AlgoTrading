# NRTR ATR Stop 策略

## 概述

**NRTR ATR Stop 策略** 是对 MetaTrader 专家顾问 `Exp_NRTR_ATR_STOP_Tm` 的完整移植。系统结合了非重绘趋势反转（NRTR）止损线和平均真实波幅（ATR）过滤器，用于识别主导趋势并动态移动保护线。所有信号都在所选时间框架的收盘价生成，并可通过可配置的已完成 K 线数量进行延迟，以复现原始 EA 的 `SignalBar` 设置。

该策略使用 StockSharp 的高级 API 实现，通过蜡烛订阅、指标绑定以及策略自带的下单辅助方法驱动，因而可以直接在 Designer、Shell、Runner 以及标准 API 环境中运行。

## 交易逻辑

1. **指标计算**
   - 在所选时间框架上计算 ATR，周期由参数控制。
   - 将 ATR 值与系数相乘，得到 NRTR 上下轨。
   - 当前趋势在上一根蜡烛突破对侧 NRTR 水平时发生翻转，同时生成用于入场的箭头信号。
2. **信号延迟**
   - `SignalBarDelay` 参数完全对应 MetaTrader 中的 `SignalBar` 输入，允许延迟若干根完整蜡烛再执行信号，从而获得与原始脚本一致的行为。
3. **入场规则**
   - 当出现看涨 NRTR 反转并且允许做多时开多单。
   - 当出现看跌 NRTR 反转并且允许做空时开空单。
4. **离场规则**
   - 当出现相反方向信号时，若允许，对应方向的持仓立即平仓。
   - 可选的时间过滤器会在交易窗口之外强制平仓并禁止开仓。
   - 止损与止盈以价格步长为单位指定，同时 NRTR 水平会持续跟随趋势收紧保护价位，实现类似追踪止损的效果。

## 风险管理

- **下单量**：使用 `OrderVolume` 参数控制开仓量，与原 EA 一样可以参与优化。
- **止损 / 止盈**：以价格步长（point）为单位设置，与 MetaTrader 版本保持一致。当同时存在手动止损与 NRTR 水平时，策略会选择距离市场更近的一侧，以避免扩大风险。
- **交易时间**：启用 `UseTradingWindow` 后，仅在 `[StartHour:StartMinute, EndHour:EndMinute]` 区间内允许开仓，并在时间窗外立即平仓。时间窗支持跨越午夜。

## 参数

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | 1 | 每次下单的数量。 |
| `StopLossPoints` | 1000 | 止损距离（价格步长）。设置为 `0` 表示关闭。 |
| `TakeProfitPoints` | 2000 | 止盈距离（价格步长）。设置为 `0` 表示关闭。 |
| `BuyPosOpen` / `SellPosOpen` | `true` | 是否允许在 NRTR 反转时开多 / 开空。 |
| `BuyPosClose` / `SellPosClose` | `true` | 是否允许在相反信号出现时平多 / 平空。 |
| `UseTradingWindow` | `true` | 是否启用交易时段过滤。 |
| `StartHour` / `StartMinute` | 0 / 0 | 允许交易的开始时间。 |
| `EndHour` / `EndMinute` | 23 / 59 | 允许交易的结束时间，可设置跨日时段。 |
| `CandleType` | 1 小时蜡烛 | 计算 ATR 与 NRTR 使用的蜡烛类型。 |
| `AtrPeriod` | 20 | ATR 计算周期。 |
| `AtrMultiplier` | 2 | ATR 与 NRTR 结合使用的系数。 |
| `SignalBarDelay` | 1 | 执行信号前等待的完整蜡烛数量。 |

## 说明

- 策略仅在蜡烛收盘时做出决策，避免逐笔级别的差异，并与 StockSharp 的高级架构保持一致。
- 代码中的注释全部为英文，以满足项目要求。
- 根据需求未提供 Python 版本，仅包含 C# 实现。

# RSI MA on RSI Dual 策略

## 概述

RSI MA on RSI Dual 策略在 StockSharp 中重现了 MetaTrader 的 "RSI_MAonRSI_Dual" 专家顾问。策略同时计算快慢两组相对强弱指数 (RSI)，并对每组 RSI 结果使用相同周期的简单移动平均进行平滑。当两条平滑 RSI 线在同一侧穿越中性水平线时触发交易信号。

本实现保留了原始 EA 的核心逻辑，同时提供时间过滤、方向限制以及反转信号等选项。

## 指标

- **快 RSI**：可配置周期的相对强弱指数。
- **慢 RSI**：使用独立周期的相对强弱指数。
- **RSI 上的移动平均**：对快慢两组 RSI 结果应用同一长度的简单移动平均。

所有指标共用同一个价格类型（默认收盘价），平滑后的两条 RSI 曲线会绘制在单独的图表面板上。

## 入场规则

1. 等待当前完成 K 线上的两条平滑 RSI 都形成。
2. **做多条件**：
   - 快线在当前 K 线上穿慢线（当前值大于慢线，上一根 K 线小于慢线）。
   - 两条平滑 RSI 均低于中性水平（默认 50）。
3. **做空条件**：
   - 快线在当前 K 线下穿慢线（当前值小于慢线，上一根 K 线上于慢线）。
   - 两条平滑 RSI 均高于中性水平。
4. 可通过 `ReverseSignals` 参数反转买卖方向。
5. 每根 K 线最多触发一次信号，避免重复下单。

## 仓位管理

- `AllowLong` / `AllowShort` 控制是否允许开多或开空。
- `CloseOpposite` 会在反向开仓前平掉现有头寸。
- `OnlyOnePosition` 限制同一时间最多持有一个仓位。
- 策略使用 `Volume` 指定的固定数量发送市价单。

## 时间过滤

通过 `UseTimeFilter` 控制是否启用交易时段过滤。当启用时，仅在 `SessionStart` 与 `SessionEnd` 之间允许交易，支持跨越午夜的时段。时间依据收到的 K 线所提供的交易所时区进行判断。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 策略分析的 K 线类型。 |
| `FastRsiPeriod` | 快 RSI 的周期。 |
| `SlowRsiPeriod` | 慢 RSI 的周期。 |
| `MaPeriod` | 平滑两条 RSI 的移动平均长度。 |
| `AppliedPrice` | 参与 RSI 计算的价格类型。 |
| `NeutralLevel` | 划分多空区域的 RSI 中性水平。 |
| `AllowLong` / `AllowShort` | 控制多空方向是否允许交易。 |
| `ReverseSignals` | 反转买卖信号方向。 |
| `CloseOpposite` | 开新仓前是否平掉相反仓位。 |
| `OnlyOnePosition` | 是否限制为单一持仓。 |
| `UseTimeFilter` | 是否启用交易时段过滤。 |
| `SessionStart` / `SessionEnd` | 交易窗口的开始与结束时间。 |

## 与原始 EA 的差异

- 未复刻原始 MQL5 代码中的资金管理、止损或移动止损模块。StockSharp 版本仅使用固定手数市价单。
- 移除了所有平台专用的日志与提示，若需要可使用 StockSharp 的日志系统。
- 交易状态跟踪改由 StockSharp 的订单事件处理。

尽管如此，核心的信号生成与方向过滤逻辑与原始专家顾问保持一致。

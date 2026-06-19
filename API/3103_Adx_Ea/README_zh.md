# 3103 — ADX EA (C#)

## 概述
原版 MetaTrader “ADX EA” 通过 ADX 突破、+DI/−DI 交叉、上位周期动量确认以及月度 MACD 滤波来产生信号。C# 版本在 StockSharp 高层 API 上重现了这一多重过滤流程，并订阅三种蜡烛序列：

1. **主周期**（默认 5 分钟）——计算 ADX、线性加权均线、价格结构与成交量过滤。
2. **动量周期**（默认 15 分钟）——提供围绕 100 基准的动量偏离，用于放行或拦截信号。
3. **MACD 周期**（默认 30 天）——模拟 EA 中的月度 MACD 以管理离场。

## 交易逻辑
- **突破模块**（启用时）多头需要：
  - ADX 或 +DI 高于 `EntryLevel`，且 +DI 与 −DI 的差值大于 `MinDirectionalDifference`。
  - 快速 LWMA 位于慢速 LWMA 之上，满足 `Low[2] < High[1]` 的多头形态，以及动量上升（`Momentum[1] > Momentum[2]`）。
  - 上位周期最近三次动量至少一次偏离 100 超过 `MomentumBuyThreshold`。
  - 主周期成交量上升（`Volume[1] > Volume[2]` 或 `Volume[1] > Volume[3]`）。
  - 月度 MACD 多头（`MacdMain[1] > MacdSignal[1]`）。
  - ADX 高于 `ExitLevel` 以确认趋势强度。

  空头突破采用对称条件：−DI 主导、`Low[1] < High[2]`、动量低于 100、MACD 看空等。

- **交叉模块**在 +DI 向上穿越 −DI（做多）或 −DI 向上穿越 +DI（做空）时触发。额外过滤条件与 EA 一致：
  - `RequireAdxSlope` 要求当前 ADX 高于前一读数。
  - `ConfirmCrossOnBreakout` 需要交叉时也满足突破阈值。
  - `MinAdxMainLine` 设定交叉时的 ADX 最小值。
  - 仍需满足 LWMA 方向、动量斜率、成交量扩张及 MACD 极性。

- **加仓（Pyramiding）**使用 `LotExponent` 递增仓位。`TradeVolume` 是基础手数，新增仓位乘以 `LotExponent^n`（`n` 为已开仓阶梯数），总仓位受 `MaxTrades` 限制。

## 风险控制
- **保护单**：`TakeProfitSteps` 与 `StopLossSteps` 以价格步长为单位传入 `StartProtection`。
- **追踪止损**：`TrailingStopSteps` 在最新收盘价之外维护手动追踪水平。
- **保本逻辑**：当 `UseBreakEven` 启用且价格向有利方向运行 `BreakEvenTrigger` 个步长后，将止损移至入场价以上/以下 `BreakEvenOffset` 个步长。
- **MACD 离场**：`EnableMacdExit` 为真时，月度 MACD 关系与 EA 中 `Close_BUY`/`Close_SELL` 一致，确保多头在 MACD 主线跌破信号线时离场，空头反之。
- **权益止损**：`UseEquityStop` 追踪累计盈亏曲线，当回撤达到 `TotalEquityRisk` 百分比时强制平仓。

EA 中基于账户货币的目标（例如 “Take Profit in Money”、“Trailing Profit in Money”）未迁移，原因是 StockSharp 更适合使用价格距离与内置保护机制。其他决策逻辑均通过对应指标实现。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | 0.01 | 首笔建仓手数。 |
| `CandleType` | 5 分钟 | 主周期蜡烛流，用于 ADX/LWMA。 |
| `MomentumCandleType` | 15 分钟 | 上位周期动量过滤。 |
| `MacdCandleType` | 30 天 | 提供 MACD 离场数据的周期。 |
| `FastMaPeriod` | 6 | 快速 LWMA 周期。 |
| `SlowMaPeriod` | 85 | 慢速 LWMA 周期。 |
| `AdxPeriod` | 14 | ADX 指标周期。 |
| `MomentumPeriod` | 14 | 上位周期动量指标周期。 |
| `MacdFastPeriod` | 12 | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | 26 | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | 9 | MACD 信号线周期。 |
| `EnableBreakoutStrategy` | true | 启用 ADX 突破逻辑。 |
| `EnableCrossStrategy` | true | 启用 DI 交叉逻辑。 |
| `UseTrendFilter` | true | 在突破逻辑中要求多头 +DI 优势、空头 −DI 优势。 |
| `RequireAdxSlope` | true | 交叉逻辑中要求 ADX 上升。 |
| `ConfirmCrossOnBreakout` | true | 交叉逻辑附加突破阈值。 |
| `EnableMacdExit` | true | 启用 MACD 离场。 |
| `EntryLevel` | 10 | 突破筛选使用的 ADX/+DI/−DI 最小值。 |
| `ExitLevel` | 10 | 允许新开仓的 ADX 最小值。 |
| `MinDirectionalDifference` | 10 | +DI 与 −DI 的最小差值。 |
| `MinAdxMainLine` | 10 | 交叉时 ADX 的最小值。 |
| `MomentumBuyThreshold` | 0.3 | 多头确认所需的动量偏离幅度。 |
| `MomentumSellThreshold` | 0.3 | 空头确认所需的动量偏离幅度。 |
| `MaxTrades` | 10 | 最大加仓阶梯数。 |
| `LotExponent` | 1.44 | 每阶加仓的体积倍增系数。 |
| `TakeProfitSteps` | 50 | 以步长计的止盈距离。 |
| `StopLossSteps` | 20 | 以步长计的止损距离。 |
| `TrailingStopSteps` | 40 | 手动追踪止损距离。 |
| `UseBreakEven` | true | 启用保本移动。 |
| `BreakEvenTrigger` | 30 | 激活保本所需的有利步长。 |
| `BreakEvenOffset` | 30 | 移动止损时相对入场价的额外步长。 |
| `UseEquityStop` | true | 启用权益止损。 |
| `TotalEquityRisk` | 1 | 允许的最大权益回撤（百分比）。 |

## 使用建议
- 依据主周期调整 `MomentumCandleType` 与 `MacdCandleType`，可重现 EA 中“当前周期 → 上位周期 → 月度”的映射。
- 同步调节 `EntryLevel`、`MinDirectionalDifference` 与 `MinAdxMainLine`；降低这些值会显著放宽过滤条件。
- 若不希望放大仓位，可将 `LotExponent` 设为 1.0；大于 1.0 时复刻 EA 的马丁加仓方案。


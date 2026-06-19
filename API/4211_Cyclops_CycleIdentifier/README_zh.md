# Cyclops Cycle Identifier 策略

## 概述

该策略将 MetaTrader 上的 **Cyclops v1.2** 专家顾问及其自带的 *CycleIdentifier* 指标移植到 StockSharp 高级 API。算法使用平滑移动平均 (SMMA) 对收盘价进行平滑处理，通过长期 ATR 估算波动范围，当价格相对最近极值移动足够远时标记周期拐点。主要周期 (major) 触发入场，次要周期 (minor) 可作为可选的离场信号。

可选的零滞后滤波器用于确认平滑序列的斜率，滤波源可以选择平滑后的价格或基于同一序列的 Wilder RSI。策略还提供 Momentum 动量过滤，以及按星期几/小时限制交易的功能。

## 信号逻辑

- **周期识别**：内部状态机跟踪平滑价格的最新高低点。当价格超出“平均范围 × *Length*”时生成次要周期信号，乘以 *MajorCycleStrength* 的阈值标记主要周期。
- **入场**：`MajorBuy` 打开多头，`MajorSell` 打开空头。在反向信号出现前会先平掉已有仓位。
- **出场**：启用 *UseExitSignal* 时，若当前仓位盈利且不存在相反的主要周期信号，`MinorSellExit`（多头）或 `MinorBuyExit`（空头）可触发平仓。
- **零滞后滤波**：启用 *UseCycleFilter* 时，需要零滞后滤波器确认斜率——上涨允许多头，下跌允许空头。滤波源由 *CycleFilterMode* 选择。
- **Momentum 过滤**：启用 *UseMomentumFilter* 时，多头要求 Momentum ≥ *MomentumTriggerLong*，空头要求 Momentum ≤ *MomentumTriggerShort*。

## 仓位管理

- **固定目标**：*TakeProfitPips*、*StopLossPips* 定义以点 (pip) 为单位的固定止盈/止损。
- **保本移动**：盈利达到 *BreakEvenTrigger* 点时，将止损移动到入场价 ±1 个点。
- **跟踪止损**：盈利达到 *TrailingStopTrigger* 点后，启动距离为 *TrailingStopPips* 的跟踪止损。
- **交易时段**：若 *UseTimeRestriction* 为真，仅在 `DayEnd`（0=周日）之前、且该日 `HourEnd` 小时以内允许开新仓；已有仓位仍会继续管理。

## 参数说明

| 参数 | 说明 |
|------|------|
| `Volume` | 入场时使用的下单数量。 |
| `PriceActionFilter` | 作用于收盘价的 SMMA 长度。 |
| `Length` | 检测次要周期时使用的平均范围倍数。 |
| `MajorCycleStrength` | 区分主要与次要周期的倍数。 |
| `UseCycleFilter` | 是否启用零滞后斜率确认。 |
| `CycleFilterMode` | 滤波源：平滑价 (`Sma`) 或 RSI (`Rsi`)。 |
| `FilterStrengthSma` | 使用价格时零滞后滤波器的长度。 |
| `FilterStrengthRsi` | 使用 RSI 时的滤波长度及 RSI 周期。 |
| `UseMomentumFilter` | 是否启用 Momentum 过滤。 |
| `MomentumPeriod` | Momentum 指标周期。 |
| `MomentumTriggerLong` | 多头所需的最低 Momentum。 |
| `MomentumTriggerShort` | 空头允许的最高 Momentum。 |
| `UseExitSignal` | 盈利时是否按次要周期信号退出。 |
| `UseTimeRestriction` | 是否限制交易时间窗口。 |
| `DayEnd` | 允许开新仓的最后一天。 |
| `HourEnd` | 最后交易日内允许开仓的最后小时。 |
| `BreakEvenTrigger` | 触发保本移动所需的盈利点数。 |
| `TrailingStopTrigger` | 启动跟踪止损所需的盈利点数。 |
| `TrailingStopPips` | 跟踪止损与当前价格的距离。 |
| `TakeProfitPips` | 固定止盈距离。 |
| `StopLossPips` | 固定止损距离。 |
| `CandleType` | 提供数据的主时间框架。 |

## 与原版 EA 的差异

- 平均范围通过 250 周期 ATR × *Length* 计算，与原 MQL 中的高低点平均效果一致。
- Momentum 过滤实际使用指标数值（原脚本比较的是常数 `bm`，导致过滤器永远通过）。
- 零滞后滤波使用相同的递推系数并以 decimal 精度实现；RSI 模式采用周期为 *FilterStrengthRsi* 的 Wilder RSI。

## 使用建议

1. 选择交易标的并设置 `CandleType` 为目标时间框架。
2. 根据账户及品种配置风险和交易时段参数。
3. 需要更严格确认时可启用 *UseCycleFilter* 或 *UseMomentumFilter*；关闭可获得更频繁信号。
4. 策略始终只保持单向仓位，出现反向主要信号时先平仓再等待新的入场条件。

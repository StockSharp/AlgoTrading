# Double Channel EA 策略

## 概述

**Double Channel EA** 将 MetaTrader 4 顾问 "DoubleChannelEA_v1.2" 迁移到 StockSharp。自定义 `DoubleChannelIndicator` 完整复现
*iDoubleChannel_v1.5* 指标的上轨、下轨、中线以及买卖箭头缓冲区，并使用高层 API 执行交易和风控。

主要特点：

- 通过 `BindEx` 订阅蜡烛和指标，避免手动管理集合。
- Level1 订阅用于实时监控点差，点差超限时拒绝入场。
- 支持叠加仓位、止盈、止损、跟踪止损以及移动保本等风险控制。
- 可配置的时间窗口和最大持仓数量限制。

## 交易流程

1. 订阅选定的 `CandleType`，仅在蜡烛收盘后处理数据。
2. 指标维护 `ChannelPeriod` 根历史蜡烛，计算：
   - 中线：收盘价的算术平均。
   - 上下轨：按照原指标的窗口公式组合高低开收价计算。
   - 箭头：上一根和上上一根通道位置发生反转，同时上一根蜡烛收于突破方向。
3. `IndicatorShift` 参数允许将信号延后若干根已完成蜡烛。
4. 买箭头触发做多，卖箭头触发做空。`OpenEverySignal = true` 时允许加仓，`CloseInSignal = true` 时遇到反向箭头先平仓。
5. 每根蜡烛都会执行保护性检查：止盈、止损、移动保本、跟踪止损。
6. 如果不满足交易时间、点差或最大持仓条件，则拒绝新订单。

## 资金管理

```
volume = ManualLotSize * (AutoLotSize ? max(RiskFactor, 0.1) : 1)
```

反向开仓时，会自动加上当前反向仓位的绝对值，从而一次市价单完成反转。

## 参数表

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 15 分钟 | 主要蜡烛时间框。 |
| `ChannelPeriod` | 14 | 通道窗口长度。 |
| `IndicatorShift` | 0 | 信号延迟的已完成蜡烛数量。 |
| `OpenEverySignal` | true | 允许每个信号加仓。 |
| `CloseInSignal` | false | 反向信号时先平掉现有仓位。 |
| `UseTakeProfit` | false | 启用止盈。 |
| `TakeProfitPoints` | 10 | 止盈距离，单位为价格绝对值。 |
| `UseStopLoss` | false | 启用止损。 |
| `StopLossPoints` | 10 | 止损距离（价格绝对值）。 |
| `UseTrailingStop` | false | 启用跟踪止损。 |
| `TrailingStopPoints` | 5 | 跟踪止损的基础距离。 |
| `TrailingStepPoints` | 1 | 更新跟踪止损所需的最小改善。 |
| `UseBreakEven` | false | 启用移动保本。 |
| `BreakEvenPoints` | 4 | 保本后设置的止损价差。 |
| `BreakEvenAfterPoints` | 2 | 触发保本所需的额外盈利。 |
| `AutoLotSize` | true | 将手动手数乘以 `RiskFactor`。 |
| `RiskFactor` | 1 | 自动手数时的风险系数。 |
| `ManualLotSize` | 0.01 | 手动手数基准。 |
| `UseTimeFilter` | false | 启用交易时间过滤。 |
| `TimeStartTrade` | 0 | 开始交易的小时（含）。 |
| `TimeEndTrade` | 0 | 停止交易的小时（不含）。若与开始相同则不限制。 |
| `MaxOrders` | 0 | 每个方向允许的最大仓位数（0 表示无限制）。 |
| `MaxSpreadPoints` | 0 | 允许的最大点差（价格单位）。 |

## 迁移说明

- 箭头逻辑完全复制原始指标：保存两根历史通道，判断当前蜡烛是否满足反转条件后给出信号。
- MT4 中的资金管理依赖账户参数，此处改为简单的风险系数乘法，便于跨平台复现。
- 所有距离参数（止盈、止损、跟踪、保本）都以绝对价格表示，使用时请结合标的最小价位调整。
- 若缺少 Level1 报价，策略不会入场，与原顾问保持一致的防御性行为。

# Eliot Waves 策略

## 概述

Eliot Waves 策略是在 StockSharp 平台上对 MetaTrader 同名 EA 的重构。策略通过高层接口 `SubscribeCandles().BindEx(...)` 同时驱动多种指标，仅对所选时间框架的已完成 K 线做决策，从而保持与原始脚本一致的确定性行为。

## 入场逻辑

1. **趋势过滤。** 默认情况下使用周期为 6 的快速线性加权移动平均线（LWMA）和周期为 85 的慢速 LWMA，基于典型价格计算。当快速 LWMA 位于慢速 LWMA 之上时，只允许做多信号；当快速 LWMA 低于慢速 LWMA 时，只允许做空信号。
2. **动量确认。** 动量指标（周期 14）最新三次读数只要有一次偏离 100 的绝对值超过阈值（默认 0.3），即可视为动量充足，复刻了原 EA 的判断方式。
3. **蜡烛结构。** 做多时要求前两根 K 线的最低价低于上一根 K 线的最高价；做空时要求上一根 K 线的最低价低于前两根 K 线的最高价。该条件反映了源码中的“艾略特波”结构过滤器。
4. **仓位递增。** 每次信号尝试按固定手数（默认 0.1）叠加仓位，最多叠加 `MaxPositions`（默认 10） 次。若存在反向仓位，会先平掉再按新方向建仓。

## 风险控制与出场

- **止损/止盈。** 以点值为单位设置，并基于最新的持仓均价动态计算。
- **移动止损。** 当浮动盈利超过 `TrailingStopPips` 时，将止损跟随价格向盈利方向移动。
- **保本功能。** 达到 `BreakEvenTriggerPips` 后，将止损移至开仓价，并按照 `BreakEvenOffsetPips` 留出缓冲。
- **布林带退出。** 多单触及 20 周期、宽度为 2 的布林带下轨时平仓，空单触及上轨时平仓，复现了原始脚本的波动性离场规则。
- **MACD 过滤。** 一旦 MACD（12,26,9）主线与信号线交叉且方向不利于当前持仓，则立即退出。
- **强制离场。** `EnableExitStrategy` 参数可在需要时立即清空所有持仓。

## 参数说明

| 参数 | 描述 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 单次加仓的成交量。 | 0.1 |
| `CandleType` | 用于计算的 K 线类型。 | 15 分钟 K 线 |
| `FastMaPeriod` / `SlowMaPeriod` | 快速/慢速 LWMA 的周期。 | 6 / 85 |
| `MomentumPeriod` | 动量指标的回看长度。 | 14 |
| `MomentumThreshold` | 动量偏离 100 的最小阈值。 | 0.3 |
| `StopLossPips` / `TakeProfitPips` | 止损与止盈的点数。 | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | 是否启用移动止损及其距离。 | true / 40 |
| `EnableBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | 保本功能开关、触发距离与偏移量。 | true, 30, 30 |
| `MaxPositions` | 最大叠加次数。 | 10 |
| `EnableExitStrategy` | 是否强制立即平仓。 | false |

## 转换细节

- 所有指标都在同一个订阅管道中更新，避免了对历史值的手动索引，符合项目对高层 API 的要求。
- 点值转换优先使用证券的 `PriceStep`。如果无法获取精确点值，策略会写入警告并退回到价格步长作为近似值，与原 EA 的弹性逻辑一致。
- 邮件、推送提醒等 MetaTrader 特有的通知功能被移除，改用 StockSharp 的日志系统。
- 止损、止盈、移动止损和保本规则全部由策略内部管理，实际执行时通过市价单完成，便于回测重现。

## 使用建议

1. 根据账户规模设置 `TradeVolume` 与 `MaxPositions`，默认参数对应较保守的资金管理方式。
2. 若标的波动性与默认假设差异较大，可优先优化 `MomentumThreshold`、`StopLossPips` 与 `TrailingStopPips`。
3. 确保所选证券提供正确的价格步长，否则点值换算可能导致止损/止盈距离失真。
4. 关注日志中 *"Unable to determine pip size from security settings"* 的警告，如出现应及时校正证券属性或手动调整风险参数。


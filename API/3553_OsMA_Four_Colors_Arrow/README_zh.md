# OsMA Four Colors Arrow 策略

## 概述

该策略把 MetaTrader 中的 "OsMA Four Colors Arrow" 智能交易系统移植到 StockSharp 平台。原始 EA 使用彩色箭头指示 OsMA（MACD 柱状图）状态的改变。本移植版本通过监控 MACD 柱状图的零轴穿越来复现箭头信号：当柱状图从负值转为正值时开多，当从正值跌破零轴时开空。可选的反向模式可以快速切换到对冲或反转交易思路。

策略只在收盘 K 线时运行，可选的时间过滤器允许限定每日交易时段。风险控制部分支持固定手数、聚合仓位的最大数量限制，以及以“点”为单位设置的止损、止盈和移动止损。

## 交易逻辑

1. 订阅指定周期的蜡烛，并按照可配置的快/慢/信号 EMA 周期计算 MACD 柱状图（OsMA）。
2. 每根蜡烛收盘时检测柱状图符号：
   - 柱状图上穿零轴 → 生成多头信号。
   - 柱状图下破零轴 → 生成空头信号。
3. 下单前应用附加条件：
   - 仅做多、仅做空或双向模式。
   - 反向模式颠倒买卖方向。
   - 如有需要，先平掉反向仓位。
   - 限制为单一仓位或在总仓位上限之内逐步加仓。
4. 按设定手数发送市价单，`StartProtection` 会把点值转换为绝对价格，自动管理止损、止盈与移动止损。
5. 如果启用时间控制，非交易时段的信号会被忽略。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `CandleType` | 计算与信号使用的周期。 |
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | MACD 柱状图的 EMA 周期。 |
| `StopLossPips` / `TakeProfitPips` | 止损、止盈的点数（0 表示关闭）。 |
| `TrailingActivatePips` | 触发移动止损所需的盈利点数。 |
| `TrailingStopPips` | 移动止损与价格之间的距离（点）。 |
| `TrailingStepPips` | 每次上调移动止损所需的额外盈利点数。 |
| `MaxPositions` | 允许的最大聚合仓位数量（以 `TradeVolume` 为单位，0 表示无限制）。 |
| `ReverseSignals` | 反转买卖方向。 |
| `DirectionMode` | 允许的交易方向。 |
| `CloseOppositePositions` | 开新仓前是否平掉反向仓位。 |
| `OnlyOnePosition` | 是否禁止在同方向重复加仓。 |
| `UseTimeControl` | 启用交易时段过滤。 |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 交易时段的起止时间，可跨越午夜。 |
| `TradeVolume` | 市价单的手数。 |

## 注意事项

- 移动止损的参数遵循原版 EA：只有在盈利达到 `TrailingActivatePips` 后才会启动，并按照 `TrailingStepPips` 的步长调整。
- 策略需要品种提供 `PriceStep` 与 `Decimals`，才能把点值转换为价格距离；若缺失则退化为 1 个价格单位。
- 当 `MaxPositions` 大于 0 时，可以在不超过限制的前提下按批次加仓。
- 启用时间过滤且开始与结束时间相同会禁止交易，以避免时段含糊。
- 逻辑只对收盘蜡烛生效，不会在未完成的 K 线上下单，与原始 MQL 策略保持一致。

# Ketty 通道突破策略

## 概述
Ketty 通道突破策略是 Ketty.mq5 专家的 C# 版本复刻。策略在可配置的盘前时间段构建一个短期价格通道，并等待价格向通道外剧烈波动。一旦出现剧烈波动，就在通道的另一侧挂入止损单，同时配合可选的止损和止盈订单，完全复现原始 MQL5 方案的挂单流程。

## 交易逻辑
1. **每日初始化**：在每天的第一根 K 线到来时，策略会删除所有挂单（如果没有持仓还会撤掉保护性订单），并重新计算通道统计数据。
2. **通道构建**：在 `ChannelStartHour:ChannelStartMinute` 到 `ChannelEndHour:ChannelEndMinute` 之间，策略跟踪所选 `CandleType` 的最高价和最低价。得到的区间在当天剩余时间里用作突破通道。
3. **挂单价格**：计划中的买入止损价格为 `channelHigh + OrderPriceShiftPips`，卖出止损价格为 `channelLow - OrderPriceShiftPips`。pip 转换方式与原始程序一致：当标的价格有 3 位或 5 位小数时，一个 pip 等于 10 个最小报价步长，否则等于一个步长。
4. **信号判定**：当通道已经形成且当前时间处于 `PlacingStartHour` 与 `PlacingEndHour` 之间时，检查最近一根收盘 K 线。如果该 K 线的最低价向下突破通道不少于 `ChannelBreakthroughPips`，则准备买入止损订单；如果最高价向上突破同样的距离，则准备卖出止损订单。
5. **挂单管理**：任意时刻只保留一个挂单。出现新信号时会撤销旧单并提交新的止损单。在 `PlacingEndHour` 之后系统会自动撤销所有挂单。
6. **保护性订单**：挂单成交后，若 `StopLossPips` 大于 0，会立即挂出止损单；若 `TakeProfitPips` 大于 0，会同时挂出止盈单。当持仓完全平仓后，这些保护性订单会被撤销。

## 参数说明
- `EntryVolume`：下单的默认手数。
- `StopLossPips`：进场价到止损单的距离，为 0 表示不启用。
- `TakeProfitPips`：进场价到止盈单的距离，为 0 表示不启用。
- `ChannelStartHour` / `ChannelStartMinute`：通道统计的开始时间。
- `ChannelEndHour` / `ChannelEndMinute`：通道统计的结束时间，算法支持跨越午夜的情况。
- `PlacingStartHour`：允许挂入止损单的起始小时。
- `PlacingEndHour`：超过该小时后所有挂单会被撤销。
- `ChannelBreakthroughPips`：最近一根 K 线必须突破的缓冲距离，满足后才会挂单。
- `OrderPriceShiftPips`：在通道边界基础上附加的价格偏移。
- `VisualizeChannel`：开启后会在图表上绘制表示当前通道的两条水平线。
- `CandleType`：用于构建和监控通道的 K 线周期。

## 额外说明
- 策略假设行情数据连续。如果通道时间窗内缺少数据，将在新 K 线到来时继续补齐通道。
- 在 StockSharp 中无法像 MetaTrader 那样把止损/止盈直接附着到挂单上，因此策略在挂单成交后使用独立的止损/止盈订单进行保护。
- 请确保 `EntryVolume` 满足经纪商的最小交易步长，并选择活跃的时间周期。原版策略默认使用 1 分钟 K 线。

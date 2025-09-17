# 汇率规律策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 4 专家顾问 **Strategy_of_Regularities_of_Exchange_Rates.mq4** 的 StockSharp 版本。它实现了一种典型的日内突破对敲：在指定的时间同时挂出多、空方向的止损单，到了夜间的收盘时间则无条件撤单并平掉所有仓位。这样可以确保交易活动完全限制在一个交易日之内。

策略不依赖技术指标，仅根据时间和距离做决策。当出现 `OpeningHour` 时，程序读取当前的买一/卖一价格，按 `EntryOffsetPoints`（以经纪商“点”为单位）向上和向下偏移，分别放置 *Buy Stop* 与 *Sell Stop*。代码会根据 `PriceStep` 自动放大 3 位或 5 位小数报价的最小变动，以保持与原始 MQL 脚本一致。

## 交易流程

1. **开仓时间**：当一根完成的蜡烛属于 `OpeningHour` 时，策略会先清理残留的挂单，再在买卖价两侧按照 `EntryOffsetPoints * point` 的距离挂出对称止损单。
2. **保护止损**：启动后立即调用 `StartProtection`，将 `StopLossPoints` 转换成绝对价格偏移，确保成交后立刻挂上平台侧的止损单。
3. **止盈监控**：每当收盘价刷新，如果浮动利润超过 `TakeProfitPoints * point`，就会用市价单平仓，复制了原脚本中 `OrderClose` 的盈利退出逻辑。
4. **收盘时间**：当时间到达 `ClosingHour`，策略会撤销所有未成交的挂单，并无条件平掉剩余仓位。
5. **日内重置**：每天只会重新布置一次挂单，避免在 1 小时以下的时间框架里重复提交同一组订单。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OpeningHour` | `9` | 安排挂单的小时（0–23）。 |
| `ClosingHour` | `2` | 撤单并强制平仓的小时（0–23）。 |
| `EntryOffsetPoints` | `20` | 挂单与当前买卖价之间的点数距离。 |
| `TakeProfitPoints` | `20` | 触发手动止盈的点数距离，设置为 `0` 可关闭。 |
| `StopLossPoints` | `500` | 传递给 `StartProtection` 的止损点数。 |
| `OrderVolume` | `0.1` | 每个止损挂单的下单量。 |
| `CandleType` | `30 分钟` | 用于判定时间窗口的蜡烛类型，建议保持在 1 小时及以下以贴近原策略。 |

## 移植说明

- 原脚本基于逐笔报价并直接调用 `Hour()`。在 StockSharp 中改为监听已完成的蜡烛，并读取其 `OpenTime.Hour`，既符合仓库仅处理完成蜡烛的规范，也保持了时间逻辑。
- 挂单价格通过 `Security.ShrinkPrice` 归一化，从而保证与标的的最小价位变动对齐。
- 保护性止损交给 `StartProtection` 管理，相当于在 MetaTrader 的 `OrderSend` 中附带 stop-loss 参数。
- 新代码记录最近一次布单的日期，避免在子小时级别的图表上重复铺设相同的双向挂单。
- 源码加入了详细的英文注释，完整解释每一步的意图，方便后续维护和二次开发。

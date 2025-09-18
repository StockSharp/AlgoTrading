# 新闻过滤策略（MQL 转换版）

## 策略概览

**News Filter Strategy** 是对 MetaTrader 4 上 "NEWS_Filter" 智能交易程序的完整移植。策略会定时从 FXStreet 经济日历获取未来一周的新闻事件，通过货币、重要级别以及关键字等条件过滤出需要重点关注的事件，在新闻前后窗口内触发暂停交易的信号。

## 工作流程

1. 访问 FXStreet mini widget 接口，下载未来 7 天的经济数据。
2. 解析每条事件的日期、时间、货币、重要级别（波动性）、标题以及公布值/预期值/前值。
3. 根据以下条件筛选事件：
   - 用户设置的货币代码列表。
   - 低、中、高三个重要级别的勾选情况。
   - 可选的关键字过滤，标题必须包含该关键字。
4. 为每个事件创建一个“禁止交易窗口”：`事件时间 - StopBeforeNewsMinutes` 到 `事件时间 + StartAfterNewsMinutes`。
5. 维护一个当前状态标志 `IsNewsActive`，在窗口内时为 `true`，并向日志输出详细提示，供其它策略暂停下单。

## 参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `EnableNewsFilter` | `true` | 是否启用新闻过滤逻辑。 |
| `UseLowImportance` | `false` | 是否纳入一星（低影响）事件。 |
| `UseMediumImportance` | `true` | 是否纳入二星（中影响）事件。 |
| `UseHighImportance` | `true` | 是否纳入三星（高影响）事件。 |
| `StopBeforeNewsMinutes` | `30` | 新闻前多少分钟停止交易。 |
| `StartAfterNewsMinutes` | `30` | 新闻后多少分钟恢复交易。 |
| `CurrenciesFilter` | `USD,EUR,CAD,AUD,NZD,GBP` | 需要关注的货币列表，使用逗号分隔。 |
| `FilterByKeyword` | `false` | 是否启用关键字过滤。 |
| `Keyword` | `employment` | 关键字内容，仅在 `FilterByKeyword` 为 `true` 时生效。 |
| `RefreshIntervalMinutes` | `10` | 日历刷新周期（分钟）。计时器每分钟检查一次，若超过该周期则重新下载。 |

## 交易逻辑

- 策略内部仅使用一个计时器，既负责刷新缓存，也负责判断当前是否处于新闻窗口。
- 窗口区间定义为 `[事件时间 - StopBeforeNewsMinutes, 事件时间 + StartAfterNewsMinutes]`。
- 当当前时间位于任意窗口内时，`IsNewsActive` 置为 `true`，并记录诸如 `News time: USD Nonfarm Payrolls at 2024-06-07 12:30` 的日志。
- 一旦脱离窗口，策略会在调试日志中提示下一条新闻时间，并将 `IsNewsActive` 设为 `false`。
- 运行在同一平台上的其他策略可在下单前检查该标志，从而模拟原始 EA 中 `NEWS_ON` 全局变量和 `Comment()` 函数的效果。

## 移植细节

- HTML 解析采用正则表达式，逻辑与原脚本的字符串处理保持一致，同时兼容可能出现的 JSON 响应格式。
- MQL 版本在图表上绘制文本和垂直线，本移植版本改为通过日志输出提示，便于在 StockSharp 的图形界面或日志系统中查看。
- 默认按照 UTC 时间处理新闻，与原脚本利用 `TimeGMTOffset()` 做时间矫正的思路一致。
- 策略本身不提交委托，主要用于为其他交易策略提供“新闻期间暂停交易”的辅助信号。

## 使用建议

1. 在 AlgoTrader 示例或自建宿主中启动该策略并绑定目标证券。
2. 打开日志或消息面板，关注策略输出的新闻提示与状态变化。
3. 在其他策略中调用下单函数前，先检查 `IsNewsActive`，若为 `true` 则延迟或取消交易。 
4. 根据所交易市场的特点调整货币列表、重要级别和关键字，聚焦对策略影响最大的新闻事件。


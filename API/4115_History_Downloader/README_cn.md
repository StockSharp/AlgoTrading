# History Downloader 策略

## 概述
**History Downloader Strategy** 是 MetaTrader 专家顾问 `HistoryDownloader.mq4` 的 StockSharp 版本。原程序通过模拟按下图表窗口的
`HOME` 键，并借助一个配套指标把图表状态写入全局变量，从而强制终端回放更早的历史数据。移植到 C# 后，策略直接调用
StockSharp 的高层行情接口，不再依赖平台特性，而是持续请求旧的 K 线，直到到达指定的起始日期。策略不参与交易，专门用于
预先下载历史数据，为回测或图表分析做准备。

## 工作流程
1. 启动时，策略会针对 `Strategy.Security` 指定的标的按参数 `CandleType` 订阅蜡烛序列。
2. 每根收盘蜡烛都会刷新内部统计：
   - `_receivedCandles` 记录收到的蜡烛数量；
   - `_earliestCandleTime` 保存目前为止最早的 `OpenTime`；
   - `_lastUpdateTime` 表示最近一次数据更新时间，供看门狗计时器使用。
3. 内置 `Strategy.Timer` 取代了 MQL 中的等待循环，每经过 `RequestTimeout` 周期触发一次：
   - 如果期间有新数据到达，超时计数器会被重置；
   - 如果没有新数据，计数器递增，当连续达到 `MaxFailures` 次时视为失败并停止下载，等价于 MQL 参数
     `MaxFailsInARow` 的作用。
4. 每根蜡烛都会写入一条进度日志（`LogInfo`），类似 MetaTrader 图表上的 `Comment`。辅助方法
   `FormatDuration` 将耗时格式化为 `Xh Ym Zs` 字符串，对应原脚本里的 `FormatTime`。
5. 一旦最早的蜡烛时间不晚于 `TargetDate`，策略即判定下载完成，注销行情订阅并自行停止，同时输出汇总信息：总蜡烛数、
   最早时间以及执行时长。

## 监控与日志
- `LogInfo` 输出当前进度、最早时间和累计耗时。
- `LogWarning` 在检测到长时间无数据时提示，并展示已累计的超时次数。
- `LogError` 在超过 `MaxFailures` 后中止任务并记录失败原因。
- 策略不会发送任何订单，因此 `PnL`、持仓和订单事件始终保持为空。

## 参数
| 名称 | 类型 | 默认值 | MetaTrader 对应项 | 说明 |
| --- | --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟 | 图表周期 | 需要从数据源下载的蜡烛时间框架。 |
| `TargetDate` | `DateTimeOffset` | 2009-01-01 00:00:00Z | `ToDate` | 需要覆盖到的最早开盘时间，达到后任务即视为成功。 |
| `RequestTimeout` | `TimeSpan` | 1 秒 | `Timeout` (毫秒) | 等待新蜡烛的最长间隔，超过即累计一次失败。 |
| `MaxFailures` | `int` | 10 | `MaxFailsInARow` | 允许连续超时的次数，超过后终止下载。 |

## 与原版 EA 的差异
- Windows `PostMessage` 键盘模拟被原生的蜡烛订阅取代，更加稳定。
- 不再需要指标 `HistoryDownloaderI.mq4`，所有统计都在策略内部完成。
- 全局变量改为使用 StockSharp 的时间戳及看门狗计时器，进度更可控。
- 成功或失败通过日志上报，没有弹窗提示，方便在无人值守的环境中运行。

## 使用建议
- 启动前请配置好 `Security`、`Portfolio` 和 `Connector`，确保数据源可以提供历史蜡烛。
- 根据数据供应商的延迟调整 `RequestTimeout`，若使用慢速档案接口建议适当增大间隔。
- 如果历史数据分批返回或队列较长，可以提高 `MaxFailures` 以避免过早判定失败。
- 策略在成功或失败后会自动停止，适合嵌入到预处理历史数据的自动化流程中。

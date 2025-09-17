# News Template Universal 策略

## 概述
**News Template Universal Strategy** 将原始 MQL 新闻过滤模板移植到 StockSharp 框架。策略通过
`Connector.SubscribeMarketData(Security, MarketDataTypes.News)` 订阅经济新闻，并按照重要性、货币代码以及自定义
关键字对消息进行筛选。在任何新闻事件前后的限定时间窗口内，属性 `IsNewsActive` 会被置为 `true`，以便上层
交易逻辑停止开仓或执行其他保护措施。

与 MQL 版本相比，本实现具有以下差异：

* 不再需要 `WebRequest` 下载网页，也无需解析 HTML，完全依赖 StockSharp 的新闻数据流。
* 使用默认的一分钟 K 线作为时间驱动，负责清理过期事件并检测当前时间是否处于限制区间。
* 未创建图表文本或垂直线，所有信息通过日志与公开属性对外提供。

## 工作流程
1. 启动时同时订阅新闻和指定类型的 K 线数据。
2. 每根收盘的 K 线都会触发一次检查：移除已过期的事件，并判断当前时间是否落在任何新闻窗口内。
3. 新到达的 `NewsMessage` 会被解析。当满足以下条件时事件会被保存：
   * 符合 `IncludeLow`、`IncludeMedium`、`IncludeHigh` 设置的影响等级。
   * 新闻文本中包含 `Currencies` 列表里的任意货币代码。
   * 当 `CheckSpecificNews` 为 `true` 时，文本中必须包含 `SpecificNewsText` 指定的关键字。
4. 若当前时间处于事件开始前 `StopBeforeNewsMinutes` 分钟或结束后 `StartAfterNewsMinutes` 分钟内，则
   `IsNewsActive` 设为 `true` 并在日志输出 "News time..."。离开窗口后输出 "No news" 并清除标记。

## 参数说明
| 参数 | 描述 | 默认值 |
|------|------|--------|
| `UseNewsFilter` | 是否启用新闻过滤逻辑。 | `true` |
| `IncludeLow` | 接受包含低影响标识（"*" 或 `LOW`）的新闻。 | `false` |
| `IncludeMedium` | 接受包含中等影响标识（"**"、`MEDIUM`、`MODERATE`）的新闻。 | `true` |
| `IncludeHigh` | 接受包含高影响标识（"***" 或 `HIGH`）的新闻。 | `true` |
| `StopBeforeNewsMinutes` | 新闻前需要停止交易的分钟数。 | `30` |
| `StartAfterNewsMinutes` | 新闻结束后恢复交易的分钟数。 | `30` |
| `Currencies` | 逗号分隔的货币代码，在新闻文本中逐个搜索。 | `USD,EUR,CAD,AUD,NZD,GBP` |
| `CheckSpecificNews` | 是否启用关键字过滤。 | `false` |
| `SpecificNewsText` | 当启用关键字过滤时必须出现的文本（忽略大小写）。 | `employment` |
| `CandleType` | 参与时间判断的 K 线类型/周期。 | `1 分钟时间框架` |

## 实现细节
* 所有文本先转换成大写，保证比较不区分大小写。
* 影响等级既支持 FxStreet 的星号表示（`*`、`**`、`***`），也支持单词（`LOW`、`MEDIUM`、`HIGH`、`MODERATE`）。
* 事件列表在每次插入后都会排序，便于最快找到下一条新闻。
* 当当前时间晚于事件时间加上 `StartAfterNewsMinutes` 后，事件会被删除，保持内存占用稳定。
* 在 `OnStopped` 中取消新闻订阅，防止连接器残留无用的订阅。

## 使用建议
1. 在自己的交易策略中读取 `IsNewsActive` 属性，避免在新闻窗口内提交新的订单。
2. 如果数据提供商使用自定义字段表示重要性，可以重写 `OnProcessMessage` 或扩展 `ParseImportance`。
3. 在回测或仿真环境中，请确认所选证券确实提供新闻数据，否则不会触发任何过滤。

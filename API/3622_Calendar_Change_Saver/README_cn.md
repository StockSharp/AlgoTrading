# Calendar Change Saver 策略

## 概述
**Calendar Change Saver** 是基于 StockSharp 的策略，用来复刻 `MQL/45592` 包中的 MQL5 服务。原始服务会不断读取 MetaTrader 的经济日历，并把每一次返回的变动标识保存到文件里。移植后的策略会订阅经纪商提供的新闻流，将接收到的每条新闻序列化写入日志，方便在离线环境中分析。

策略本身 **不会** 执行交易。它的目标是构建一份持久的新闻档案，帮助研究日历事件对市场的影响或验证基于新闻的交易系统。

## 转换说明
- MQL5 版本通过循环调用 `CalendarValueLast` 获取最新的变动 ID，把结果写入二进制文件，如果返回的记录超过 100 条则判定为异常并忽略。
- 在 StockSharp 中采用事件驱动方式。策略订阅所选证券的 `MarketDataTypes.News`，并缓存每个 `NewsMessage`。
- 通过可配置的计时器定期刷新缓存，把累积的新闻批量写入磁盘。每一行都包含刷新时间、批量大小以及新闻内容的简洁表示。
- 当批量大小超出 `BulkLimit` 时会被跳过，以此复制 MQL5 中的防护逻辑。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `OutputFileName` | 保存新闻批次的文本文件路径，会自动创建缺失的目录。 | `calendar_changes.log` |
| `FlushIntervalMilliseconds` | 刷新计时器的间隔（毫秒）。数值越小写入越频繁。 | `1000` |
| `BulkLimit` | 每个批次允许的最大新闻数量，超过该值的批次会被忽略。 | `100` |

## 输出格式
```
<flush_time_iso>|<batch_count>|[<event_time_iso>;<source>;<headline>;<story>,...]
```
- `flush_time_iso`：刷新发生时的 UTC 时间。
- `batch_count`：该批次写入的新闻条数。
- `event_time_iso`：新闻的 UTC 时间戳。
- `source`、`headline`、`story`：经过清理的文本字段，内部的分隔符都会被替换成空格。

## 使用步骤
1. 将策略连接到支持新闻推送的交易接入端。
2. 选择目标证券，并根据需要调整参数（例如输出路径或刷新间隔）。
3. 启动策略，所有新的 `NewsMessage` 会被加入缓存并按设定的节奏写入文件。
4. 停止策略以关闭文件句柄。

## 限制
- 如果数据源不提供新闻或日历事件，日志文件将保持为空。
- 超过 `BulkLimit` 的批次会被过滤，确保历史记录的可靠性。


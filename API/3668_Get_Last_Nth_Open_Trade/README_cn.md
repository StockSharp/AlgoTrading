# Get Last Nth Open Trade 策略

## 概述
**Get Last Nth Open Trade Strategy** 复刻原始 MetaTrader 专家的逻辑：定期扫描当前投资组合，并输出倒数第 N 个未平仓头寸的详细信息。该策略仅用于监控，不会提交或撤单，而是周期性检查经纪商持仓并把格式化的快照写入策略日志，方便操作者在 Designer 中直接查看票据、方向、数量、价格和盈利情况。

## 主要特性
- 通过定时器按设定的时间间隔刷新快照，策略启动后立即生成第一份报告。
- 支持与 MQL 版本一致的过滤器：可以只检查策略所选证券，也可以只检查带有指定策略标识（相当于 MetaTrader 的 magic number）的头寸。
- 根据 `LastChangeTime` 逆序排序，索引 `0` 总是代表最新的交易，从而与原脚本“从末尾取第 N 个”的行为一致。
- 日志输出包含头寸编号、品种、方向、数量、均价、收益、最后变更时间以及（若存在）来源策略标识。
- 使用线程安全的定时器防护，避免在上一轮快照尚未完成时触发新的执行。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `EnableMagicNumber` | 启用后，仅报告策略标识等于 `MagicNumber` 的头寸。 | `false` |
| `EnableSymbolFilter` | 启用后，只检查 `Strategy.Security` 对应的头寸。 | `false` |
| `MagicNumber` | 字符串形式的策略标识（MetaTrader magic number 的对应物），在启用 magic 过滤时用于匹配头寸。 | `"1234"` |
| `TradeIndex` | 在按最新时间排序后的列表中要报告的索引（从 0 开始，0 表示最新头寸）。 | `0` |
| `RefreshInterval` | 相邻两次快照之间的时间间隔。策略启动时立即执行第一次快照。 | `1` 秒 |

## 工作原理
1. `OnStarted` 中检查是否已分配投资组合，并确认 `RefreshInterval` 为正值。
2. 创建 `System.Threading.Timer`，初始延迟为 0，因此第一次快照立即执行，后续按设定间隔运行。
3. 每次回调遍历 `Portfolio.Positions`，跳过数量为零的条目，然后根据可选的品种和策略标识过滤器筛选。
4. 将剩余头寸按 `LastChangeTime` 倒序排列。如果 `TradeIndex` 超出范围，会在日志中给出说明并等待下一次回调。
5. 找到目标头寸后，组装包含票据、品种、方向、数量、均价、收益、最后变更时间以及可选策略标识的文本块，并通过 `LogInfo` 写入日志。
6. 整个过程使用 `Interlocked.Exchange` 实现的无锁防护，确保慢速 I/O 或日志不会导致定时器回调重叠。

## 使用说明
- 启动前为策略指定连接器和 `Portfolio`。若需启用品种过滤，请同时设置 `Strategy.Security`。
- 如需模拟 MetaTrader 的 magic number 过滤器，将其他策略下单时使用的 `StrategyId` 原样填写到 `MagicNumber` 中。
- 报告写入策略日志（Designer 控制台、Runner 日志等）。若需要界面展示，可订阅日志输出并在 UI 中显示。
- `TradeIndex` 从 0 开始计数：`0` 代表最新交易，`1` 代表次新交易，以此类推。

## 与 MQL 版本的差异
- MetaTrader 头寸直接提供止损、止盈和备注等字段；StockSharp 的头寸没有暴露这些属性，因此报告聚焦于编号、方向、数量、价格和盈亏信息。
- 本移植版本通过 `LogInfo` 写入 StockSharp 日志，而不是在图表注释中展示数据。
- 排序依据 `LastChangeTime`（最近一次来自连接器的更新），而原脚本按票据编号排序。两种方式都能保证索引 `0` 对应最新头寸。
- Magic 过滤使用头寸中保存的字符串 `StrategyId`。若持仓缺少该元数据，可保持 `EnableMagicNumber` 关闭。

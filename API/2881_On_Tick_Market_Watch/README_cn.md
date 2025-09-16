# 跳动行情观察策略

## 概述
**跳动行情观察策略**（On Tick Market Watch Strategy）复刻了 MetaTrader 脚本 `scOnTickMarketWatch.mq5` 的行为。原始脚本会持续扫描行情观察列表，只要任意品种出现新的跳动，就发送自定义事件并打印该品种的买价与点差。本策略将该逻辑转换为 StockSharp 的高级策略：订阅 Level1 数据并通过日志输出 Tick 信息。

该策略本质上并不交易，主要用于监控或诊断连接器收到的实时报价。得益于 StockSharp 的事件驱动模式，实现过程中无需像 MQL 脚本那样维护循环和延时。

## 主要特性
- 监控策略主标的以及通过逗号分隔列表提供的额外标的。
- 为每个标的订阅 Level1 数据，捕获买卖盘口变动。
- 当买、卖价同时存在时计算点差（卖价减买价），并以英文记录详细日志。
- 保持与用户指定顺序一致的内部索引，以模拟 Market Watch 列表中的顺序。
- 当某个符号无法由当前 `SecurityProvider` 解析时输出友好警告。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `SymbolsList` | `string` | `""` | 额外监控标的的逗号分隔列表（例如 `AAPL@NASDAQ,MSFT@NASDAQ`），会在主标的之后被依次添加。所有标的都必须存在于当前的 `SecurityProvider` 中。 |

## 工作流程
1. `OnStarted` 中先解析所有符号。主标的 (`Strategy.Security`) 始终位于列表首位，其后依次加入 `SymbolsList` 中的其他符号。
2. 对每个解析成功的标的调用 `SubscribeLevel1`，并绑定回调以接收 `Level1ChangeMessage`。
3. 回调会检查更新是否包含 `LastTradePrice`、`BestBidPrice` 或 `BestAskPrice` 之一。
4. 买价优先取 `BestBidPrice`，若为空则回退到 `LastTradePrice`；卖价取 `BestAskPrice`；两者同时存在时计算点差。
5. 日志输出格式与原脚本一致：`New tick on the symbol <id> index in the list=<index> bid=<bid> spread=<spread>`。若卖价缺失，则点差显示为 `n/a`。
6. 当 `SecurityProvider` 无法找到某个符号时，记录警告并跳过该符号。

## 使用步骤
1. 通过界面或代码设置策略主标的 (`Strategy.Security`)。
2. 如需监控额外标的，将其以逗号分隔写入 `SymbolsList` 参数，顺序决定日志中显示的索引。
3. 确保连接器能够为这些标的提供 Level1 数据。
4. 启动策略后会立即发起 Level1 订阅并开始打印 Tick 日志。
5. 在策略日志中检查实际的报价与计算出的点差信息。

## 与 MQL 版本的差异
- C# 版本完全事件驱动，无需 `Sleep` 或手工循环。
- `SymbolsTotal(true)` 的效果通过保持添加顺序实现，索引从 0 开始。
- MetaTrader 中点差以点值表示，而此处使用十进制价格差。
- 自定义图表事件被日志替代，利用了 StockSharp 自带的日志系统。
- 若当前更新缺少卖价，会明确输出 `n/a`，提醒 Level1 数据不完整。
- 策略仅用于监控，不会提交任何委托。

## 示例日志
```
New tick on the symbol AAPL@NASDAQ index in the list=0 bid=171.25 spread=0.02
New tick on the symbol MSFT@NASDAQ index in the list=1 bid=324.10 spread=n/a
```
上述日志展示了策略如何针对每个被监控的品种输出买价和点差信息。

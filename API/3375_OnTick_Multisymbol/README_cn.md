# OnTick 多品种策略

## 概述
- 复刻 MetaTrader 5 模板 **OnTick(string symbol).mq5**，将所选交易品种的每个 tick 推送到统一的处理函数。
- 为每个解析成功的标的订阅成交明细（tick 数据），一旦收到成交就写入包含时间、价格和成交量的日志。
- 通过 StockSharp 参数化机制，用户可以在界面或优化器中灵活调整需要监控的符号列表。

## 原版专家顾问的特性
- MQL5 模板提供 `OnTick(string symbol)` 事件，用 `Print()` 输出触发该事件的符号名称。
- 通过预处理宏 `SYMBOLS_TRADING` 或关键字 `MARKET_WATCH` 决定需要监听的品种。
- `OnInit`、`OnDeinit` 和 `OnChartEvent` 保持空实现，仅演示结构。

## StockSharp 改写说明
- 策略新增 **Symbols** 参数（以逗号/分号/空格分隔的字符串），对应 MQL 模板中的 `SYMBOLS_TRADING` 列表。
- 对每个找到的 `Security` 调用 `SubscribeTrades(security)`，在 `OnTrade` 回调中记录成交信息，与 MQL5 中的日志输出相呼应。
- 当 `SecurityProvider` 无法解析某个标的时，会输出警告并继续处理其余符号，避免静默失败。
- 关键字 `MARKET_WATCH` 暂不自动处理；策略会给出提示，建议用户显式填写与当前数据源匹配的代码。

## 参数
| 名称 | 说明 | 备注 |
| --- | --- | --- |
| `Symbols` | 需要监控的品种 ID 列表，可用逗号、分号或空格分隔。 | 默认值为 `EURUSD,GBPUSD,USDJPY,USDCHF`。无论是否出现在列表中，策略的主 `Security` 属性都会被订阅。 |

## 数据订阅
- `SubscribeTrades(security)` —— 获取逐笔成交，与原始脚本的 tick 驱动逻辑一致。
- `GetWorkingSecurities()` —— 声明策略对每个符号都需要 tick 数据，便于设计器和优化器正确分配行情流。

## 使用建议
1. 将策略连接到已配置 `SecurityProvider` 的终端或服务。
2. 如需额外监控特定标的，可在启动前设置策略的 `Security` 属性。
3. 在界面中编辑 **Symbols** 参数，输入数据源支持的代码并用逗号/分号/空格分隔。
4. 启动策略：每当收到成交，日志中都会出现形如 `Tick received for EURUSD@2024-02-01T10:00:00Z: price=1.08450, volume=1` 的记录。
5. 若需要进一步处理，可在 `OnTrade` 方法中添加自定义逻辑或信号生成。

## 与 MQL 版本的差异
- 使用运行时参数代替预处理宏，支持在 UI/优化器中直接配置。
- 对解析失败的符号给出明确告警，避免无提示地忽略。
- 未自动实现 `MARKET_WATCH` 逻辑；用户应提供具体代码，或自行扩展策略以查询可交易列表。

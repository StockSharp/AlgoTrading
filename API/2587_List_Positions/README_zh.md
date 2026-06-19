# 持仓列表策略

## 概述
**持仓列表策略** 复刻了原始 MetaTrader 脚本的行为，会定期把当前投资组合中的持仓写入策略日志。该策略仅用于监控，不会发送任何订单。它会生成一份持仓快照，便于操作者在 Designer 或其他 StockSharp 日志工具中直接查看交易品种、方向、数量、开仓价格以及当前盈亏。

## 主要特性
- 通过定时器驱动的持仓报告，策略启动后立即生成首个快照。
- 支持按策略标的证券或策略标识符（等同于 MetaTrader 的 magic number）进行可选过滤。
- 在日志中输出详尽信息，包括持仓编号、最近更新时间、方向、数量、均价及盈亏。
- 使用原子操作避免定时器回调重叠，确保在繁忙环境下依旧稳定。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `StrategyIdFilter` | 需要忽略的策略标识符。留空则报告所有持仓。 | 空字符串 |
| `SelectionMode` | 控制是遍历所有品种还是仅遍历 `Strategy.Security` 对应的品种。 | `AllSymbols` |
| `TimerInterval` | 连续快照之间的时间间隔。 | 6 秒 |

## 工作流程
1. 在 `OnStarted` 中检查是否已绑定投资组合以及定时器间隔是否为正值。
2. 创建 `System.Threading.Timer`，首个触发延迟为零，因此第一份报告会立即产生，并按设定间隔重复执行。
3. 每次定时器触发都会调用 `ProcessPositions`，遍历 `Portfolio.Positions`，应用可选的品种及策略标识符过滤，并将格式化后的信息写入 `StringBuilder`。
4. 至少有一条持仓通过过滤时，通过 `LogInfo` 将表格写入日志；若没有匹配项，则输出简洁的提示。
5. 借助原子变量阻止定时器重入，避免因为日志写入阻塞而出现并发执行。

## 使用注意事项
- 启动前请设置好 `Portfolio` 和 `Connector`。若 `SelectionMode` 选为 `CurrentSymbol`，还需为 `Strategy.Security` 指定需要监控的标的。
- 若要复刻 MetaTrader 的 magic 过滤功能，可在 `StrategyIdFilter` 中填写其他策略下单时使用的 `StrategyId` 字符串，这些持仓将被排除在外。
- 本策略不会修改持仓或发送订单，可与其他实盘策略并行运行，作为信息面板使用。
- 日志输出以 `Idx | Symbol | PositionId | LastChange | Side | Quantity | AvgPrice | PnL` 为表头，方便人工阅读或外部工具解析。

## 与 MQL 版本的差异
- MetaTrader 使用无符号 64 位整数表示 magic number，StockSharp 持仓提供的是字符串类型的策略标识符，因此过滤条件也采用字符串。
- MQL 脚本把结果写入图表批注，本移植版本通过 `LogInfo` 输出，在 Designer、Runner 等工具中均可查看。
- StockSharp 版本加入了定时器互斥保护，在负载较高时更加可靠。
- 时间戳基于 `Position.LastChangeTime`，反映 StockSharp 的持仓更新时间；原脚本显示的是订单创建时间。

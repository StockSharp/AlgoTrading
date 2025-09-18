# Filling Type 策略

原始的 MQL5 专家顾问 “Filling Type” 只用来检测当前品种支持的下单成交模式。它调用 `SymbolInfoInteger(SYMBOL_FILLING_MODE)`，并根据返回值在图表上显示 Fill or Kill (FOK)、Immediate or Cancel (IOC) 或 Return 的说明。

迁移到 StockSharp 时保留了这一诊断目的，并利用 `ExchangeBoard` 暴露的元数据：

1. 策略启动时会确认已经为 `Strategy.Security` 指定标的且该证券包含 `ExchangeBoard`。如果缺少交易板块，则记录警告——类似原脚本清空 `Comment()` 的行为。
2. 随后策略通过反射依次读取常见的板块属性（例如 `OrderExecutionTypes`、`ExecutionType` 等）。不同的 StockSharp 连接器会使用不同的属性名称，因此代码会先遍历一组候选名称，再退而求其次扫描包含 “Execution” 或 “Filling” 的任何属性。
3. 找到的属性逐条写入日志。如果属性是一个可枚举集合，会按照索引输出每一项，便于确认是否存在 FOK/IOC/RETURN 或经纪商自定义的执行代码。
4. 若没有找到显式属性，则检查 `ExchangeBoard.ExtensionInfo` 中的键，挑选名称包含 `fill` 或 `exec` 的条目——部分适配器就是通过扩展字典公开成交模式。
5. 如果仍然没有线索，则打印交易板块的通用能力摘要（`IsSupportMarketOrders`、`IsSupportStopOrders`、`IsSupportStopLimitOrders`、`IsSupportOddLots`、`IsSupportMargin`），帮助用户手动比对经纪商文档。
6. 参数 `LogBoardDiagnostics` 控制是否在检测完成后输出扩展诊断信息，包括板块名称、交易所、国家、时区、结算方式、交割板块以及交易时间表。

## 参数

| 名称 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `LogBoardDiagnostics` | `true` | 开启后会在记录成交模式后追加交易板块的详细信息，便于确认已经连接到正确的品种。 |

## 使用说明

- 该策略仅用于诊断，不会发送任何订单。请将其附加到目标证券并在启动后立即查看日志。
- StockSharp 中的成交模式由连接器决定，可能会使用不同的属性名称。通过反射可以在不硬编码某个经纪商协议的情况下保持兼容性。
- 如果既没有属性也没有扩展字典提供信息，回退日志仍然能给出足够的上下文，便于结合经纪商手册推断限制（例如某些市场只允许限价单部分成交）。
- 与 MQL5 不同，StockSharp 没有 `Comment()` 面板，因此所有消息都通过 `LogInfo`/`LogWarning` 输出，并且可以在策略日志或文件中查看。

## 与 MQL 版本的差异

- MQL5 版本在每个 tick 上检查成交模式。StockSharp 版本在 `OnStarted` 中执行一次检测，因为交易板块的元数据通常是静态的，重复输出只会造成噪音。
- 不再使用 MetaTrader 专有的枚举值，而是通过反射读取适配器提供的属性，使得策略能在未来 SDK 更新或不同市场中保持可用。
- 所有信息都写入日志而不是调用 `Comment()`。

## 运行步骤

1. 配置连接器并在策略设置中选择目标证券。
2. 启动策略，日志会显示用于判定成交模式的属性及其具体值。
3. 如需更多上下文，可启用 `LogBoardDiagnostics`，以便获取交易板块的额外资料并核对经纪商的下单要求。

这种实现方式在 StockSharp 环境中复现了原策略的教学价值。

# Count And Wait 策略

## 概述
**Count And Wait Strategy** 复刻了 MetaTrader 示例“Count and Pass”和“Pass Wait then Pass”的行为。策略会统计接收到的逐笔成交数量，当计数达到可配置的阈值时记录一条日志消息，随后可以进入等待阶段，再累计第二个阈值的成交数量，最后重置计数器并开始新的循环。这一逻辑适用于任何需要在固定数量的市场更新之后才执行动作的场景。

## 参数
- **Count Limit** – 需要接收的成交数量，达到后认为一个循环完成。默认值为 `50`，对应原始脚本中的 `count` 输入。
- **Wait Limit** – 触发动作之后需要额外等待的成交数量，再次达到后重置计数器。默认值为 `0`。当取值为零时，等待阶段被禁用，与“Count and Pass”脚本完全一致；取正值时，会启用等待阶段，对应“Pass Wait then Pass”脚本中的 `wait` 参数。

两个参数都通过 `StrategyParam<int>` 暴露，因此可以在界面中调整或用于参数优化。

## 工作流程
1. 启动时，策略会订阅所选品种的逐笔成交数据。
2. 每条成交到来时，计数器加一，直到达到 `Count Limit`。
3. 达到阈值后，策略写入“Count limit X reached. Executing cycle action.”日志，清零等待计数器，并在 `Wait Limit` 为零时立即重启循环，否则进入等待阶段。
4. 等待阶段中，策略对每条成交继续累加等待计数器，直到达到 `Wait Limit`。此时写入“Wait limit X reached. Restarting counting phase.”日志，同时重置两个计数器。
5. 以上步骤会在策略运行期间不断重复。

策略本身不会自动发送订单，保持了 MQL 示例中“Your code goes here”占位的结构。用户可以将日志调用替换为下单操作，或其他需要在固定数量的成交之后执行的行为。

## 转换说明
- 原始脚本使用 `OnTick` 内的全局变量进行计数。移植版本在 `SubscribeTrades().Bind(ProcessTrade)` 回调中维护类字段，确保每个成交只被处理一次。
- `Alert` 与 `Comment` 调用被替换为 `LogInfo`，以便在 StockSharp 的日志系统中提供反馈，而不会强制执行交易动作。
- 参数语义保持不变，只需调整 `Wait Limit` 即可在两个原始示例之间切换。

# 指标参数演示策略

## 概述

`IndicatorParametersDemoStrategy` 是 MetaTrader 示例 *IndicatorParameters_Demo.mq5* 的 StockSharp 高级 API 版本。原始脚本监听图表事件，在指标被添加或删除时输出详细的参数信息。此 C# 策略在 StockSharp 策略环境中复现相同的思想：凡是通过 `TrackIndicator` 注册的指标都会被跟踪，并在添加、删除或手动刷新时写入格式化的参数快照。

策略本身不会发出任何交易指令，因此可以安全地用于实盘或回测环境。它更像是一种诊断和学习工具，展示如何通过代码枚举指标的可配置属性、查看当前值以及理解何时可以获得这些数值。

## 主要功能

- 创建三种常见指标（简单移动平均、指数移动平均、相对强弱指数），并展示如何提取其参数。
- 每次添加指标时记录 `+ added` 日志，清除跟踪器时记录 `- deleted` 日志，模拟 MetaTrader 样例的输出。
- 为每个指标构建一份易读的报告，包含指标类型以及每个可读取属性的名称、类型和值。
- 提供公开方法 `RefreshIndicatorSnapshots`，可以从界面调用以随时重新生成参数报告。
- 通过参数 `LogIndicatorValues` 控制是否在每根完成的 K 线上额外记录指标值。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 为所有指标提供数据的 K 线类型或数据源。请设置为需要观测的时间框架或数据类型。 | 1 分钟 K 线 |
| `SmaLength` | 示例简单移动平均的周期。该值会显示在日志中，便于确认参数如何被记录。 | 20 |
| `EmaLength` | 示例指数移动平均的周期。 | 50 |
| `RsiLength` | 相对强弱指数的周期。 | 14 |
| `LogIndicatorValues` | 为 `true` 时，在每根完成的 K 线之后输出 `[时间戳] 指标数值` 日志；为 `false` 时，仅记录结构性事件，日志更简洁。 | `false` |

所有参数都使用 `StrategyParam<T>` 创建，以便支持校验、界面绑定和优化。若要扩展该示例，只需在 `OnStarted` 中实例化新的指标、绑定到订阅并调用 `TrackIndicator`，跟踪器便会自动把它们纳入报告。

## 日志行为

1. **启动**：`OnStarted` 创建三个示例指标，调用 `TrackIndicator` 并输出 `+ added` 消息。随后生成详细的参数快照，例如：

   ```
   SMA(20) parameter snapshot:
       Length (Int32) = 20
       IsFormed (Boolean) = False
       LastValue (Decimal?) = null
   ```

   报告中的内容取决于指标类型。辅助方法会读取所有可写的公共属性，只要其类型属于基础数值、`decimal`、`string`、`TimeSpan`、`DateTime`、`DateTimeOffset`、`DataType` 或枚举类型。

2. **运行中**：如果启用 `LogIndicatorValues`，在每根 K 线收盘后会写入一条包含 ISO 8601 时间戳和最新数值的日志，方便与市场数据对照。

3. **手动刷新**：调用 `RefreshIndicatorSnapshots()`（例如在界面按钮上绑定）即可重新遍历所有已跟踪的指标并生成新的快照，这与原始 MQL 示例在图表事件触发时的行为一致。

4. **重置**：重写的 `OnReseted` 会清理跟踪器，并为每个指标写入 `- deleted` 日志，展示如何处理指标移除事件。

## 使用步骤

1. 像普通策略一样指定交易品种和投资组合。
2. 根据需要调整 K 线类型以及各个指标的周期参数。
3. 决定是否需要逐 K 线记录指标值，并设置 `LogIndicatorValues`。
4. 启动策略，在日志窗口中查看自动生成的指标说明。
5. （可选）在运行过程中随时调用 `RefreshIndicatorSnapshots` 以获取最新的参数快照。

由于策略不会提交订单，因此无需额外的交易配置。无论在仿真环境还是实盘连接中都可放心使用。

## 与 MQL 版本的差异

- MetaTrader 使用 `CHARTEVENT_CHART_CHANGE` 事件，而在 StockSharp 中需要在创建指标时主动调用 `TrackIndicator` 来记录新增的指标。
- MQL 通过 `IndicatorParameters` 返回 `MqlParam` 数组；本版本使用反射来访问 `IIndicator` 的强类型属性，并输出名称与数值。
- 所有输出均通过 `AddInfoLog` 写入到标准的策略日志体系，而不是调用 `Print`。

## 扩展建议

若要分析更多指标，可在 `OnStarted` 中实例化并绑定这些指标，然后将它们交给 `TrackIndicator`。请为别名选择易于理解的名称（如包含周期或数据源），这样在日志中更清晰。调用 `RefreshIndicatorSnapshots` 即可在运行时获取新的快照，方便在修改参数后立即验证效果。

## 注意事项

- 策略不会发送任何买卖指令，不会影响账户持仓。
- 功能依赖于高层次的 K 线订阅，请确保所选 `CandleType` 被当前数据连接支持。
- 如果在非常小的时间框架上启用 `LogIndicatorValues`，日志量可能很大。此时可以改用手动刷新方式来避免信息过载。

# Trade Protector 策略

## 概览
该策略是 MetaTrader 4 专家顾问 "trade_protector-1_0" 的忠实 StockSharp 版本。原始脚本不会产生交易信号，它的唯一职责是实时监控已有仓位，并根据价格变化动态调整保护性订单。本移植版本完全保留这一思路，通过订阅 level1 行情工作，因此既可以附加到人工交易的账户，也可以与其它自动化策略配合使用。所有距离参数均以点（pip）为单位，与 MQL 输入保持一致。

## 运行逻辑
- 每次收到 level1 更新都会记录最新的买价和卖价，同时检查当前是否持有仓位；若没有持仓，策略保持空闲。
- 对于多头仓位，策略会评估三个候选止损：现有止损、基于浮动利润的比例止损以及经典的跟踪止损。只要浮盈超过 `ProportionalThresholdPips`，比例止损就按照 `entry + ratio * (bid - entry) - spread` 计算；当价格仍然靠近入场价（`bid < entry + 4 * spread`）时，跟踪止损会把止损价维持在距离买价 `TrailingStopPips` 的位置。
- 在所有候选项中选取最大的一个作为新的多头止损，并在提交订单前将价格向下微调一个最小跳动，以避免立刻成交。
- 空头仓位使用镜像逻辑。比例止损在盈利阶段变为 `entry - ratio * (entry - ask) + spread`，而跟踪止损在市场靠近入场价时放置在 `ask + TrailingStopPips + spread`。策略始终保证止损位于卖价之上。
- Escape 模块复刻了原脚本的“脱身”模式：当多头的回撤超过 `EscapeLevelPips`（外加五个最小跳动）时，会挂出 `entry + EscapeTakeProfitPips` 的止盈；空头则在对称的亏损后挂出 `entry - EscapeTakeProfitPips`。参数允许填写负值，从而在可接受的亏损处离场。
- 每当计算出新的保护价位，策略都会重新注册相应的止损或止盈订单。如果价格或数量发生变化，旧订单会先被撤销，确保市场上始终只有一张对应方向的保护单。

## 参数说明
| 参数 | 描述 |
| --- | --- |
| `TrailingStopPips` | 跟踪止损的基础距离（点）。 |
| `ProportionalThresholdPips` | 启动比例止损所需的最小浮动利润（点）。 |
| `ProportionalRatio` | 构建比例止损时使用的利润比例。 |
| `UseEscape` | 是否启用基于回撤的 escape 逻辑及对应的止盈单。 |
| `EscapeLevelPips` | 触发 escape 的亏损距离（点）。设置为 0 时与原 EA 相同（回撤 5 个最小跳动后触发）。 |
| `EscapeTakeProfitPips` | Escape 止盈相对于入场价的距离，可为负值以锁定较小亏损。 |
| `EnableDetailedLogging` | 启用后，每次移动保护性订单都会在日志中输出一条提示信息。 |

## 移植细节
- 点值通过与 MetaTrader `Point` 一致的校正算法转换，包含对三位和五位报价的特殊处理，同样用于 escape 触发条件以保证阈值一致。
- 策略完全基于 level1 数据运行，复刻了原脚本 `start()` 函数的持续监控流程，无需蜡烛或指标。
- 保护性订单通过 StockSharp 的高层接口（`BuyStop`、`SellStop`、`BuyLimit`、`SellLimit`）创建，既贴近原先“修改订单”的概念，又保持了 StockSharp 的最佳实践。
- MQL 中的文件日志被 `EnableDetailedLogging` 控制的 `LogInfo` 信息输出所取代，使代码更简洁，同时便于调试。
- Escape 止盈在被挂出后不会撤销，即使价格随后脱离回撤区间，也会一直保留，这与原版 EA 的行为完全一致。

# Autotrade Pending Stops 策略

## 概述
该策略是 MetaTrader 顾问 *Autotrade (barabashkakvn's edition)* 的 StockSharp 版本。策略始终在当前价格上下维持两张对称的挂单：在价格上方放置 Buy Stop，在价格下方放置 Sell Stop。只要没有持仓，挂单会在每根收盘 K 线时刷新；一旦挂单被触发，系统会根据市场波动情况或绝对盈亏阈值来决定何时平仓。实现过程中严格遵循 AGENTS.md 的要求，并完全使用 StockSharp 的高级 API。

## 参数对照
| StockSharp 参数 | MQL5 输入 | 说明 |
| --- | --- | --- |
| `IndentTicks` | `InpIndent` | 当前价格到挂单的距离（以价格最小跳动数表示）。 |
| `MinProfit` | `MinProfit` | 在行情趋于平静时触发平仓所需的最小浮动盈利（账户货币）。 |
| `ExpirationMinutes` | `ExpirationMinutes` | 挂单的存活时间，超时后挂单会被取消并在下一根 K 线重建。 |
| `AbsoluteFixation` | `AbsoluteFixation` | 触发强制平仓的绝对盈亏阈值（账户货币）。 |
| `StabilizationTicks` | `InpStabilization` | 前一根 K 线实体的最大允许长度，用于识别盘整行情。 |
| `OrderVolume` | `Lots` | Buy Stop 与 Sell Stop 的下单手数。 |
| `CandleType` | `Period()` | 驱动策略的 K 线类型（默认 1 分钟）。 |

所有以“点”为单位的距离都会根据 `Security.PriceStep` 转换为实际价格跳动。盈亏阈值通过 `Security.StepPrice` 计算，以便与原始 MQL5 版本使用的账户货币结果一致。

## 交易流程
### 挂单布置
1. 只处理状态为 `CandleStates.Finished` 的完整 K 线。
2. 第一根 K 线用于初始化历史数据（上一根开/收价），随后立即放置挂单。
3. 当仓位为零时会清理失效引用，然后：
   - 在 `Close + IndentTicks * PriceStep` 处放置 Buy Stop；
   - 在 `Close - IndentTicks * PriceStep` 处放置 Sell Stop。
4. 每张挂单的到期时间均设为 `CloseTime + ExpirationMinutes` 分钟；一旦过期便取消，并在下一根 K 线上重新创建。

### 仓位管理
1. 当其中一张挂单成交后，会立刻取消另一张挂单，以避免在 StockSharp 的净额模型下产生对冲仓位。
2. 策略保存上一根 K 线的实体长度（`|Open - Close|`），用于判断市场是否进入低波动区间。
3. 当持仓存在时：
   - 根据 `PositionAvgPrice` 计算当前浮动盈亏（使用 `PriceStep` 和 `StepPrice` 转换为货币单位）。
   - 若浮盈超过 `MinProfit` 且上一根 K 线实体小于 `StabilizationTicks * PriceStep`，则以市价平仓。
   - 无论波动如何，只要绝对盈亏超过 `AbsoluteFixation`，也会立即平仓。
4. 仓位归零后，所有剩余的挂单会被彻底移除。

### 其他行为
- 策略始终保持单向净头寸，`OrderVolume` 会同步设置策略的 `Volume`。
- 在多数回测场景中缺乏实时买卖盘，因此挂单价格基于完成 K 线的收盘价计算。
- 下单前会检查 `IsFormedAndOnlineAndAllowTrading()`，确保数据已就绪且允许交易。

## 实现差异与注意事项
- 盈亏换算依赖 `Security.PriceStep` 与 `Security.StepPrice`。若交易品种未提供这些值，代码会退化为使用默认值 `1`，需要在接入市场数据前确认配置正确。
- 原始 MQL5 版本允许对冲式的双向仓位；本移植版在挂单成交后立即撤销另一张挂单，以适配 StockSharp 的净额账户模型。
- 挂单到期时间基于 K 线的 `CloseTime`。如果数据源缺失该字段，需要扩展数据适配层以提供有效时间戳。
- 通过调整 `CandleType` 可以轻松切换不同的时间框架或其他类型的蜡烛图数据。

## 使用建议
1. 将 `CandleType` 设置为与原始策略相同的周期，以保持交易节奏一致。
2. 根据品种的最小跳动和 tick 价值调节 `IndentTicks`、`StabilizationTicks`、`MinProfit` 与 `AbsoluteFixation`。
3. 确认账户模式（净额/对冲）。策略假设为净额模式，会在重新布置挂单前确保仓位归零。
4. 在 StockSharp Designer 或 Backtester 中利用参数进行优化，以适配不同市场或交易品种。
5. 关注日志输出：策略仅在收到完整数据且交易被允许时才会提交新订单。

## 风险提示
量化交易存在较高风险。请在历史数据上充分回测、验证参数，并确保满足券商关于挂单最小距离等限制后再用于真实账户。

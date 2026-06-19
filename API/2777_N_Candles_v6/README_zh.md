# N Candles v6 策略

## 概述
**N Candles v6** 策略跟踪最近完成的蜡烛，寻找方向完全相同的连续序列。当市场连续出现 `N` 根阳线时，策略建立多头仓位；连续出现 `N` 根阴线时则开立空头。该方案源自 MetaTrader 专家顾问 *N Candles v6.mq5*，并针对 StockSharp 高级 API 重新实现。

该算法适用于任何提供常规时间周期蜡烛的品种。可以通过交易时间窗口控制进场时段，而已经建立的仓位在窗口关闭时仍然受到止损、止盈与追踪止损的保护。

## 交易逻辑
1. 订阅设定的蜡烛类型，只处理状态为 `Finished` 的蜡烛。
2. 统计连续的阳线（`Close > Open`）与阴线（`Close < Open`），十字线会重置计数。
3. 当检测到 `CandlesCount` 根阳线：
   - 预测加仓后的净头寸不超过 `MaxPositionVolume`。
   - 发送市价买单；若当前持有空头，会自动增加手数以一次性反向至多头。
4. 当检测到 `CandlesCount` 根阴线：
   - 确认新的空头不会突破 `MaxPositionVolume`。
   - 发送市价卖单；若当前持有多头，同样会扩大手数以完成反向。
5. 若最新蜡烛破坏了连阳/连阴结构（“黑羊”）：
   - 根据 `ClosingMode` 仅执行一次平仓操作：全部平仓、只平反向仓，或只平顺势仓。
6. 每根蜡烛都会执行风控：
   - 止损与止盈按点数和 `PriceStep` 计算为绝对价差。
   - 价格在有利方向移动 `TrailingStopPips + TrailingStepPips` 后启动追踪止损，并且只沿盈利方向推进。
   - 一旦价格触及止损、止盈或追踪止损，立即平掉全部仓位。

## 风险控制
- **Stop Loss (pips)**：将点数转换为绝对价格距离；对于 5 位或 3 位小数报价，会自动放大 10 倍以符合传统“pip”定义。
- **Take Profit (pips)**：达到设定盈利点数后平仓，设置为 `0` 则关闭该功能。
- **Trailing Stop / Step (pips)**：启用追踪止损后，价格至少移动指定的点数才会更新止损位置；当 `TrailingStopPips > 0` 时，`TrailingStepPips` 必须大于 0。
- **Max Position Volume**：限制净头寸的绝对值，超出限制的信号会被忽略。
- **Closing Mode**：决定出现“黑羊”时的处理方式：
  - `All` – 平掉全部仓位。
  - `Opposite` – 仅平掉与连阳/连阴方向相反的仓位。
  - `Unidirectional` – 仅平掉与连阳/连阴方向相同的仓位。
- **交易时间窗口**：只有当蜡烛的开盘时间处于 `StartHour` 与 `EndHour`（包含端点）之间时才允许开仓；保护性平仓逻辑始终有效。

## 参数
| 名称 | 默认值 | 说明 |
|------|--------|------|
| `CandlesCount` | 3 | 触发信号所需的连续同向蜡烛数量。 |
| `OrderVolume` | 0.01 | 基础下单手数；若存在反向仓位，会附加相应数量以完成反向。 |
| `TakeProfitPips` | 50 | 止盈点数，`0` 为关闭。 |
| `StopLossPips` | 50 | 止损点数，`0` 为关闭。 |
| `TrailingStopPips` | 10 | 追踪止损的距离，`0` 为关闭。 |
| `TrailingStepPips` | 4 | 每次更新追踪止损所需的最小价格改进；启用追踪止损时必须 > 0。 |
| `MaxPositionVolume` | 2 | 净头寸的最大绝对值。 |
| `UseTradingHours` | true | 是否启用交易时间过滤。 |
| `StartHour` | 11 | 允许开仓的起始小时（0-23）。 |
| `EndHour` | 18 | 允许开仓的结束小时（0-23）。 |
| `ClosingMode` | All | 出现“黑羊”时的平仓策略。 |
| `CandleType` | 1 小时蜡烛 | 用于信号计算的数据类型。 |

## 其他说明
- Pip 换算基于 `PriceStep`，对 5 位或 3 位小数报价会自动乘以 10。
- 策略启动时调用 `StartProtection()`，以启用 StockSharp 提供的安全保护机制（异常断线、撤单等）。
- 策略依据净头寸 (`Strategy.Position`) 运作，适合净额账户；通过提高 `MaxPositionVolume` 可近似实现多头/空头同时持仓的效果。

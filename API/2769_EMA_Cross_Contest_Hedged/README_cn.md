# EMA Cross Contest Hedged 策略

## 概览
- 将 MetaTrader 平台上的 “EMA Cross Contest Hedged” 策略迁移到 StockSharp 的高级 API。
- 使用一组快慢 EMA 判断趋势，并可选用 MACD 主线作为信号过滤器。
- 每次入场后会建立四个分层的止损挂单（对冲阶梯），在行情延续时逐步加仓。
- 通过以点（pip）表示的固定止损/止盈以及可调节的跟踪止损控制风险。
- `TradeBar` 参数允许选择基于当前已完成 K 线还是上一根 K 线来判定交叉信号。

## 指标与数据
- 可配置长度的快 EMA（默认 4）。
- 可配置长度的慢 EMA（默认 24），要求快 EMA 周期小于慢 EMA 周期。
- MACD(4, 24, 12) 主线，用于可选的方向确认。
- 支持任何由 `CandleType` 参数提供的时间周期，默认使用 15 分钟 K 线。

## 入场逻辑
1. 等待所选时间周期的 K 线收盘。
2. 计算快、慢 EMA 的最新值。根据 `TradeBar` 选项使用以下其中一种组合来判断交叉：
   - `Current`：使用最新收盘与上一根收盘。
   - `Previous`（默认）：使用上一根收盘与更早一根收盘。
3. 当快 EMA 向上穿越慢 EMA 时生成多头信号；若启用 `UseMacdFilter`，对应 K 线的 MACD 值需大于等于零。
4. 当快 EMA 向下穿越慢 EMA 时生成空头信号；若启用过滤器，MACD 值需小于等于零。
5. 仅在当前无持仓时开立新仓位。
6. 以 `OrderVolume` 指定的手数市价入场，随后：
   - 按照 `StopLossPips` 和 `TakeProfitPips` 计算并保存止损/止盈价格。
   - 重置跟踪止损状态。
   - 按 `HedgeLevelPips` 的步长在趋势方向挂出四个止损单，带有相同的止损与止盈设置，每个挂单的有效期由 `PendingExpirationSeconds` 控制。

## 出场与持仓管理
- **止损 / 止盈：** 监控 K 线内的最高价和最低价，触及任一保护价位即平掉全部仓位。
- **跟踪止损：** 当浮动盈利超过 `TrailingStopPips + TrailingStepPips` 时，将止损上移/下移至距离最新收盘价 `TrailingStopPips` 的位置，随后继续随行情移动。
- **反向交叉：** 如果开启 `CloseOppositePositions`，在出现反向 EMA 交叉时立即平仓。
- **对冲阶梯：** 每个挂单被触发后会执行等量市价单，加仓的同时根据成交价调整平均持仓价及保护水平。

## 参数说明
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | 0.1 | 每次市价单和挂单的交易量。 |
| `StopLossPips` | 140 | 止损距离（点）。设置为 0 表示不使用固定止损。 |
| `TakeProfitPips` | 120 | 止盈距离（点）。设置为 0 表示不使用固定止盈。 |
| `TrailingStopPips` | 30 | 跟踪止损距离（点）。0 表示关闭跟踪止损。 |
| `TrailingStepPips` | 1 | 每次更新跟踪止损前所需的额外盈利（点）。 |
| `HedgeLevelPips` | 6 | 分层挂单之间的间隔（点）。 |
| `CloseOppositePositions` | false | 出现反向交叉时是否立即平仓。 |
| `UseMacdFilter` | false | 是否要求 MACD 主线确认（多头 >= 0，空头 <= 0）。 |
| `PendingExpirationSeconds` | 65535 | 每个对冲挂单的有效时间（秒）。 |
| `ShortMaPeriod` | 4 | 快 EMA 周期，必须小于 `LongMaPeriod`。 |
| `LongMaPeriod` | 24 | 慢 EMA 周期。 |
| `TradeBar` | Previous | 交叉检测所使用的 K 线组合。 |
| `CandleType` | 15 分钟 | 策略请求的数据时间周期。 |

## 额外说明
- 点值换算方式为 `PriceStep × 点数`，对于 3 或 5 位小数的品种会自动乘以 10，以符合 MetaTrader 的点值定义。
- 对冲挂单在策略内部模拟，当 K 线的最高价或最低价触及某一级别时立即执行。
- 策略在启动时调用 `StartProtection()`，启用 StockSharp 自带的保护机制。
- 多头与空头拥有独立的跟踪止损状态，以贴近原始 MQL 实现的对冲逻辑。

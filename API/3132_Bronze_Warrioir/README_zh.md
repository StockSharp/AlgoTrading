# Bronze Warrioir 策略

## 概述
- 将 MetaTrader 5 智能交易系统 *Bronze Warrioir.mq5* 转写为 StockSharp 高级 API 版本。
- 使用完成的 K 线，并联合 CCI、Williams %R 以及自定义的 DayImpuls 振荡器来判断多空动能。
- 目标是在三项指标同向共振时捕捉加速行情，同时保留原始 EA 的权益控制逻辑。

## 指标组合
- **CCI（商品通道指数）**：周期为 `IndicatorPeriod`。做空时要求 CCI 大于 `CciLevel`，做多时要求 CCI 小于 `-CciLevel`。
- **Williams %R**：同样使用 `IndicatorPeriod`。值高于 `WilliamsLevelUp` 代表超买，值低于 `WilliamsLevelDown` 代表超卖。
- **DayImpuls 振荡器**：复刻随附的自定义指标。每根 K 线的实体（收盘价减开盘价）被转换为点值，再经过两次相同周期的指数移动平均平滑。指标上行表示多头动能增强，下行表示空头动能增强。

## 交易逻辑
1. **权益保护**：统计当前净头寸的浮动盈亏，超过 `ProfitTarget` 或低于 `LossTarget` 时立即平仓所有仓位。
2. **信号过滤**：仅处理状态为 `Finished` 的 K 线，并缓存前一根 DayImpuls 值以模拟原版中 `custom[1]` 的效果。
3. **做空条件**：
   - 当前没有空头持仓。
   - DayImpuls 高于 `DayImpulsLevel` 且大于上一根的数值。
   - Williams %R 高于 `WilliamsLevelUp`，CCI 大于 `CciLevel`。
   - 在 StockSharp 的净持仓模型中，卖出量为 `TradeVolume` 加上已有多头头寸，以一次性完成反手。
4. **做多条件**：
   - 当前没有多头持仓。
   - DayImpuls 低于 `DayImpulsLevel` 且小于上一根的数值。
   - Williams %R 低于 `WilliamsLevelDown`，CCI 小于 `-CciLevel`。
   - 买入量为 `TradeVolume` 加上已有空头头寸，同样实现单笔反向操作。
5. **“对冲式”反转**：当只持有单向仓位且浮动盈亏离开区间 `[-PredTarget/2, PredTarget]` 时，原 EA 会检查 `LotCoefficient` 后追加反向仓位。在本移植版本中仍然保留该校验，但由于 StockSharp 采用净持仓模式，执行方式为“先平后开”的反手市价单。

## 风险控制
- `StopLossPips` 与 `TakeProfitPips` 会根据标的的 `PriceStep` 转换为价格距离，对 3 或 5 位小数的外汇品种额外乘以 10 以模拟 MT5 的“点”。
- 通过 `StartProtection` 启动高阶保护，自动附加止损与止盈。
- 通过 `OnOwnTradeReceived` 维护多头与空头的平均持仓价，从而重现原策略中 `Commission + Swap + Profit` 的浮动盈亏计算。

## 参数列表
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 每次开仓的基础手数。 | `1` |
| `StopLossPips` | 以点数表示的止损距离。 | `50` |
| `TakeProfitPips` | 以点数表示的止盈距离。 | `50` |
| `IndicatorPeriod` | CCI、Williams %R、DayImpuls 共用周期。 | `14` |
| `CciLevel` | CCI 多空触发阈值。 | `150` |
| `WilliamsLevelUp` | Williams %R 超买阈值。 | `-15` |
| `WilliamsLevelDown` | Williams %R 超卖阈值。 | `-85` |
| `DayImpulsLevel` | DayImpuls 判定多空的临界值。 | `50` |
| `ProfitTarget` | 浮动盈利目标（账户货币）。 | `100` |
| `LossTarget` | 浮动亏损上限（账户货币）。 | `-100` |
| `PredTarget` | 触发反转的盈亏带宽。 | `40` |
| `LotCoefficient` | 原 EA 的加仓校验系数。 | `2` |
| `CandleType` | 使用的 K 线周期。 | `15m` |

## 实现细节
- DayImpuls 被实现为内部指标类，完整复刻两次指数平滑的计算过程。
- 由于无法在净持仓模式下同时持有多空，策略在需要对冲时通过一次性反手单来近似原始逻辑。
- 仅在 `IsFormedAndOnlineAndAllowTrading()` 为真时执行信号，保证与 StockSharp 生命周期兼容。
- 反手或部分平仓后的持仓均通过内部体量追踪更新平均价格，确保浮动盈亏计算准确。

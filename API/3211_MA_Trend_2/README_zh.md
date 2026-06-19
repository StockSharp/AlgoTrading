# MA Trend 2 策略

## 摘要
- 基于 MetaTrader 5 专家顾问 `MA Trend 2.mq5` 改写。
- 利用可配置的移动平均线判断价格与均线的相对位置并触发交易。
- 支持止损/止盈、阶梯式移动止损以及按固定手数或风险百分比下单。

## 策略逻辑
1. 订阅用户选定的 K 线类型，并按设置的周期、平滑方式、移位与价格来源计算移动平均。
2. 每当 K 线收盘，把最新的均线值存入短历史列表，以便取出前一根（加上 `MaShift`）作为参考值。
3. 当收盘价高于参考均线且允许做多时发送买入信号；当收盘价低于参考均线且允许做空时发送卖出信号。启用 `ReverseSignals` 时条件互换。
4. 下单前检查 `OnlyOnePosition` 与 `CloseOppositePositions`：可选择仅在仓位为空时进场，或先用同一张订单平掉反向仓位再反手。
5. 下单手数可固定，也可按账户风险百分比计算。风险模式会把止损距离换算成价格，并估算实现目标风险所需的成交量，随后按 `VolumeStep` 向上取整。
6. 移动止损与原始 EA 一致：只有当浮盈超过 `TrailingStopPips + TrailingStepPips` 时才推进，并且永不放松。当价格回落至移动止损以下（做多）或以上（做空）时立即市价离场。
7. 通过高层 API `StartProtection` 附加止损与止盈，使撮合器能够在 K 线之间执行保护。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `StopLossPips` | 止损距离（点）。为 `0` 时关闭。 | `50` |
| `TakeProfitPips` | 止盈距离（点）。为 `0` 时关闭。 | `140` |
| `TrailingStopPips` | 移动止损基础距离（点）。 | `15` |
| `TrailingStepPips` | 移动止损每次推进所需的额外利润（点）。 | `5` |
| `LotMode` | `FixedVolume` 直接使用 `LotOrRiskValue`；`RiskPercent` 把它视为风险百分比。 | `RiskPercent` |
| `LotOrRiskValue` | 固定手数或风险百分比。 | `3` |
| `MaPeriod` | 移动平均周期。 | `12` |
| `MaShift` | 当前 K 线与参考均线之间的已完成 K 线数量。 | `3` |
| `MaMethod` | 移动平均类型（简单、指数、平滑、线性加权）。 | `LinearWeighted` |
| `MaPrice` | 均线使用的价格字段。 | `Weighted` |
| `CandleType` | 订阅的 K 线类型。 | `1 分钟` |
| `Direction` | 允许的方向（多头/空头/双向）。 | `Both` |
| `OnlyOnePosition` | 是否限制只持有一个方向的仓位。 | `false` |
| `ReverseSignals` | 是否反转买卖信号。 | `false` |
| `CloseOppositePositions` | 是否在开仓前先平掉反向仓位。 | `false` |

## 资金管理
- 风险百分比模式会读取证券的 `PriceStep` 与 `StepPrice`，把点值转换为绝对价格距离。
- 使用投资组合当前市值（若不可得则退回初始市值）计算风险预算。
- 计算出的手数向上取整到最接近的 `VolumeStep`，避免因最小变动不符合而被拒单。

## 移动止损
- 所有距离都以点表示，代码会根据交易品种的点值自动换算为价格。
- 多头仓位在满足阈值后把止损推进到 `Close - TrailingStopPips`，并要求每次推进至少间隔 `TrailingStepPips`。
- 空头仓位采用对称逻辑：价格上行超出阈值时下调止损；一旦触及即平仓。

## 转换说明
- 全部交易调用 StockSharp 高层 API（`BuyMarket`、`SellMarket`、`StartProtection`）。
- 仅保留极短的均线历史（移位值加额外缓冲），即可重现前一根均线参考而无需庞大数据结构。
- 代码中的注释均使用英文，方便跨团队协作。

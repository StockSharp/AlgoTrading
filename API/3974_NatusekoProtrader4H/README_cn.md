# Natuseko Protrader 4H 策略

## 概述
Natuseko Protrader 4H 策略是 MetaTrader 4 专家顾问 *NatusekoProtrader4HStrategy* 的 StockSharp 版本。原始 EA 在四小时级别上
同时使用指数移动平均线（EMA）、经布林带过滤的 MACD、RSI 阈值以及抛物线 SAR 来寻找强劲的突破蜡烛。本移植版保留了相同的
入场、等待回调以及分批止盈管理流程。

## 交易逻辑
1. 订阅由 `CandleType` 定义的主时间框架（默认 4 小时），仅处理收盘蜡烛。
2. 在收盘价上计算三条 EMA：快速、慢速与趋势 EMA。
3. 计算 MACD 主线，并对其应用简单移动平均线与布林带。布林带的中轨作为多空判断的参考。
4. 计算收盘价 RSI 以及抛物线 SAR，二者共同决定入场与离场。
5. 当满足以下全部条件时生成多头信号：快速 EMA 高于慢速与趋势 EMA；RSI 位于 `RsiEntryLevel` 与 `RsiTakeProfitLong`
   之间；MACD 主线高于其自身的 SMA 与布林中轨；蜡烛实体大于上下影线；抛物线 SAR 位于收盘价之下。
6. 空头信号使用完全对称的条件：EMA 顺序反转、RSI 位于 `RsiTakeProfitShort` 与 `RsiEntryLevel` 之间、MACD 位于布林中轨
   之下、看跌实体并且 SAR 在收盘价上方。
7. 如果信号蜡烛距离趋势 EMA 过远（超过 `DistanceThresholdPoints` 点），策略会记录等待标志并观察回调。价格回落触及快速 EMA
   且 RSI、SAR 仍支持原方向时再入场；空头端的逻辑相同。
8. 满足条件时策略会平掉相反持仓并按 `TradeVolume` 开仓。止损优先使用抛物线 SAR（若启用 `UseSarStopLoss`），否则使用趋势 EMA，
   并将 `StopOffsetPoints` 换算成价格增量叠加在止损价上。
9. 持有多头期间，每根蜡烛都会重新计算止损并执行以下管理：
   - 价格跌破止损立即平仓全部多头；
   - 当盈利达到 `MinimumProfitPoints` 点以上且未分批时，如果 RSI 高于 `RsiTakeProfitLong` 或 SAR 翻到价格上方，就平仓一半；
   - 当盈利满足条件且 RSI 回落到 `RsiEntryLevel` 以下时，平掉剩余多头。
10. 空头管理与上述规则完全镜像，只是阈值方向相反。

## 仓位管理
- 每个方向最多只进行一次分批止盈，之后等待 RSI 回到中枢或止损触发再平掉剩余头寸。
- 止损价格会随着最新的 SAR 或趋势 EMA 每根蜡烛更新，以贴合 MQL 的做法。
- 当仓位归零时，等待标志、止损引用与分批标记都会重置。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 4 小时 | 主时间框架。 |
| `TradeVolume` | `decimal` | `0.1` | 每次开仓数量。 |
| `FastEmaPeriod` | `int` | `13` | 快速 EMA 长度。 |
| `SlowEmaPeriod` | `int` | `21` | 慢速 EMA 长度。 |
| `TrendEmaPeriod` | `int` | `55` | 趋势 EMA 长度，同时用于止损与距离判定。 |
| `MacdFastPeriod` | `int` | `5` | MACD 快速 EMA 长度。 |
| `MacdSlowPeriod` | `int` | `200` | MACD 慢速 EMA 长度。 |
| `MacdSignalPeriod` | `int` | `1` | MACD 信号线长度。 |
| `BollingerPeriod` | `int` | `20` | 计算 MACD 布林带的样本数。 |
| `BollingerWidth` | `decimal` | `1` | MACD 布林带的标准差倍数。 |
| `MacdSmaPeriod` | `int` | `3` | MACD 主线的平滑 SMA 长度。 |
| `RsiPeriod` | `int` | `21` | RSI 长度。 |
| `RsiEntryLevel` | `decimal` | `50` | RSI 中枢阈值。 |
| `RsiTakeProfitLong` | `decimal` | `65` | 多头分批止盈 RSI 阈值。 |
| `RsiTakeProfitShort` | `decimal` | `35` | 空头分批止盈 RSI 阈值。 |
| `DistanceThresholdPoints` | `decimal` | `100` | 价格相对趋势 EMA 的最大距离（点）。 |
| `SarStep` | `decimal` | `0.02` | 抛物线 SAR 加速步长。 |
| `SarMaximum` | `decimal` | `0.2` | 抛物线 SAR 最大加速度。 |
| `UseSarStopLoss` | `bool` | `false` | 是否使用 SAR 作为止损。 |
| `UseTrendStopLoss` | `bool` | `true` | 是否使用趋势 EMA 作为止损。 |
| `StopOffsetPoints` | `int` | `0` | 止损附加点数。 |
| `UseSarTakeProfit` | `bool` | `true` | 是否在 SAR 反向时分批止盈。 |
| `UseRsiTakeProfit` | `bool` | `true` | 是否在 RSI 超过阈值时分批止盈。 |
| `MinimumProfitPoints` | `decimal` | `5` | 启用止盈规则所需的最小盈利（点）。 |

## 与原 EA 的差异
- StockSharp 采用净头寸模型，因此在反向开仓前会先平掉现有持仓，以模拟 MT4 单票逻辑。
- 止盈止损通过市价单执行，而不是修改订单属性，因为 StockSharp 不维护 MT4 式的单独订单。效果与原 EA 分批+全部平仓的逻辑一致。
- 距离与盈利阈值会按交易品种的 `PriceStep` 换算。如果标的未提供价格步长，则默认使用 1，可根据需要调整相关参数。

## 使用建议
- 根据标的合约大小调整 `TradeVolume`，构造函数也会同步设置 `Strategy.Volume` 以便辅助方法使用正确的手数。
- 若经常因为价格离趋势 EMA 太远而错过交易，可降低 `DistanceThresholdPoints` 或设为 0 关闭该过滤。
- 建议开启图表：策略会绘制蜡烛、三条 EMA、RSI、抛物线 SAR 以及 MACD 布林带，方便对照 MQL 行为。
- MACD 参数完全复制原策略（5/200/1），该组合非常平滑但滞后，实盘前可以考虑优化。

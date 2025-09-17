# Divergence MACD Stochastic 策略

该策略将 MetaTrader 5 专家顾问 **“Divergence EA pip sl tp”** 迁移到 StockSharp 框架。算法在价格行为与 MACD 柱状图之间寻找经典背离，并通过随机指标的超买/超卖过滤进行确认，然后执行反转交易。

## 交易逻辑

1. 根据 `CandleType` 参数订阅主要时间框的K线。
2. 在每根收盘K线上计算 MACD 柱状图（`MACD 线 - Signal 线`）以及随机指标 %K/%D。
3. 维护价格与柱状图最近两个高点和低点，以判断背离。
4. **看跌背离**：价格创出更高高点，但 MACD 柱状图峰值更低，同时 %K 高于 `StochasticUpperLevel`，则开空或反手离场多头。
5. **看涨背离**：价格创出更低低点，但柱状图谷值更高，同时 %K 低于 `StochasticLowerLevel`，则开多或反手离场空头。
6. 可选的 `TakeProfitSteps` 与 `StopLossSteps` 会转换成价格步长单位，并在策略启动时一次性启用风险保护。

## 实现细节

- 使用 StockSharp 高级 API，通过单一K线订阅绑定 `MovingAverageConvergenceDivergenceSignal` 与 `StochasticOscillator` 指标。
- 背离状态完全保存在内部字段中，不调用 `GetValue` 方法，符合转换规范。
- 若存在图表区域，会绘制价格K线、MACD 与随机指标以及成交记录。
- 触发信号时通过在基础 `Volume` 上叠加当前持仓的绝对值来实现快速反手。

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 用于计算背离的时间框。 | 1 小时K线 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD 的快慢 EMA 及信号线长度。 | 12 / 26 / 9 |
| `MacdDivergenceThreshold` | 连续柱状图极值之间所需的最小差值。 | 0.0005 |
| `StochasticLength` | 随机指标快速 %K 的周期。 | 50 |
| `StochasticSlowK`, `StochasticSlowD` | %K 与 %D 的平滑长度。 | 9 / 9 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | 用于确认看跌/看涨信号的超买与超卖阈值。 | 80 / 20 |
| `TakeProfitSteps`, `StopLossSteps` | 以价格步长表示的止盈/止损距离（0 表示禁用）。 | 50 |

## 使用方法

1. 将策略连接到支持所选时间框的 StockSharp 交易通道。
2. 通过 `Volume` 设置基础手数，并根据需求调整指标参数。
3. 启动策略，当背离与随机指标条件满足时会自动发送订单。

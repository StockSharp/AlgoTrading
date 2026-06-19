# Polish Layer 策略

## 概述
**Polish Layer** 策略源自 `MQL/17484` 的 MetaTrader 智能交易系统，现已移植到 StockSharp 的高层 API。策略关注外汇市场的短期趋势延续，默认使用 5 分钟或 15 分钟 K 线。趋势方向由快、慢指数移动平均线（EMA）的相对位置和 RSI 的动量斜率决定，入场需要 Stochastic、DeMarker 与 Williams %R 三个振荡指标同时给出突破信号。

## 指标
- **指数移动平均线 (EMA)** —— `ShortEmaPeriod` 与 `LongEmaPeriod` 组成的快慢趋势过滤器。
- **相对强弱指数 (RSI)** —— 根据前两根 K 线的值评估动量变化。
- **随机振荡指标 (Stochastic Oscillator)** —— 通过 %K 线突破水平判断超买/超卖反转。
- **DeMarker 指标** —— 辨别市场的吸筹与派发阶段。
- **Williams %R** —— 在极值区域确认动量反转。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `ShortEmaPeriod` | 9 | 快速 EMA 的周期。 |
| `LongEmaPeriod` | 45 | 慢速 EMA 的周期。 |
| `RsiPeriod` | 14 | RSI 的计算周期。 |
| `StochasticKPeriod` | 5 | %K 线的周期。 |
| `StochasticDPeriod` | 3 | %D 线的平滑周期。 |
| `StochasticSlowing` | 3 | %K 的最终平滑系数。 |
| `WilliamsRPeriod` | 14 | Williams %R 的回看窗口。 |
| `DeMarkerPeriod` | 14 | DeMarker 指标的回看窗口。 |
| `TakeProfitPoints` | 17 | 止盈距离（按 `Security.PriceStep` 转换为价格）。 |
| `StopLossPoints` | 77 | 止损距离（按价格步长计算）。 |
| `CandleType` | 5 分钟 | 策略使用的 K 线类型。 |
| `Volume` | 1 | 每次下单的交易量。 |

## 交易逻辑
1. **趋势过滤**：上一根 K 线的快速 EMA 必须高于慢速 EMA，同时上一根 RSI 要高于两根之前的 RSI 才允许做多；做空信号则相反。
2. **振荡指标确认**：仅在没有持仓时检查以下条件：
   - Stochastic %K 上穿 19 触发做多，下穿 81 触发做空。
   - DeMarker 上穿 0.35（多头）或下破 0.63（空头）。
   - Williams %R 上穿 -81（多头）或下破 -19（空头）。
3. **下单执行**：满足条件后调用 `BuyMarket(Volume)` 或 `SellMarket(Volume)`，并通过 `StartProtection` 自动附加止盈止损。

## 风险控制
- `StartProtection` 会根据 `PriceStep` 将 `TakeProfitPoints` 与 `StopLossPoints` 换算为绝对价格差，并自动维护保护单。
- 只有在已有仓位通过止盈或止损离场后，策略才会寻找新的交易机会，从而与原始 EA 的行为保持一致。

## 使用建议
- 适用于流动性高的外汇品种，推荐 5 分钟或 15 分钟周期。
- 请确认交易品种已正确设置 `PriceStep`，必要时调整止盈、止损参数以匹配最小报价单位。
- 由于多重指标需要同步确认，建议在真实交易前进行前向测试以验证滑点与数据差异的影响。

# Genie Stoch RSI 策略

该策略结合相对强弱指数（RSI）和随机指标（Stochastic Oscillator）。
当市场进入超买或超卖区域时，策略等待 Stochastic 主线与信号线的交叉来确认可能的反转。
同时使用移动止损和固定止盈进行风险控制。

## 逻辑

1. 订阅所选时间框架的K线。
2. 计算具有可配置周期的 RSI。
3. 计算具有可配置 %K、%D 及减缓参数的 Stochastic。
4. 做多条件：
   - RSI 低于超卖水平；
   - %K 低于 Stochastic 的超卖水平；
   - 上一根 %K 低于上一根 %D 且当前 %K 向上穿越当前 %D。
5. 做空条件：
   - RSI 高于超买水平；
   - %K 高于 Stochastic 的超买水平；
   - 上一根 %K 高于上一根 %D 且当前 %K 向下穿越当前 %D。
6. 持仓量来自策略的 `Volume` 属性，如出现反向信号则反手。
7. `StartProtection` 使用价格点数设置的止盈和移动止损。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `RsiPeriod` | RSI 计算周期。 |
| `KPeriod` | Stochastic %K 周期。 |
| `DPeriod` | Stochastic %D 周期。 |
| `Slowing` | Stochastic 减缓值。 |
| `RsiOverbought` | RSI 超买水平。 |
| `RsiOversold` | RSI 超卖水平。 |
| `StochOverbought` | Stochastic 超买水平。 |
| `StochOversold` | Stochastic 超卖水平。 |
| `TakeProfit` | 止盈距离（价格点）。 |
| `TrailingStop` | 移动止损距离（价格点）。 |
| `CandleType` | 分析用的K线类型与时间框架。 |

## 注意

策略仅处理已完成的K线，直到所有指标形成后才产生信号。
该示例仅供学习使用，实际交易前需要充分测试。

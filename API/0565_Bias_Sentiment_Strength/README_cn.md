# Bias And Sentiment Strength 策略
[English](README.md) | [Русский](README_ru.md)

该策略将多种动量和成交量指标（MACD、RSI、Stochastic、Awesome Oscillator、Alligator 平滑均线和成交量偏差）汇总成一个数值。当综合偏向大于零时做多，小于零时做空。

## 详情

- **入场条件**：
  - **多头**：综合数值 > 0。
  - **空头**：综合数值 < 0。
- **多/空**：双向。
- **出场条件**：信号反转。
- **止损**：通过 `StopLossPercent` 设置百分比止损。
- **默认值**：
  - MACD 快线 12，慢线 26，信号线 9。
  - RSI 周期 14。
  - Stochastic 周期 21/14/14。
  - AO 周期 5/34。
  - 成交量偏差长度 30。
- **过滤**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：有
  - 复杂度：复杂
  - 时间框架：中期

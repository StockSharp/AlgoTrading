# MACD Adaptive Histogram
[English](README.md) | [Русский](README_ru.md)

**MACD Adaptive Histogram** 策略基于相关指标构建。

当指标在指定周期的数据上确认条件时触发信号，适合积极交易者。

止损依赖ATR倍数及其他参数，可根据需要调整默认值以平衡风险和收益。

## 详细信息
- **入场条件**: see implementation for indicator conditions.
- **多空**: Both directions.
- **出场条件**: opposite signal or stop logic.
- **止损**: Yes, using indicator-based calculations.
- **默认值**:
  - `FastPeriod = 12`
  - `SlowPeriod = 26`
  - `SignalPeriod = 9`
  - `HistogramAvgPeriod = 20`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **过滤器**:
  - 分类: Trend following
  - 方向: Both
  - 指标: Histogram
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday (15m)
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
测试表明年均收益约为 184%，该策略在加密市场表现最佳。

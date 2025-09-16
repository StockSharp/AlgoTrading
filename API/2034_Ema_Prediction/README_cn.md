# EMA Prediction 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 EMA Prediction 指标，当快速和慢速指数移动平均线在确认方向的 K 线中发生交叉时产生信号。

当快速 EMA 在看涨 K 线上向上穿过慢速 EMA 时，策略开多并关闭所有空头。当快速 EMA 在看跌 K 线上向下穿过慢速 EMA 时，策略开空并关闭所有多头。

## 细节

- **入场条件**：
  - 多头：快速 EMA 上穿慢速 EMA 且 K 线为阳线。
  - 空头：快速 EMA 下穿慢速 EMA 且 K 线为阴线。
- **多/空**：双向
- **离场条件**：反向信号
- **止损**：固定止盈与止损
- **默认值**：
  - `CandleType` = 6 小时 K 线
  - `FastPeriod` = 1
  - `SlowPeriod` = 2
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
- **过滤器**：
  - 类别：均线交叉
  - 方向：双向
  - 指标：EMA
  - 止损：止盈与止损
  - 复杂度：基础
  - 时间框架：6 小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

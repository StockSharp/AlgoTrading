# Volatility Skew Arbitrage 策略
[English](README.md) | [Русский](README_ru.md)

该期权策略观察两种执行价期权的隐含波动率差异。当波动率偏斜显著偏离历史均值时，预期其回归，并据此建仓。

当偏斜度高于平均值 `Threshold` 个标准差时，买入低波动率期权并卖出高波动率期权形成多头价差；当偏斜度低于平均值同样幅度时则进行空头操作。偏斜度回到均值附近便平仓。

该策略适合熟悉期权定价的资深交易者，并使用止损防止波动率预期持续偏离。
## 详细信息
- **入场条件**:
  - **做多**: Volatility skew > average + Threshold * StdDev
  - **做空**: Volatility skew < average - Threshold * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when skew returns toward average
  - **做空**: Exit when skew returns toward average
- **止损**: 是
- **默认值**:
  - `LookbackPeriod` = 20
  - `Threshold` = 2m
  - `StopLossPercent` = 2m
- **筛选条件**:
  - 类别: 套利
  - 方向: 双向
  - 指标: Volatility Skew
  - 止损: 是
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 高

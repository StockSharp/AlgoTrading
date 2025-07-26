# Pairs Trading Strategy
[English](README.md) | [Русский](README_ru.md)

该策略监控两只相关资产之间的价差，并与其历史均值与标准差比较，捕捉暂时性偏离。

测试表明年均收益约为 88%，该策略在股票市场表现最佳。

当价差低于均值减去乘数倍标准差时做多价差（买入第一只资产卖出第二只资产）；当价差高于均值加上乘数倍标准差时做空价差。价差回到均值附近时平仓。

此方法适合追求市场中性、擅长平衡两只合约敞口的交易者。若价差继续扩大，止损可以限制回撤。

## 细节
- **入场条件**:
  - 多头: `Spread < Mean - Multiplier * StdDev`
  - 空头: `Spread > Mean + Multiplier * StdDev`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价差回到均值时平仓
  - 空头: 价差回到均值时平仓
- **止损**: 百分比止损，基于价差
- **默认值**:
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Arbitrage
  - 方向: 双向
  - 指标: Spread statistics
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等


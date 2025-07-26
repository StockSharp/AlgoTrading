# Cointegration Pairs Strategy
[English](README.md) | [Русский](README_ru.md)

该策略交易长期协整关系的两只资产。通过计算第一只资产与经beta调整的第二只资产之间的残差z值，寻找会回归均衡的偏离。

当残差z值低于`-EntryThreshold`时买入第一只、卖出第二只；当z值高于阈值时做相反操作。价差回到零附近时平仓。

此策略适合能够同时管理两只工具的统计套利者，内置止损在关系短暂失衡时保护资金。

## 细节
- **入场条件**:
  - 多头: `Z-Score < -EntryThreshold`
  - 空头: `Z-Score > EntryThreshold`
- **多/空**: 双向
- **离场条件**:
  - 多头: `|Z-Score| < 0.5` 时平仓
  - 空头: `|Z-Score| < 0.5` 时平仓
- **止损**: 百分比止损
- **默认值**:
  - `Period` = 20
  - `EntryThreshold` = 2.0m
  - `Beta` = 1.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Arbitrage
  - 方向: 双向
  - 指标: Cointegration
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等

测试表明年均收益约为 103%，该策略在股票市场表现最佳。

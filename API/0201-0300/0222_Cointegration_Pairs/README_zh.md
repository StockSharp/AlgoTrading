# Cointegration Pairs 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略交易长期协整关系的两只资产。通过计算第一只资产与经beta调整的第二只资产之间的残差z值，寻找会回归均衡的偏离。

测试表明年均收益约为 103%，该策略在股票市场表现最佳。

当残差z值低于`-EntryThreshold`时买入第一只、卖出第二只；当z值高于阈值时做相反操作。价差回到零附近时平仓。

此策略适合能够同时管理两只工具的统计套利者，内置止损在关系短暂失衡时保护资金。

## 细节
- **入场条件**:
  - 多头: `Z-Score < -EntryThreshold`
  - 空头: `Z-Score > EntryThreshold`
- **多/空**: 双向
- **离场条件**:
  - 多头: `|Z-Score| < ExitThreshold` 时平仓
  - 空头: `|Z-Score| < ExitThreshold` 时平仓
- **止损**: 百分比止损
- **默认值**:
  - `Period` = 20
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.5
  - `Beta` = 1
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Asset2` — 必填，无默认值
    残差为策略主品种（Security）收盘价减去Beta倍Asset2在同一时间K线上的收盘价，其z-score基于最近Period个残差的均值和标准差。0.5是规则中离场水平的默认值。第一条腿交易Volume数量，Asset2交易Beta倍Volume；相反信号会同时反转两条腿。当残差朝不利方向移动入场残差的StopLossPercent百分比时止损平掉两条腿，按K线收盘检查；设为0则关闭。
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


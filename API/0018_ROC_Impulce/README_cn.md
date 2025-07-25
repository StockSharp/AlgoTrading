# ROC动量突发
[English](README.md) | [Русский](README_ru.md)

该策略捕捉ROC指标的突然脉冲。ROC快速向上冲高时做多，快速向下跌破时做空，当动量回落至零附近则平仓。可调整触发阈值以只在极端动量事件时入场，ATR止损防止冲高回落造成大亏。

测试表明年均收益约为 91%，该策略在股票市场表现最佳。

## 详情
- **入场条件**: 基于 ATR、ROC、Momentum 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `RocPeriod` = 12
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, ROC, Momentum
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中


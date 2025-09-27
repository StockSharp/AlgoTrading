# 盈利SuperTrend + MA + 随机指标策略
[English](README.md) | [Русский](README_ru.md)

该策略结合SuperTrend趋势判定、EMA交叉和随机指标。

它通过SuperTrend确认趋势，并以EMA交叉和Stochastic水平作为入场信号，包含可选的止盈和止损。

## 详情
- **入场条件**: SuperTrend趋势、EMA交叉、Stochastic阈值。
- **多空方向**: 双向
- **退出条件**: 反向EMA交叉或TP/SL
- **止损**: 是
- **默认值**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: SuperTrend, EMA, Stochastic
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

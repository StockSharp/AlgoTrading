# MACD交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略在指定区间内利用MACD线与信号线的交叉。

当MACD线在上下阈值之间并向上穿越信号线时，策略做多；向下穿越且位于区间内时做空。出现反向交叉则平仓，无止损。

## 详情
- **入场条件**: 区间内的MACD交叉
- **多空方向**: 双向
- **退出条件**: 反向交叉
- **止损**: 无
- **默认值**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `LowerThreshold` = -0.5m
  - `UpperThreshold` = 0.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MACD
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

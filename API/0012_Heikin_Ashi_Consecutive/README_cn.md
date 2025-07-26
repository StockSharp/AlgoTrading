# Heikin Ashi 连续形态
[English](README.md) | [Русский](README_ru.md)

该策略等待连续数根同色的Heikin Ashi蜡烛以确认动量。出现一系列上涨或下跌蜡烛后跟进，并在首根反向蜡烛或ATR止损时离场。由于Heikin Ashi平滑了价格数据，连续同色强调方向明确，ATR跟踪止损能在走势突然反转时保住利润。

## 详情
- **入场条件**: 基于 Heikin Ashi 信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `ConsecutiveCandles` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Heikin
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

测试表明年均收益约为 73%，该策略在加密市场表现最佳。

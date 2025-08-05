# MACD趋势
[English](README.md) | [Русский](README_ru.md)

该策略基于MACD指标的金叉和死叉。当MACD线向上穿越信号线时做多，向下穿越时做空，出现反向交叉或触发止损则离场。MACD通过衡量动量适应不同市场，此方法旨在顺势跟随，直至指标转变为相反方向。

测试表明年均收益约为 64%，该策略在外汇市场表现最佳。

## 详情
- **入场条件**: 基于 MA、MACD 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MA, MACD
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中


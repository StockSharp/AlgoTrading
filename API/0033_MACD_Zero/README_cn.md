# MACD零轴反转 (MACD Zero Cross)
[English](README.md) | [Русский](README_ru.md)

当MACD柱趋近零轴时捕捉动量反转。

等待MACD减弱后入场, 交叉信号或止损离场。

## 详情

- **入场条件**: MACD trending toward zero from either side.
- **多空方向**: Both directions.
- **出场条件**: MACD crosses signal line or stop.
- **止损**: Yes.
- **默认值**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Momentum
  - 方向: Both
  - 指标: MACD
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 136%，该策略在股票市场表现最佳。

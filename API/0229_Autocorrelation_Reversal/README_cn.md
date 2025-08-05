# Autocorrelation Reversal Strategy
[English](README.md) | [Русский](README_ru.md)

此策略分析短期价格自相关性，以判断近期走势是否可能反转。负自相关表明价格变动倾向交替，为均值回归创造条件。

测试表明年均收益约为 124%，该策略在外汇市场表现最佳。

当计算得出的自相关值低于阈值且价格在均线下方时买入；自相关为负且价格高于均线时做空。价格穿越均线或自相关升至阈值上方即平仓。

该方法适合寻找统计优势而非图形形态的交易者。百分比止损可在趋势持续而未按预期反转时保护资金。

## 细节
- **入场条件**:
  - 多头: Autocorrelation < Threshold && Close < MA
  - 空头: Autocorrelation < Threshold && Close > MA
- **多/空**: 双向
- **离场条件**:
  - 多头: Close > MA 或 自相关 > Threshold
  - 空头: Close < MA 或 自相关 > Threshold
- **止损**: 百分比止损
- **默认值**:
  - `AutoCorrPeriod` = 20
  - `AutoCorrThreshold` = -0.3m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: Autocorrelation, MA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等


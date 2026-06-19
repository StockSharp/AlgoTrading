# Price Statistical Z-Score
[English](README.md) | [Русский](README_ru.md)

该策略使用平滑的Z分数交叉与K线动量过滤。

当短期Z分数上穿长期Z分数时买入，跌破时平仓。策略在相同信号之间保持间隔，并避免三根连续看涨K线后的入场。

## 细节

- **入场条件**：短期Z分数高于长期Z分数、无连续3根看涨K线、信号之间有间隔。
- **多空方向**：仅做多。
- **出场条件**：短期Z分数低于长期Z分数、无连续3根看跌K线、信号之间有间隔。
- **止损**：无。
- **默认值**：
  - `ZScoreBasePeriod` = 3
  - `ShortSmoothPeriod` = 3
  - `LongSmoothPeriod` = 5
  - `GapBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别: 趋势
  - 方向: 做多
  - 指标: SMA, StandardDeviation
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

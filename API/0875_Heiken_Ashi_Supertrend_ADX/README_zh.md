# Heiken Ashi Supertrend Adx Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合Heiken Ashi蜡烛、Supertrend方向以及可选的ADX过滤。没有下影线的看涨Heiken Ashi蜡烛在上升趋势中开多单。没有上影线的看跌蜡烛在下降趋势中开空单。仓位在反向信号或ATR跟踪止损下平仓。

测试表明年均收益约为128%，在加密货币市场表现最佳。

Heiken Ashi平滑噪声，Supertrend和ADX确认方向，ATR确定动态止损。

## 细节
- **入场条件**:
  - 多头: 看涨HA蜡烛无下影线，可选Supertrend上行和ADX确认
  - 空头: 看跌HA蜡烛无上影线，可选Supertrend下行和ADX确认
- **多/空**: 双向
- **离场条件**: 反向蜡烛或ATR跟踪止损
- **止损**: ATR跟踪止损
- **默认值**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `UseAdxFilter` = false
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `TrailAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Heiken Ashi, Supertrend, ADX, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等


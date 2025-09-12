# Strategy Sema Sdi Webhook Strategy
[English](README.md) | [Русский](README_ru.md)

基于平滑EMA交叉和方向性指标确认的策略。
当 +DI > -DI 且快速EMA > 慢速EMA时买入；当 -DI > +DI 且快速EMA < 慢速EMA时卖出。

## 细节

- **入场条件**:
  - 多头: `+DI > -DI && FastEMA > SlowEMA`
  - 空头: `+DI < -DI && FastEMA < SlowEMA`
- **多空方向**: 两者
- **出场条件**: 止盈、止损、跟踪止损
- **止损**: TP、SL、Trailing
- **默认值**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: EMA, Directional Index
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

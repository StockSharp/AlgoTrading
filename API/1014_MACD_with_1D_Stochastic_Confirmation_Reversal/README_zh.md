# MACD配合1日随机指标确认的反转策略
[English](README.md) | [Русский](README_ru.md)

当MACD线向上穿越信号线并且日线随机指标K线高于D线且低于80时买入。价格跌破ATR计算的止损或跌破追踪EMA止盈线时平仓。

## 细节

- **入场条件**：
  - 多头：`MACD上穿Signal 且 DailyK > DailyD 且 DailyK < 80`
- **多空方向**：仅做多
- **止损**：ATR止损和EMA追踪止盈
- **默认参数**：
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TrailingEmaLength` = 20
  - `StopLossAtrMultiplier` = 3.25m
  - `TrailingActivationAtrMultiplier` = 4.25m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **过滤器**：
  - 类别：反转
  - 方向：多头
  - 指标：MACD, Stochastic, ATR, EMA
  - 止损：是
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

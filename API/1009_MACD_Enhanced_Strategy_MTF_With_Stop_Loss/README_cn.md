# MACD 增强多周期止损策略
[English](README.md) | [Русский](README_ru.md)

多周期MACD评分结合ATR移动止损线的策略。

## 详情

- **入场条件**：MACD评分转为正或负。
- **多空方向**：双向。
- **出场条件**：反向信号或价格突破止损线。
- **止损**：ATR移动止损。
- **默认参数**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CrossScore` = 10
  - `IndicatorScore` = 8
  - `HistogramScore` = 2
  - `StopLossFactor` = 1.2
  - `StopLossPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：MACD, ATR
  - 止损：是
  - 复杂度：中等
  - 周期：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

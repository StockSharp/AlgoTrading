# Graph Style 4th Dimension RSI
[English](README.md) | [Русский](README_ru.md)

基于价格变动和RSI水平的策略。

测试显示年化收益约80%，在波动性市场表现良好。

策略结合最近的价格变动方向和RSI极值。当RSI离开超买/超卖区并得到价格变化确认时开仓；当RSI回到中间区域或出现反向信号时平仓。

## 细节

- **入场条件**：价格变动方向配合RSI极值。
- **多空方向**：双向。
- **出场条件**：反向信号或RSI回到中位。
- **止损**：百分比止损。
- **默认值**：
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70m
  - `OversoldLevel` = 30m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选器**：
  - 类别：动量
  - 方向：双向
  - 指标：RSI
  - 止损：百分比
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

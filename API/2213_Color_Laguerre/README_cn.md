# Color Laguerre
[English](README.md) | [Русский](README_ru.md)

基于 Color Laguerre 振荡器的趋势策略。

Color Laguerre 使用拉盖尔滤波器平滑价格序列，并通过颜色变化标记趋势方向。当振荡器转为看涨时策略买入，转为看跌时卖出。达到极端水平时，在动能减弱下可强制平仓。

## 详情

- **入场条件**: 振荡器穿越中线。
- **多空方向**: 双向。
- **退出条件**: 反向信号或止损。
- **止损**: 是。
- **默认值**:
  - `Gamma` = 0.7m
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: 振荡器
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (1h)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等


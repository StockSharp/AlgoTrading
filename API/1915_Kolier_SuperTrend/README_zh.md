# Kolier SuperTrend
[English](README.md) | [Русский](README_ru.md)

该策略基于 Kolier SuperTrend 指标，利用 ATR 带识别趋势反转。

指标根据 ATR 绘制动态支撑和阻力线。价格收于下轨上方并且线条翻到价格下方时产生看多信号；价格收于上轨下方并且线条翻到价格上方时产生看空信号。

通过跟随这些自适应轨迹，策略尝试捕捉强劲趋势，同时在动能减弱时保持保护。

## 详情
- **入场条件**: 价格穿越 SuperTrend 线。
- **多空方向**: 双向。
- **退出条件**: 反向穿越。
- **止损**: 无。
- **默认值**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, SuperTrend
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 波段 (4h)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

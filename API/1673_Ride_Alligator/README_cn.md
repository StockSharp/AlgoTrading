# Ride Alligator 策略
[English](README.md) | [Русский](README_ru.md)

Ride Alligator 策略使用三个移动平均线，即 Alligator 指标。当嘴唇线从下向上穿过下颚线且牙齿线位于下颚线下方时开多仓；当嘴唇线从上向下穿过下颚线且牙齿线在下颚线上方时开空仓。开仓后使用下颚线作为跟踪止损。

## 细节

- **入场条件**:
  - 多头: `Lips > Jaws && Teeth < Jaws && previous Lips < previous Jaws`
  - 空头: `Lips < Jaws && Teeth > Jaws && previous Lips > previous Jaws`
- **多头/空头**: 双向
- **出场条件**:
  - 多头: `price <= Jaws`
  - 空头: `price >= Jaws`
- **止损**: 下颚线跟踪止损
- **默认值**:
  - `AlligatorPeriod` = 5
  - `MaType` = MovingAverageTypeEnum.Weighted
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: Alligator
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

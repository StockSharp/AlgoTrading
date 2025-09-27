# Combo 123 Reversal & Fractal Chaos Bands 策略
[English](README.md) | [Русский](README_ru.md)

该策略将基于随机指标的123反转与分形混沌带突破相结合。
当形成看涨123反转并且收盘价突破上分形带时开多仓。
当形成看跌123反转并且收盘价跌破下分形带时开空仓。

## 细节

- **入场条件**:
  - 多头：Reversal123 多头信号且收盘价高于上分形带。
  - 空头：Reversal123 空头信号且收盘价低于下分形带。
- **多空方向**: 双向
- **出场条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `Length` = 15
  - `KSmoothing` = 1
  - `DLength` = 3
  - `Level` = 50m
  - `Pattern` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**:
  - 类别: 形态与突破
  - 方向: 双向
  - 指标: Stochastic Oscillator, Fractal Chaos Bands
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

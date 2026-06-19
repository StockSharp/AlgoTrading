# Fractal RSI 策略
[English](README.md) | [Русский](README_ru.md)

基于 Fractal RSI 指标的自适应策略。
该指标根据价格运动的分形维度调整 RSI 的周期，
使振荡器在趋势市场中更快，在盘整市场中更慢反应。

当指标穿越预设水平时策略开仓，
可选择顺势或逆势交易。

## 细节

- **入场条件**:
  - *趋势模式*:
    - 做多：值下穿 `LowLevel`
    - 做空：值上穿 `HighLevel`
  - *逆势模式*:
    - 做多：值上穿 `HighLevel`
    - 做空：值下穿 `LowLevel`
- **多空**: 双向
- **出场条件**: 反向信号
- **止损**: 可选的固定止损和止盈
- **默认值**:
  - `CandleType` = `TimeSpan.FromHours(4).TimeFrame()`
  - `FractalPeriod` = 30
  - `NormalSpeed` = 30
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `StopLoss` = 1000 点
  - `TakeProfit` = 2000 点
- **过滤器**:
  - 类别: 趋势 / 振荡指标
    - 方向: 双向
  - 指标: Fractal Dimension, RSI
  - 止损: 有
  - 复杂度: 高级指标
  - 时间框架: 4H (可配置)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

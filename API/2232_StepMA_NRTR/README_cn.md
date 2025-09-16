# StepMA NRTR 策略
[English](README.md) | [Русский](README_ru.md)

基于 StepMA NRTR 指标的趋势跟随策略。该指标结合阶梯移动平均和 Nick Rar Trend 反转机制，在趋势改变时产生买卖信号。

## 细节

- **入场条件**：StepMA NRTR 买/卖信号
- **多空方向**：双向
- **出场条件**：相反的 StepMA NRTR 信号
- **止损**：无
- **默认值**：
  - `Length` = 10
  - `Kv` = 1
  - `StepSize` = 0
  - `UseHighLow` = true
  - `CandleType` = 1 小时时间框
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **筛选器**：
  - 类别：Trend
  - 方向：双向
  - 指标：StepMA NRTR
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中等

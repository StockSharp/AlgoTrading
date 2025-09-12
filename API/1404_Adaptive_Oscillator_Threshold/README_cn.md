# Adaptive Oscillator Threshold 策略
[English](README.md) | [Русский](README_ru.md)

Adaptive Oscillator Threshold 使用基于 Bufi 自适应阈值 (BAT) 的动态阈值 RSI。当 RSI 低于固定水平或自适应阈值时买入。

## 细节

- **入场条件**: RSI 跌破固定或自适应阈值
- **多空方向**: 多头
- **出场条件**: 固定条数退出或美元止损
- **止损**: 美元止损
- **默认值**:
  - `UseAdaptiveThreshold` = true
  - `RsiLength` = 2
  - `BuyLevel` = 14
  - `AdaptiveLength` = 8
  - `AdaptiveCoefficient` = 6
  - `ExitBars` = 28
  - `DollarStopLoss` = 1600
- **过滤器**:
  - 分类: 振荡器
  - 方向: 多头
  - 指标: RSI, StandardDeviation, LinearRegression
  - 止损: 美元
  - 复杂度: 基础
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

# Bollinger RSI Countertrend SOL 策略
[English](README.md) | [Русский](README_ru.md)

该策略针对SOL，利用布林带与RSI进行逆势交易。价格向上穿越下轨且RSI较低时做多，价格向下穿越上轨且RSI较高时做空，仅在工作日交易。

## 细节

- **入场条件**：
  - **多头**：价格上穿下轨且 `RSI` < `Long RSI`，并且是工作日。
  - **空头**：价格下穿上轨且 `RSI` > `Short RSI`，并且是工作日。
- **多空方向**：双向。
- **出场条件**：
  - 多头：价格上穿上轨或触发最近低点下方的止损。
  - 空头：价格上穿中轨或达到盈利目标。
- **止损**：多头在最近低点下方设置止损。
- **默认值**：
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `RSI Length` = 14
  - `Long RSI` = 25
  - `Short RSI` = 79
  - `Short Profit %` = 3.5
- **筛选**：
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: 多个
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 有 (工作日)
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

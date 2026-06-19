# Arpeet MACD策略
[English](README.md) | [Русский](README_ru.md)

Arpeet MACD策略在MACD与信号线交叉时交易，并使用零轴过滤。当天MACD线在零轴下方向上穿越信号线时，生成做多信号。当天MACD线在零轴上方向下穿越信号线时，生成做空信号。

## 细节

- **入场条件**：
  - **多头**：MACD上穿信号线且MACD < 0。
  - **空头**：MACD下穿信号线且MACD > 0。
- **止损**：无。
- **默认参数**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
- **过滤器**：
  - 类型：指标
  - 方向：双向
  - 指标：MACD
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

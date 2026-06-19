# Macd Momentum Reversal
[English](README.md) | [Русский](README_ru.md)

该策略利用MACD柱状图检测动量反转。
当阳线实体增大但MACD柱下降时做空。
当阴线实体增大但MACD柱上升时做多。

## 细节

- **入场条件**：较大的蜡烛实体与减弱的MACD动量。
- **多空**：双向。
- **出场条件**：相反信号。
- **止损**：无。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：动量
  - 方向：双向
  - 指标：MACD
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

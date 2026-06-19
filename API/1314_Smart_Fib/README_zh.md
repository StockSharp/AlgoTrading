# Smart Fib 策略
[English](README.md) | [Русский](README_ru.md)

使用简单均线突破作为入场，并使用基于ATR的斐波那契带作为出场的策略。

## 详情

- **入场条件**：收盘价穿越SMA。
- **多空**：双向。
- **出场条件**：价格触及ATR斐波那契带。
- **止损**：否。
- **默认值**：
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SmaLength` = 50
  - `FibSmaLength` = 8
  - `AtrLength` = 6
  - `FirstFactor` = 1.618
  - `SecondFactor` = 2.618
- **筛选**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: SMA, ATR
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

# AutoFib Breakout 策略
[English](README.md) | [Русский](README_ru.md)

该策略根据最近的摆动高点和低点绘制动态斐波那契扩展，当价格在上升趋势（EMA200）中突破1.618水平时做多。风险通过基于ATR的止损和目标进行管理。

## 细节

- **入场条件**：收盘价高于1.618扩展并高于EMA200。
- **多空方向**：仅做多。
- **出场条件**：ATR止损或3×ATR止盈。
- **止损**：是，基于ATR。
- **默认值**：
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `FibLevel` = 1.618
  - `PivotPeriod` = 10
  - `CandleType` = 5 分钟
- **筛选**：
  - 类别：突破
  - 方向：多
  - 指标：EMA、ATR、Highest、Lowest
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中

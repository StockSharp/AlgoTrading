# 多时间框架 MACD
[English](README.md) | [Русский](README_ru.md)

多时间框架 MACD 将当前时间框架和更高时间框架的 MACD 信号结合。只有当两个时间框架在均线交叉或零线穿越上达成一致时才入场。

## 详情
- **数据**：两个时间框架的价格蜡烛。
- **入场条件**：
  - **做多**：基于 `Entry` 参数，默认是两个时间框架同时出现看涨交叉。
  - **做空**：与做多相反。
- **出场条件**：反向信号或移动止损。
- **止损**：可选的移动止损。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = tf(5)
  - `HigherCandleType` = tf(1d)
  - `ShowCurrentTimeframe` = true
  - `ShowHigherTimeframe` = true
  - `Entry` = Crossover
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 2
- **过滤器**：
  - 类别：趋势
  - 方向：多头和空头
  - 指标：MACD
  - 止损：是
  - 复杂度：中等
  - 时间框架：多时间框架 (5m/1d)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

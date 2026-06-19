# Double MACD
[English](README.md) | [Русский](README_ru.md)

Double MACD 使用两组不同速度的 MACD 指标。当两组 MACD 同时指向同一方向时才开仓。

第一组 MACD 反应更快，第二组较慢，用于在交易前确认趋势。

## 细节
- **数据**：价格 K 线。
- **入场条件**：
  - **多头**：两组 MACD 均高于各自的信号线。
  - **空头**：两组 MACD 均低于各自的信号线。
- **出场条件**：反向信号或止损。
- **止损**：可选的止损。
- **默认参数**：
  - `FastLength1` = 12
  - `SlowLength1` = 26
  - `SignalLength1` = 9
  - `MaType1` = Ema
  - `FastLength2` = 24
  - `SlowLength2` = 52
  - `SignalLength2` = 9
  - `MaType2` = Ema
  - `StopLossPercent` = 2
  - `CandleType` = tf(5)
- **过滤器**：
  - 类别：趋势
  - 方向：多 & 空
  - 指标：MACD
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

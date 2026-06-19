# Anomalous Holonomy Field Theory 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 EMA、RSI、MACD、ATR、ROC 和 VWAP 距离生成综合信号。当信号超过设定阈值时做多，当信号低于负阈值时做空。ATR 止损用于保护持仓。

## 详情

- **入场条件**：
  - **多头**：信号 ≥ 阈值。
  - **空头**：信号 ≤ −阈值。
- **多空方向**：双向。
- **出场条件**：ATR 止损。
- **止损**：是，ATR。
- **默认值**：
  - `SignalThreshold` = 2
  - `CandleType` = 5 分钟
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：有
  - 复杂度：高级
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：高

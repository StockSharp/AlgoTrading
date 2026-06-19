# Nifty 50 5mint Strategy
[English](README.md) | [Русский](README_ru.md)

**Nifty 50 5mint Strategy** 是一套用于 Nifty 50 指数的突破策略，结合 DEMA、VWAP 和布林带确认。

## 详情
- **入场条件**：
  - **多头**：收盘价突破前高、位于布林带上轨之上且 DEMA 高于 VWAP。
  - **空头**：收盘价跌破前低、位于布林带下轨之下且 DEMA 低于 VWAP。
- **多空方向**：双向。
- **出场条件**：止损。
- **止损**：是，固定点数。
- **默认参数**：
  - `DemaPeriod = 6`
  - `BollingerLength = 20`
  - `BollingerStdDev = 2`
  - `LookbackPeriod = 5`
  - `StopLossPoints = 25`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤**：
  - 类别：突破
  - 方向：双向
  - 指标：DEMA、VWAP、Bollinger Bands
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

# Bbsr Extreme
[English](README.md) | [Русский](README_ru.md)

**Bbsr Extreme** 策略结合布林带突破和均线趋势过滤。
当价格从下轨反弹且均线上升时做多；当价格从上轨回落且均线下降时做空。
退出基于ATR计算的止损和止盈。

## 细节
- **入场条件**：价格突破布林带并得到趋势确认。
- **多/空**：双向。
- **出场条件**：ATR止损或止盈。
- **止损**：是，基于ATR。
- **默认值**：
  - `BollingerPeriod = 20`
  - `BollingerMultiplier = 2`
  - `MaLength = 7`
  - `AtrLength = 14`
  - `AtrStopMultiplier = 2`
  - `AtrProfitMultiplier = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：Bollinger Bands, EMA, ATR
  - 止损：是
  - 复杂度：基础
  - 周期：盘中 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中

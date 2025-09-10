# Averaging Down 策略
[English](README.md) | [Русский](README_ru.md)

Averaging Down 策略在 RSI 低于设定阈值时继续加仓做多，从而摊低平均持仓价格。当收盘价突破前一根 K 线的最高价时平仓。

## 详情

- **入场条件**：
  - RSI 低于 `RsiBuyThreshold`。
- **多/空**：仅多头。
- **出场条件**：
  - 收盘价高于前一根 K 线的最高价。
- **止损**：无。
- **默认值**：
  - `RsiLength` = 10
  - `RsiBuyThreshold` = 33
- **过滤条件**：
  - 分类：均值回归
  - 方向：多头
  - 指标：RSI
  - 止损：否
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低

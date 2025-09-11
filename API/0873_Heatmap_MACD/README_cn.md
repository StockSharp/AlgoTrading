# 热力图 MACD
[English](README.md) | [Русский](README_ru.md)

Heatmap MACD 策略在五个时间框架的 MACD 直方图一致时进行交易。当所有直方图转为正值时做多，当全部转为负值时做空。如有需要，当任一直方图反向时可平仓。

## 细节
- **数据**：价格 K 线。
- **入场条件**：
  - **多头**：五个时间框架的 MACD 直方图均 > 0，且之前并非全部为正。
  - **空头**：五个时间框架的 MACD 直方图均 < 0，且之前并非全部为负。
- **出场条件**：反向信号或可选的反向平仓。
- **止损**：默认无。
- **默认参数**：
  - `FastLength` = 9
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TimeFrame1` = tf(60)
  - `TimeFrame2` = tf(120)
  - `TimeFrame3` = tf(240)
  - `TimeFrame4` = tf(240)
  - `TimeFrame5` = tf(480)
  - `CloseOnOpposite` = false
- **过滤器**：
  - 类别：趋势
  - 方向：多 & 空
  - 指标：MACD
  - 止损：无
  - 复杂度：基础
  - 时间框架：多时间框架
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

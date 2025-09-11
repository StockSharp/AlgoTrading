# 分层与K-Means聚类策略
[English](README.md) | [Русский](README_ru.md)

该策略在SuperTrend系统中应用波动性聚类。ATR值被分为三个聚类以识别市场状态，SuperTrend方向改变触发进场。可选的均线和ADX过滤器用于确认趋势强度。当多空成交量比接近平衡时，仓位可提前平仓。

## 细节

- **入场条件**：
  - **多头**：SuperTrend转为看涨 && 聚类趋势 > 0 && 过滤器通过。
  - **空头**：SuperTrend转为看跌 && 聚类趋势 < 0 && 过滤器通过。
- **多空**：双向。
- **离场条件**：
  - 成交量平衡或相反信号。
- **止损**：仅基于成交量。
- **默认值**：
  - `ATR Length` = 11。
  - `SuperTrend Factor` = 3。
  - `Training Data Length` = 200。
  - `Moving Average Length` = 50。
  - `Trend Strength Period` = 14。
  - `Trend Strength Threshold` = 20。
  - `Volume Ratio Threshold` = 0.9。
  - `Delay Bars` = 4。
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：是
  - 复杂度：复杂
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

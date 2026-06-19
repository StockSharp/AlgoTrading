# Trend Envelopes Strategy
[English](README.md) | [Русский](README_ru.md)

基于 TrendEnvelopes 指标的趋势策略。该指标结合 EMA 与基于 ATR 的带状结构来识别突破。
价格向上突破上轨并出现买入信号时开多；价格向下突破下轨并出现卖出信号时开空；反向带触发平仓。

## 细节

- **入场条件**：
  - 多头：收盘价高于上轨并产生买入信号
  - 空头：收盘价低于下轨并产生卖出信号
- **多/空**：两者
- **出场条件**：相反的趋势信号
- **止损**：是（止盈和止损）
- **默认值**：
  - `MaPeriod` = 14
  - `Deviation` = 0.2m
  - `AtrPeriod` = 15
  - `AtrSensitivity` = 0.5m
  - `TakeProfit` = 2000 点
  - `StopLoss` = 1000 点
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **过滤器**：
  - 分类：Trend following
  - 方向：双向
  - 指标：EMA, ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：4h
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等


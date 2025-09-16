# RGT RSI 布林带策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 RSI 指标和布林带来寻找均值回归机会。当 RSI 处于超卖区且价格跌破下轨时开多单；当 RSI 处于超买区且价格突破上轨时开空单。入场后先设置初始止损，当达到最低盈利后启动跟踪止损。

跟踪止损在价格朝有利方向移动时以固定距离跟随，从而锁定利润。当价格触及跟踪止损时平仓。

## 细节

- **入场条件**：RSI 低于 `RsiLow` 且价格低于下轨做多；RSI 高于 `RsiHigh` 且价格高于上轨做空。
- **多/空**：双向。
- **出场条件**：触发跟踪止损。
- **止损**：初始止损和跟踪止损。
- **默认值**：
  - `RsiPeriod` = 8
  - `RsiHigh` = 90
  - `RsiLow` = 10
  - `StopLossPips` = 70
  - `TrailingStopPips` = 35
  - `MinProfitPips` = 30
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 分类：均值回归
  - 方向：双向
  - 指标：RSI，布林带
  - 止损：是
  - 复杂度：初级
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

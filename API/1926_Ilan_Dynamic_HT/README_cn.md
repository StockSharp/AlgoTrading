# Ilan Dynamic HT 策略
[English](README.md) | [Русский](README_ru.md)

基于网格的马丁格尔策略，通过 RSI 信号开仓，并利用动态价格区间扩大持仓。每增加一笔交易，仓位按倍数增长，共享同一个止盈和止损。

## 详情

- **入场条件**：
  - 多头：RSI 低于 `RsiMinimum`
  - 空头：RSI 高于 `RsiMaximum`
- **多/空**：多头和空头
- **出场条件**：
  - 达到统一的止盈或止损
- **止损**：
  - `TakeProfit`（点）
  - `StopLoss`（点）
- **默认值**：
  - `LotExponent` = 1.4
  - `MaxTrades` = 10
  - `DynamicPips` = true
  - `DefaultPips` = 120
  - `Depth` = 24
  - `Del` = 3
  - `BaseVolume` = 0.1
  - `RsiPeriod` = 14
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `TakeProfit` = 100
  - `StopLoss` = 500
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**：
  - 分类：网格 / 马丁格尔
  - 方向：多头和空头
  - 指标：RSI, Highest, Lowest
  - 止损：止盈, 止损
  - 复杂度：高级
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：高

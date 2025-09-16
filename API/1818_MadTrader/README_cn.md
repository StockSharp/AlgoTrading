# Mad Trader 策略
[English](README.md) | [Русский](README_ru.md)

Mad Trader 是从原始 MQL 专家“madtrader-8.7”移植而来的趋势跟随策略。它结合 ATR 和 RSI 指标，在波动率较低但开始上升的时段寻找回调。当 ATR 低于设定阈值但仍在上升，并且 RSI 在总体趋势方向上增加时，若蜡烛实体位于设定范围内，策略将按 RSI 指示的方向开仓。仓位通过跟踪止损和篮子收益机制保护，当账户权益达到目标增幅时关闭所有交易。

## 细节

- **入场条件**：
  - ATR 小于 `MaxAtr` 且大于前一值。
  - 蜡烛实体在 `MinCandle` 与 `MaxCandle` 之间。
  - 交易时间位于 `[StartHour, EndHour)`。
  - RSI 趋势高于 50 且当前 RSI 上升但低于 `RsiLowerLevel` → 买入。
  - RSI 趋势低于 50 且当前 RSI 下降但高于 `RsiUpperLevel` → 卖出。
  - 连续交易之间至少间隔 `TradeInterval`。
- **多/空**：双向。
- **出场条件**：
  - 触发跟踪止损。
  - 达到篮子收益目标（`BasketProfit` 或 `BasketProfit * BasketBoost`）。
- **止损**：使用价格点表示的跟踪止损。
- **默认值**：
  - `AtrPeriod` = 14
  - `RsiPeriod` = 14
  - `TrendBars` = 60
  - `MinCandle` = 5
  - `MaxCandle` = 10
  - `MaxAtr` = 10
  - `RsiUpperLevel` = 50
  - `RsiLowerLevel` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `TradeInterval` = 30 分钟
  - `TrailingStop` = 7
  - `BasketProfit` = 1.05
  - `BasketBoost` = 1.1
  - `RefreshHours` = 24
  - `ExponentialGrowth` = 0.01
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：ATR、RSI
  - 止损：跟踪止损
  - 复杂度：中等
  - 周期：短期（5 分钟蜡烛）
  - 风险等级：中等

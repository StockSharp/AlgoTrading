# HTH Trader 对冲策略

## 概述

本策略为 MetaTrader "HTH Trader" 智能交易系统的移植版本。它通过构建由 EURUSD、USDCHF、GBPUSD、AUDUSD 组成的四腿外汇篮子来博取日内均值回归。StockSharp 版本保留了原策略的时间安排、风险控制以及应急加仓逻辑，并改用高级 API 处理多品种交易。

主要特点：

- 每天在服务器时间 00:05–00:12 之间开仓一次，对四个品种同时建立对冲头寸。
- 根据 EURUSD 最近两个日线收盘价的变化方向来决定整个篮子的多空方向。
- 同时管理四个证券：EURUSD（主证券）、USDCHF、GBPUSD、AUDUSD。
- 以“点”为单位跟踪未实现盈亏，并支持整篮的止盈/止损目标。
- 当总体亏损达到阈值时，可对当前盈利的腿执行应急加仓。
- 在 23:00 或达到目标条件时平掉全部仓位。

## 数据要求

- **日内 K 线**：所有四个品种都必须提供 `IntradayCandleType` 指定的日内周期（默认 5 分钟），用于更新时间和最新价格。
- **日线 K 线**：每个品种都需要提供日线数据，以便获取最近两个完整交易日的收盘价。

## 交易流程

1. 每根日内 K 线完成后，策略会计算当前篮子的总浮动盈亏：
   - 如果启用了 `AllowEmergencyTrading`，且总盈亏 ≤ `-EmergencyLossPips`，则对所有盈利的腿执行一次加倍建仓，并在当日内停用该功能。
   - 如果启用了 `UseProfitTarget` 且盈亏 ≥ `ProfitTargetPips`，立即平掉所有仓位。
   - 如果启用了 `UseLossLimit` 且盈亏 ≤ `-LossLimitPips`，立即平掉所有仓位。
2. 当时间到达 23:00 时，无条件平仓。
3. 在没有持仓且时间处于 00:05–00:12 区间内时，比较 EURUSD 最近两个日线收盘价：
   - **上涨**：买入 EURUSD、USDCHF、AUDUSD，卖出 GBPUSD。
   - **下跌**：卖出 EURUSD、USDCHF、AUDUSD，买入 GBPUSD。
   - 若变化为零或缺少数据，则当天不交易。
4. 通过 `ClosePosition` 使用市价单关闭所有头寸。

## 参数说明

| 参数 | 作用 | 默认值 |
| --- | --- | --- |
| `TradeEnabled` | 是否允许提交订单。 | `true` |
| `ShowProfitInfo` | 在持仓期间记录篮子的浮动盈亏（点）。 | `true` |
| `UseProfitTarget` | 是否启用止盈目标。 | `false` |
| `UseLossLimit` | 是否启用止损目标。 | `false` |
| `AllowEmergencyTrading` | 是否允许应急加仓。 | `true` |
| `EmergencyLossPips` | 触发应急加仓的亏损阈值（点）。 | `60` |
| `ProfitTargetPips` | 触发止盈的盈利阈值（点）。 | `80` |
| `LossLimitPips` | 触发止损的亏损阈值（点）。 | `40` |
| `TradingVolume` | 每条腿使用的下单量。 | `0.01` |
| `Symbol2` | 第二个品种（默认 USDCHF）。 | `null` |
| `Symbol3` | 第三个品种（默认 GBPUSD）。 | `null` |
| `Symbol4` | 第四个品种（默认 AUDUSD）。 | `null` |
| `IntradayCandleType` | 日内行情周期。 | 5 分钟 K 线 |

## 使用建议

- 在启动前将 `Strategy.Security` 设为 EURUSD（或其他主导品种），并为 `Symbol2`、`Symbol3`、`Symbol4` 指定相应的证券。
- 确保所有证券都设置了有效的 `PriceStep`，否则无法计算点值，应急逻辑也不会触发。
- 应急加仓只会作用于当前盈利的腿，以避免扩大现有亏损。
- 默认假设市价单在最近一根 K 线收盘价附近成交，如需更精确结果，请连接实时、稳定的日内行情源。
- 由于策略在 K 线结束时做出决策，执行时刻可能与 MetaTrader 的逐笔版本略有差异，但整体决策流程保持一致。

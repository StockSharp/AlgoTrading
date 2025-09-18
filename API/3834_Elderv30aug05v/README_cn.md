# Elderv30aug05v 策略

## 概述
Elderv30aug05v 策略直接移植自同名的 MetaTrader 4 智能交易系统。策略在 1 小时周期上计算两组 MACD 过滤器，在 15 分钟周期上计算两组随机指标，并使用 1 分钟 K 线完成入场确认与仓位管理，从而复刻原始 MQL 程序的逐笔逻辑。系统同时最多只持有一笔仓位，依靠动态追踪止损而不是固定止盈。

## 数据与指标
- **主 MACD**（`13/30/9`，1 小时 K 线）。做多时要求当前值高于上一柱且上一柱仍低于零轴。
- **副 MACD**（`14/56/9`，1 小时 K 线）。做空时要求当前值低于上一柱且上一柱仍高于零轴。
- **快速随机指标**（`%K=2`、`%D=3`、平滑=3，15 分钟 K 线）。只有当 %K 低于 `LongStochasticThreshold`（默认 36）且相对上一柱向上时才允许做多。
- **慢速随机指标**（`%K=1`、`%D=3`、平滑=3，15 分钟 K 线）。只有当 %K 高于 `ShortStochasticThreshold`（默认 66）且相对上一柱向下时才允许做空。
- **1 分钟 K 线**用于突破确认并驱动追踪止损的更新。

所有指标都通过 `SubscribeCandles().Bind()/BindEx()` 处理已完成的 K 线，完全遵守 StockSharp 的高层 API 要求。

## 入场规则
### 多头条件
1. 主 MACD 向上且上一柱位于零轴之下。
2. 快速随机指标的 %K 低于 `LongStochasticThreshold` 并且高于上一柱。
3. 当前 1 分钟 K 线的收盘价高于上一根 1 分钟 K 线的最高价。

### 空头条件
1. 副 MACD 向下且上一柱位于零轴之上。
2. 慢速随机指标的 %K 高于 `ShortStochasticThreshold` 并且低于上一柱。
3. 当前 1 分钟 K 线的收盘价低于上一根 1 分钟 K 线的最低价。

当已经持仓时，新的信号会被忽略，直到仓位被止损或追踪止损平仓。

## 离场规则
- **初始止损**：开仓后保存入场价加/减 `LongStopLoss` 或 `ShortStopLoss` 与品种 `PriceStep` 的乘积。如果证券未提供 `PriceStep`，则使用 0.0001 作为兜底数值。
- **追踪止损**：当价格向有利方向移动至少 `LongTrailingStop` 或 `ShortTrailingStop` 个点（同样乘以 `PriceStep`）时，止损价格跟随收盘价向利润方向移动。多头止损只上移，空头止损只下移。
- 当 K 线区间触碰到保存的止损价格时，立即以市价平仓。

策略不设置固定止盈，完全遵循原始 MQL 实现。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `Volume` | `0.1` | 用于市价单的交易量。 |
| `LongStopLoss` | `17` | 多头止损距离（点）。 |
| `ShortStopLoss` | `46` | 空头止损距离（点）。 |
| `LongTrailingStop` | `18` | 多头追踪止损距离。 |
| `ShortTrailingStop` | `22` | 空头追踪止损距离。 |
| `LongStochasticThreshold` | `36` | 多头允许的快速随机指标 %K 上限。 |
| `ShortStochasticThreshold` | `66` | 空头允许的慢速随机指标 %K 下限。 |
| `BaseCandleType` | `TimeFrame(1m)` | 执行与仓位管理使用的 1 分钟 K 线。 |
| `StochasticCandleType` | `TimeFrame(15m)` | 两个随机指标使用的 15 分钟 K 线。 |
| `MacdCandleType` | `TimeFrame(1h)` | 两个 MACD 使用的 1 小时 K 线。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | `13 / 30 / 9` | 主 MACD 的参数。 |
| `AltMacdFastPeriod` / `AltMacdSlowPeriod` / `AltMacdSignalPeriod` | `14 / 56 / 9` | 副 MACD 的参数。 |
| `StochasticFastKPeriod` / `StochasticFastDPeriod` / `StochasticFastSmooth` | `2 / 3 / 3` | 快速随机指标参数。 |
| `StochasticSlowKPeriod` / `StochasticSlowDPeriod` / `StochasticSlowSmooth` | `1 / 3 / 3` | 慢速随机指标参数。 |

## 其他说明
- 只要品种提供 1 分钟 K 线和有效的 `PriceStep`，策略即可运行。
- 追踪止损在策略内部维护，并不会在交易所注册真实保护单。
- 逻辑完全基于已完成的 K 线，避免重绘问题，保持与原版 MQL 程序一致。

## 原始脚本
- **来源**：`MQL/7674/Elderv30aug05v.mq4`
- **平台**：MetaTrader 4。

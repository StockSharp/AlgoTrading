# RsiBoosterStrategy

## 概述

`RsiBoosterStrategy` 是 MetaTrader 专家顾问 *RSI booster* 的 StockSharp 移植版本。策略同时计算当前 K 线的快速 RSI 与上一根 K 线的延迟 RSI，当两者差值超过设定阈值时开仓，并通过固定止损/止盈、可选的移动止损以及损失反向加仓链来管理仓位。

策略完全基于 StockSharp 高层 API：订阅一组蜡烛序列、使用内置的 `RelativeStrengthIndex` 指标，并通过参数系统让所有设置在 Designer 中都可优化。

## 交易逻辑

1. 每根完成的 K 线都会更新两个 RSI 指标。
   * 快速 RSI 使用 `FirstRsiPeriod` 和 `FirstRsiPrice`，读取最新的价格。
   * 延迟 RSI 使用 `SecondRsiPeriod` 和 `SecondRsiPrice`，但策略仅保存其上一根 K 线的值，用作 1 根 K 线的延迟参考。
2. 当 `快速 RSI - 延迟 RSI` 大于 `Ratio` 时，在没有多头仓位的情况下买入；当该差值小于 `-Ratio` 时，在没有空头仓位的情况下卖出。
3. `OnlyOnePositionPerBar` 保证同一根 K 线的时间戳下每个方向最多只进场一次。
4. 每根 K 线结束后都会检查止损、止盈和移动止损条件，一旦满足立即平仓。
5. 若仓位以负的实际盈亏结束，并启用恢复逻辑，则策略会立即按相同手数开立反向仓位，并通过 `ReturnOrdersMax` 限制连续恢复单的数量。

## 风险控制

* **止损**：通过 `StopLossPips` 以点数形式设置，价格触及后立即平仓。
* **止盈**：通过 `TakeProfitPips` 以点数形式设置。
* **移动止损**：`TrailingStopPips` 大于 0 时启用，盈亏达到阈值后开始移动，`TrailingStepPips` 控制每次上移的最小改进。
* **反向恢复单**：`ReturnOrderEnabled` 为 `true` 时激活，亏损平仓后立即在反方向开仓，并累计恢复次数。

## 参数列表

| 参数 | 说明 |
|------|------|
| `Volume` | 每次市价单的交易量（手数或合约数）。 |
| `Ratio` | 触发开仓所需的最小 RSI 差值。 |
| `StopLossPips` | 止损距离（点）。 |
| `TakeProfitPips` | 止盈距离（点）。 |
| `TrailingStopPips` | 移动止损距离（点）。 |
| `TrailingStepPips` | 移动止损每次调整所需的最小收益改进。 |
| `OnlyOnePositionPerBar` | 限制同一根 K 线只允许一次进场。 |
| `ReturnOrderEnabled` | 是否启用亏损后的反向恢复逻辑。 |
| `ReturnOrdersMax` | 最多连续恢复单数量。 |
| `FirstRsiPeriod` | 快速 RSI 的周期。 |
| `FirstRsiPrice` | 快速 RSI 使用的价格类型，对应 MetaTrader 的价格模式。 |
| `SecondRsiPeriod` | 延迟 RSI 的周期。 |
| `SecondRsiPrice` | 延迟 RSI 使用的价格类型，对应 MetaTrader 的价格模式。 |
| `CandleType` | 用于分析的蜡烛类型。 |

## 注意事项

* 价格点值优先使用品种的 `PriceStep`，若不可用则回退到 `0.0001`。
* 只要出现盈利交易或恢复单数量达到上限，恢复计数会自动清零。
* 策略会在图表区域同时绘制两个 RSI 曲线，并展示成交记录，便于可视化分析。

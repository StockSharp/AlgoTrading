# Trading Criteria 策略

## 概述

Trading Criteria 策略是将 MQL4 顾问 "Trading Criteria" 移植到 StockSharp 平台的版本。策略结合了多个时间框架：入场级别使用快慢线性加权均线（LWMA），趋势级别使用 Momentum 偏差和 MACD，长期方向通过更慢的时间框架（默认每月）上的 MACD 进行确认。风险控制部分提供止损/止盈、跟踪止损以及保本位移动作。

## 入场条件

1. **基础时间框架**：当快速 LWMA 高于慢速 LWMA 时触发多头，反之触发空头。
2. **Momentum 过滤**：在趋势时间框架计算 |Momentum-100|，最近三个值任意一个超过多头或空头阈值时满足该条件。
3. **趋势 MACD 过滤**：检查 MACD 主线和信号线的关系，并要求当前与前一根柱的关系一致，从而避免高频震荡。
4. **长期 MACD 过滤**：在慢速时间框架（可配置）上的 MACD 用于确认主要趋势方向。
5. **仓位控制**：净头寸不超过 `MaxPositions * Volume`。若出现反向信号且当前持有反向仓位，会先通过市价单平掉原有仓位。

## 出场与风险管理

- **止损与止盈**：参数 `StopLossPoints` 和 `TakeProfitPoints` 按合约的最小价格步长转换成绝对价格，并在蜡烛完成时检查。
- **跟踪止损**：通过 `EnableTrailing` 和 `TrailingStopPoints` 开启。当价格朝有利方向运行超过触发距离后，止损会沿着最高价（多头）或最低价（空头）移动。
- **保本位移**：启用 `EnableBreakEven` 后，当价格在盈利方向走出 `BreakEvenTriggerPoints` 的距离时，止损被移至开仓价加上 `BreakEvenOffsetPoints` 的偏移。
- **强制平仓**：若当根蜡烛触及止损或止盈价格，将立即关闭全部头寸。

## 主要参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 生成信号与计算 LWMA 的基础时间框架。 |
| `TrendCandleType` | Momentum 和趋势 MACD 所用的时间框架。 |
| `MonthlyCandleType` | 长周期 MACD 确认所用时间框架。 |
| `FastMaPeriod` / `SlowMaPeriod` | 快速/慢速 LWMA 的周期。 |
| `MomentumPeriod` | 趋势时间框架的 Momentum 回溯长度。 |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Momentum 偏离 100 的最小要求。 |
| `MaxPositions` | 最大可持有的基础手数倍数。 |
| `StopLossPoints` / `TakeProfitPoints` | 止损与止盈的点数距离。 |
| `EnableTrailing` / `TrailingStopPoints` | 启用跟踪止损以及跟踪距离。 |
| `EnableBreakEven` | 是否启用保本位移。 |
| `BreakEvenTriggerPoints` / `BreakEvenOffsetPoints` | 保本触发距离和偏移量。 |

## 使用提示

- 订阅的证券必须提供所有所需时间框架的蜡烛数据。
- 策略会根据 `PriceStep` 自动调整点值，对 3 或 5 位小数的外汇合约尤为重要。
- 跟踪止损和保本位移在蜡烛收盘时执行，如遇跳空可能在下一根蜡烛才完成平仓。
- 默认参数与原始 MQL 设置一致，可通过参数面板进一步优化。

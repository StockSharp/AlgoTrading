# Bleris 策略

## 概述
Bleris 策略通过分析最近价格极值的走势来顺势开仓。
价格被划分为三个长度相等的区间，并比较每个区间的最高价和最低价。

- **指标**：Highest、Lowest
- **参数**：
  - `SignalBarSample` – 每个区间的蜡烛数量。
  - `CounterTrend` – 反向交易信号。
  - `Lots` – 订单手数。
  - `CandleType` – 使用的蜡烛时间框架。
  - `AnotherOrderPips` – 同方向再次开仓的最小点数间隔。

## 工作原理
1. Highest 与 Lowest 指标计算最近 `SignalBarSample` 根蜡烛的极值。
2. 连续下降的高点表示下行趋势；连续上升的低点表示上行趋势。
3. 上行趋势时策略买入，下行趋势时策略卖出；当启用 `CounterTrend` 时方向相反。
4. 如果最近一次开仓价格与当前价格的差距小于 `AnotherOrderPips`，则忽略新的同向订单。

该示例使用 StockSharp 的高级 API，仅用于教学演示。

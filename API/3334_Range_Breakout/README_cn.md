# 区间突破策略

该策略在最近 `RangePeriod` 根K线上统计最高价和最低价。当K线收盘价向上或向下突破该区间，且区间宽度（以点数计）小于 `MaxRangePoints` 时，策略会在突破方向开仓。

## 入场规则
- **多头**：收盘价 >= 观察区间内的最高价，并且区间宽度（点数） <= `MaxRangePoints`，同时当前无持仓。
- **空头**：收盘价 <= 观察区间内的最低价，并且区间宽度（点数） <= `MaxRangePoints`，同时当前无持仓。

## 离场规则
- 开仓后立即设置止损和止盈保护。
- 无额外的离场条件，仓位保持到保护触发为止。

## 参数
- `RangePeriod` – 计算最高/最低价所使用的K线数量。
- `MaxRangePoints` – 允许交易的最大区间宽度（点数）。
- `CandleType` – 用于分析和交易的K线周期。
- `Volume` – 市价单交易量。
- `StopLossPoints` – 止损距离（点数）。
- `TakeProfitPoints` – 止盈距离（点数）。

## 指标
- Highest
- Lowest

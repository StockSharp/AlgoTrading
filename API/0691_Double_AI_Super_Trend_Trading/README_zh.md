# 双重AI超级趋势交易策略

该策略结合两个SuperTrend指标与加权移动平均线来确认趋势方向。当两个SuperTrend都处于多头且价格WMA高于各自的SuperTrend WMA时开多仓；当两个SuperTrend都处于空头且价格WMA低于其SuperTrend WMA时开空仓。仓位由第一个SuperTrend的ATR追踪止损进行管理。

- **多头**：两个SuperTrend均为多头且价格WMA高于SuperTrend WMA。
- **空头**：两个SuperTrend均为空头且价格WMA低于SuperTrend WMA。
- **指标**：SuperTrend、WMA、ATR。
- **止损**：基于第一个SuperTrend的追踪止损。

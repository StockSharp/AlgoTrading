# 多周期AI SuperTrend结合ADX策略

该策略结合两个SuperTrend指标和ADX强度过滤器。通过比较价格WMA与SuperTrend WMA来确认趋势方向。当两个SuperTrend均为多头且ADX显示正向强度时开多；相反条件下开空。第一个SuperTrend的ATR用作追踪止损。

- **多头**：两个SuperTrend多头，价格WMA高于SuperTrend WMA，+DI > -DI且ADX高于阈值。
- **空头**：两个SuperTrend空头，价格WMA低于SuperTrend WMA，-DI > +DI且ADX高于阈值。
- **指标**：SuperTrend、WMA、ATR、ADX。
- **止损**：基于第一个SuperTrend的ATR追踪止损。

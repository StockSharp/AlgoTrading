# Optimized Grid with KNN Strategy

当 T3 快线向上穿越慢线且基于 KNN 的平均价格变化为正时，本策略开多。入场和出场阈值根据平均变化进行调整。当 T3 快线向下穿越慢线且价格超出盈利阈值时，策略平仓。

- **入场条件**: `t3Fast > t3Slow` 且 `averageChange > 0`
- **出场条件**: `t3Fast < t3Slow` 且 `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **指标**: T3

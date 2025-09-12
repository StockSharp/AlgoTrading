# Optimized Grid with KNN Strategy

This strategy opens long positions when the T3 fast line crosses above the T3 slow line and the KNN-based average price change is positive. Entry and exit thresholds are adjusted by the average change. Positions close once the T3 fast line crosses below the slow line and price exceeds the profit threshold.

- **Entry Conditions**: `t3Fast > t3Slow` and `averageChange > 0`
- **Exit Conditions**: `t3Fast < t3Slow` and `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **Indicators**: T3

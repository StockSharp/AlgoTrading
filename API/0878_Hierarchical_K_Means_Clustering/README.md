# Hierarchical + K-Means Clustering Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies volatility clustering to a SuperTrend system. Average True Range (ATR) values are grouped into three clusters to determine market regime, while the SuperTrend direction triggers entries. An optional moving average and ADX filter confirm trend strength. Positions can be closed early when the bull/bear volume ratio moves toward balance.

## Details

- **Entry Criteria**:
  - **Long**: SuperTrend turns bullish && cluster trend > 0 && filters pass.
  - **Short**: SuperTrend turns bearish && cluster trend < 0 && filters pass.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Volume balance or opposite signal.
- **Stops**: Volume-based only.
- **Default Values**:
  - `ATR Length` = 11.
  - `SuperTrend Factor` = 3.
  - `Training Data Length` = 200.
  - `Moving Average Length` = 50.
  - `Trend Strength Period` = 14.
  - `Trend Strength Threshold` = 20.
  - `Volume Ratio Threshold` = 0.9.
  - `Delay Bars` = 4.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Complex
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

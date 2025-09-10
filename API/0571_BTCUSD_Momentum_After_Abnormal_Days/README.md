# BTCUSD Momentum After Abnormal Days Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy measures the day's return as `(close - open) / open` and compares it with a moving average and standard deviation over a configurable period. If the return exceeds the upper threshold, it opens a long position; if it falls below the lower threshold, it opens a short. All positions are closed at the next day's close.

## Details

- **Entry Criteria**:
  - Return > mean + k × std → long.
  - Return < mean - k × std → short.
- **Long/Short**: Both
- **Exit Criteria**:
  - Close all positions at the next day's close.
- **Stops**: None
- **Default Values**:
  - Lookback period = 5
  - Abnormal return threshold (k) = 1.6
  - Capital per trade = 1000
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: SMA, StandardDeviation
  - Stops: None
  - Complexity: Low
  - Timeframe: Long
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

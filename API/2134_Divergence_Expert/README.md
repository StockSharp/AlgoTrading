# Divergence Expert
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading RSI price divergences. Detects bullish divergence when price makes a lower low but RSI forms a higher low, and bearish divergence when price makes a higher high but RSI forms a lower high. Enters long or short positions accordingly and uses a percentage stop loss.

## Details

- **Entry Criteria:**
  - Long: price makes a new low and RSI makes a higher low (bullish divergence)
  - Short: price makes a new high and RSI makes a lower high (bearish divergence)
- **Long/Short:** Both
- **Exit Criteria:**
  - Long: price hits stop loss or bearish divergence appears
  - Short: price hits stop loss or bullish divergence appears
- **Stops:** Percent from entry price
- **Default Values:**
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters:**
  - Category: Divergence
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium

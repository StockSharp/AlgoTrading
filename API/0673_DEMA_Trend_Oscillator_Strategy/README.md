# DEMA Trend Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy normalizes the Double Exponential Moving Average (DEMA) with a moving average and standard deviation. Goes long when the normalized value exceeds the long threshold and price stays above the upper band; goes short when below the short threshold and price stays under the lower band. Uses ATR based trailing stop, band stop-loss and risk-reward take profit.

## Details

- **Entry Criteria**:
  - Long: normalized value > `LongThreshold` and low > upper band
  - Short: normalized value < `ShortThreshold` and high < lower band
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: price hits take profit, band stop-loss or trailing stop
  - Short: price hits take profit, band stop-loss or trailing stop
- **Stops**: Band stop-loss, ATR trailing, risk-reward take profit
- **Default Values**:
  - `DemaPeriod` = 40
  - `BaseLength` = 20
  - `LongThreshold` = 55m
  - `ShortThreshold` = 45m
  - `RiskReward` = 1.5m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: DEMA, SMA, StandardDeviation, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

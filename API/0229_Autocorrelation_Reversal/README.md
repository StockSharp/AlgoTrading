# Autocorrelation Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy analyzes short-term price autocorrelation to gauge whether recent moves are likely to reverse. Negative autocorrelation suggests successive price changes tend to alternate direction, creating mean-reverting conditions.

When the calculated autocorrelation falls below the threshold and price is below a moving average, the system buys in anticipation of a bounce. If autocorrelation is negative and price is above the average, a short position is opened. Exits occur once price crosses the average or autocorrelation rises above the threshold.

The approach is suited for traders looking for statistical edges rather than chart patterns. A percentage stop-loss is applied to protect against sustained trends that violate the expected reversal.

## Details
- **Entry Criteria**:
  - **Long**: Autocorrelation < Threshold && Close < MA
  - **Short**: Autocorrelation < Threshold && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Close > MA or autocorrelation > Threshold
  - **Short**: Exit when Close < MA or autocorrelation > Threshold
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `AutoCorrPeriod` = 20
  - `AutoCorrThreshold` = -0.3m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Autocorrelation, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

# Hurst Exponent Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This approach uses the Hurst exponent to detect when a market is behaving in a mean-reverting manner. Values below 0.5 suggest price tends to return toward its average, creating opportunities to fade extremes.

A long position is opened when the Hurst exponent is below 0.5 and price closes under a moving average. A short position occurs when the Hurst value is below 0.5 and price closes above the average. Positions exit when price returns to the average line or the Hurst exponent rises above the threshold.

The strategy fits traders who favour statistical tendencies over strong trends. A protective stop-loss shields against extended moves that fail to revert.

## Details
- **Entry Criteria**:
  - **Long**: Hurst < 0.5 && Close < MA
  - **Short**: Hurst < 0.5 && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Close >= MA or Hurst > 0.5
  - **Short**: Exit when Close <= MA or Hurst > 0.5
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Hurst Exponent, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

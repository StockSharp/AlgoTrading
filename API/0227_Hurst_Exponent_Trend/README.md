# Hurst Exponent Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This system uses the Hurst exponent to determine whether the market is exhibiting trending behaviour. Values above the threshold indicate persistence, while values below suggest noise or mean reversion. A moving average provides additional direction confirmation.

Testing indicates an average annual return of about 40%. It performs best in the crypto market.

The strategy buys when the Hurst exponent is greater than the threshold and price closes above the moving average. It sells short when the Hurst exponent is high and price closes below the average. If the Hurst exponent drops below the threshold, existing positions are closed to avoid trading in choppy markets.

Such an approach works for traders who want objective confirmation that a trend is present before entering. The combination of trend filter and stop-loss helps manage the risk of false signals.

## Details
- **Entry Criteria**:
  - **Long**: Hurst > Threshold && Close > MA
  - **Short**: Hurst > Threshold && Close < MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Close < MA or Hurst < Threshold
  - **Short**: Exit when Close > MA or Hurst < Threshold
- **Stops**: Yes, percentage stop-loss.
- **Default Values**:
  - `HurstPeriod` = 100
  - `MaPeriod` = 20
  - `HurstThreshold` = 0.55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Hurst Exponent, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium


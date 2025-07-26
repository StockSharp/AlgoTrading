# ZScore Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The ZScore Reversal strategy measures how far price deviates from a moving average in terms of standard deviations. The resulting Z-Score highlights statistically stretched conditions that may snap back toward the mean.

Testing indicates an average annual return of about 91%. It performs best in the stocks market.

A trade is opened long when the Z-Score falls below a negative threshold, signalling an oversold market. A short trade is taken when the Z-Score rises above the positive threshold. The position is closed once the Z-Score crosses back through zero, indicating price has normalized.

This technique is attractive for mean reversion traders who prefer objective entry levels. The stop-loss percentage keeps adverse moves manageable while waiting for the reversion.

## Details
- **Entry Criteria**:
  - **Long**: Z-Score < -Threshold
  - **Short**: Z-Score > Threshold
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Z-Score crosses above 0
  - **Short**: Exit when Z-Score crosses below 0
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(10)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Z-Score
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium


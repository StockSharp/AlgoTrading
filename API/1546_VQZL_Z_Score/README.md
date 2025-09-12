# VQZL Z-Score
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using z-score relative to a smoothed average.

Testing indicates an average annual return of about 42%. It performs best in the stock market.

The strategy calculates a smoothed moving average and standard deviation to compute a z-score. When price deviates beyond a threshold, it enters in the direction of the move.

## Details

- **Entry Criteria**:
  - **Long**: `Z-Score > threshold`.
  - **Short**: `Z-Score < -threshold`.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `PriceSmoothing` = 15
  - `ZLength` = 100
  - `Threshold` = 1.64
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

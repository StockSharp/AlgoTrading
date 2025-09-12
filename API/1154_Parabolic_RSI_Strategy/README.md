# Parabolic RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy applying Parabolic SAR to RSI for trend shifts. The strategy enters when the SAR flips relative to the RSI line and can filter trades by RSI thresholds.

## Details

- **Entry Criteria**:
  - Long: `SAR` flips below RSI and (optional) `RSI ≥ LongRsiMin`
  - Short: `SAR` flips above RSI and (optional) `RSI ≤ ShortRsiMax`
- **Long/Short**: Configurable
- **Exit Criteria**: Opposite SAR flip
- **Stops**: None
- **Default Values**:
  - `RsiLength` = 14
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `LongRsiMin` = 50
  - `ShortRsiMax` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Configurable
  - Indicators: Parabolic SAR, RSI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

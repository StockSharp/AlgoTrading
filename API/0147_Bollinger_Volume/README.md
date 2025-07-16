# Bollinger Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy that uses Bollinger Bands breakouts with volume confirmation.
Enters positions when price breaks above/below Bollinger Bands with increased volume.

Bollinger bands show volatility expansion and volume confirms the breakout. Positions are taken when price closes outside a band with strong activity.

Suited for breakout players expecting continuation. A stop based on ATR keeps losses manageable.

## Details

- **Entry Criteria**:
  - Long: `Close > UpperBand && Volume > AvgVolume * VolumeMultiplier`
  - Short: `Close < LowerBand && Volume > AvgVolume * VolumeMultiplier`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price returns to middle band
- **Stops**: ATR-based using `StopLossAtr`
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

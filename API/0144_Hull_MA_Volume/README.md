# Hull Ma Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy that uses Hull Moving Average for trend direction and volume confirmation for trade entries.

The Hull moving average smooths out noise, and rising volume confirms conviction. Entries occur when price moves with the Hull slope backed by a volume surge.

This method targets traders watching for strong participation on breakouts. ATR-based stops defend against sudden reversals.

## Details

- **Entry Criteria**:
  - Long: `HullMA(t) > HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
  - Short: `HullMA(t) < HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `HullMA(t) < HullMA(t-1)`
  - Short: `HullMA(t) > HullMA(t-1)`
- **Stops**: `StopLossAtr` ATR from entry
- **Default Values**:
  - `HullPeriod` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Hull MA, Moving Average, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

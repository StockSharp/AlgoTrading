# Macd Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy combining MACD (Moving Average Convergence Divergence) with volume confirmation. Enters positions when MACD line crosses the Signal line and confirms with increased volume.

MACD crossovers are filtered by an increase in volume to confirm momentum. Buy signals come on bullish crosses with expanding volume; sells do the opposite.

Momentum traders watching for volume spikes may find it valuable. Risk is limited using an ATR stop.

## Details

- **Entry Criteria**:
  - Long: `MACD crosses above Signal && Volume > AvgVolume * VolumeMultiplier`
  - Short: `MACD crosses below Signal && Volume > AvgVolume * VolumeMultiplier`
- **Long/Short**: Both
- **Exit Criteria**:
  - MACD cross in opposite direction
- **Stops**: Percent-based at `StopLossPercent`
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: MACD, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

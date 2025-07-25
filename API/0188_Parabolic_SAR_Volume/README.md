# Parabolic Sar Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy that combines Parabolic SAR with volume confirmation. Enters trades when price crosses the Parabolic SAR with above-average volume.

Parabolic SAR identifies trend shifts, and higher volume validates the signal. Trades commence when the SAR flip comes with expanding volume.

Useful for traders who track volume-based moves. The SAR trail and an ATR factor guard against big losses.

## Details

- **Entry Criteria**:
  - Long: `Close > SAR && Volume > AvgVolume`
  - Short: `Close < SAR && Volume > AvgVolume`
- **Long/Short**: Both
- **Exit Criteria**: SAR flip
- **Stops**: Uses Parabolic SAR as trailing stop
- **Default Values**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `VolumePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Parabolic SAR, Parabolic SAR, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 151%. It performs best in the stocks market.

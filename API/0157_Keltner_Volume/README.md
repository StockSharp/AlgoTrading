# Keltner Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Implementation of strategy - Keltner Channels + Volume. Buy when price breaks above upper Keltner Channel with above average volume. Sell when price breaks below lower Keltner Channel with above average volume.

Testing indicates an average annual return of about 58%. It performs best in the stocks market.

Keltner Channel boundaries define potential reversals, and increased volume signals conviction. The system trades when price touches a band with volume expanding.

Traders wanting volume confirmation around volatility bands may prefer this setup. Stops are computed from ATR.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && Volume > AvgVolume`
  - Short: `Close > UpperBand && Volume > AvgVolume`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price crosses EMA
- **Stops**: ATR-based using `StopLoss`
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Keltner Channel, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium


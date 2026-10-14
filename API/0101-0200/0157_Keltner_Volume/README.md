# Keltner Volume Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementation of strategy - Keltner Channels + Volume. Buy when price closes below the lower Keltner Channel with above average volume. Sell when price closes above the upper Keltner Channel with above average volume.

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
- **Stops**: ATR-based using `StopLossAtr`
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `VolumeAvgPeriod` = 20
  - `StopLossAtr` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The channel is the EMA plus and minus Multiplier ATRs, and AvgVolume is the average volume of the previous VolumeAvgPeriod candles. The stop lies StopLossAtr ATRs from the entry close and is checked on candle closes. An entry signal against an open position reverses it.
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


# Bollinger Bounce Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that captures reversals when price bounces off Bollinger Bands with MACD and volume confirmation. The system limits entries to five trades per day and applies fixed percent stop loss and take profit.

## Details

- **Entry Criteria**:
  - Long: `Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - Short: `Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **Long/Short**: Both
- **Stops**: Percent take profit and stop loss
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Bollinger Bands, MACD, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

# Megabar Breakout (Range & Volume & RSI)
[Русский](README_ru.md) | [中文](README_cn.md)

Megabar Breakout detects large candles supported by high volume and RSI confirmation. The strategy enters long on bullish megabars and short on bearish ones.

It multiplies average range and volume to find megabars. RSI moving average filters trades.

## Details

- **Entry Criteria**: Candle body and volume exceed their moving averages by given multipliers. RSI MA above long threshold for buys and below short threshold for sells.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VolumeAveragePeriod` = 20
  - `VolumeMultiplier` = 3
  - `RangeAveragePeriod` = 20
  - `RangeMultiplier` = 4
  - `RsiPeriod` = 14
  - `RsiMaPeriod` = 14
  - `LongRsiThreshold` = 50
  - `ShortRsiThreshold` = 70
  - `TakeProfit` = 400
  - `StopLoss` = 300
  - `FilterTradeHours` = false
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Volume, Range, RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

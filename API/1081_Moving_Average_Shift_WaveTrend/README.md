# Moving Average Shift WaveTrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a configurable moving average with a WaveTrend-style oscillator. Long trades occur when price is above the moving average and the oscillator rises, confirming an uptrend with a long-term EMA and volatility filter. Shorts trigger on the opposite conditions. Positions are protected by percentage stop loss, take profit and trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: price above MA, oscillator > 0 and rising, long-term trend up, ATR above its average, within trading hours, not already in wave.
  - **Short**: price below MA, oscillator < 0 and falling, long-term trend down, ATR above its average, within trading hours, not already in wave.
- **Long/Short**: Both.
- **Exit Criteria**:
  - oscillator reversal with price crossing MA, or trailing stop, or protections.
- **Stops**: Yes.
- **Default Values**:
  - `MaType` = SMA
  - `MaLength` = 40
  - `OscLength` = 15
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1
  - `TrailPercent` = 1
  - `LongMaLength` = 200
  - `AtrLength` = 14
  - `StartHour` = 9
  - `EndHour` = 17
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, Hull MA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Medium-term

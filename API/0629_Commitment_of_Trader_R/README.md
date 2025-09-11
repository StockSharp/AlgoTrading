# Commitment of Trader R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Williams %R indicator to detect overbought and oversold conditions. A simple moving average acts as an optional trend filter.

A long trade is opened when Williams %R rises above the upper threshold and the closing price is above the SMA. A short trade is opened when Williams %R falls below the lower threshold and price is below the SMA. Positions are closed when the oscillator leaves the signal zone.

## Details
- **Entry Criteria**:
  - **Long**: %R > upper threshold and (price > SMA if enabled)
  - **Short**: %R < lower threshold and (price < SMA if enabled)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: %R < upper threshold
  - **Short**: %R > lower threshold
- **Stops**: No
- **Default Values**:
  - `WilliamsPeriod` = 252
  - `UpperThreshold` = -10
  - `LowerThreshold` = -90
  - `SmaEnabled` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Williams %R, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

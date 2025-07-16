# Parabolic SAR Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Parabolic SAR indicator places dots above or below price to signal trend direction. When the dots flip sides, it may mark the end of the previous move. This strategy enters trades on that switch, expecting a short-term reversal.

A running Parabolic SAR value is maintained for each candle. If the indicator moves from above price to below, a long position is opened. If it flips from below to above, a short trade is executed. The method does not use an explicit profit target and typically relies on discretionary exit or trailing stops outside the sample code.

Because SAR reacts quickly, false signals can occur in ranging markets, so it's best used when price makes decisive swings.

## Details

- **Entry Criteria**: Parabolic SAR switches sides relative to price.
- **Long/Short**: Both.
- **Exit Criteria**: Manual or external stop.
- **Stops**: Not defined.
- **Default Values**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

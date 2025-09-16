# Night Scalper Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades around the evening session using Bollinger Bands. It opens positions only after a specified start hour when the band width is narrow and price breaks outside the bands.

## Details

- **Entry Criteria**:
  - **Long**: after `Start Hour`, price closes below the lower Bollinger Band and the band width is less than `Range Threshold`.
  - **Short**: after `Start Hour`, price closes above the upper Bollinger Band and the band width is less than `Range Threshold`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Position is closed if the time falls before `Start Hour` of the next day.
  - Protective stop loss and take profit managed by `StartProtection`.
- **Stops**: Uses `StartProtection` with fixed stop-loss and take-profit offsets.
- **Default Values**:
  - `BB Period` = 40
  - `BB Deviation` = 1
  - `Range Threshold` = 450
  - `Stop Loss` = 370
  - `Take Profit` = 20
  - `Start Hour` = 19
  - `Candle Type` = 1h
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Short-term

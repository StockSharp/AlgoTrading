# GM-8 and ADX Strategy with Second EMA
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters trades when price crosses a GM-8 SMA and aligns with a second EMA while ADX confirms a strong trend.

## Details

- **Entry Criteria**:
  - **Long**: price crosses above the SMA and closes above both the SMA and second EMA with ADX above the threshold.
  - **Short**: price crosses below the SMA and closes below both the SMA and second EMA with ADX above the threshold.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: price crosses below the SMA.
  - **Short**: price crosses above the SMA.
- **Stops**: Uses StartProtection.
- **Default Values**:
  - `GM Period` = 15
  - `Second EMA Period` = 59
  - `ADX Period` = 8
  - `ADX Threshold` = 34
  - `Candle Type` = 15m
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, EMA, ADX
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Short-term


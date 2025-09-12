[Русский](README_ru.md) | [中文](README_cn.md)

Trend Type Indicator detects market regime using ATR and ADX.
It goes long during uptrends, short during downtrends and exits when conditions turn sideways.

## Details

- **Entry Criteria**: +DI greater than -DI and not sideways
- **Long/Short**: Both
- **Exit Criteria**: Opposite trend or sideways
- **Stops**: No
- **Default Values**:
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMaLength` = 20
  - `UseAdx` = true
  - `AdxLength` = 14
  - `AdxLimit` = 25
  - `SmoothFactor` = 3
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, ADX
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

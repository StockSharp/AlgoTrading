# Smoothed Heiken Ashi Strategy Long Only
[Русский](README_ru.md) | [中文](README_cn.md)

Long-only strategy using smoothed Heikin-Ashi candles. Buys when the smoothed candle changes from red to green and exits when it turns red.

## Details

- **Entry Criteria**: Smoothed HA changes from red to green
- **Long/Short**: Long only
- **Exit Criteria**: Smoothed HA turns red
- **Stops**: None
- **Default Values**:
  - `EmaLength` = 10
  - `SmoothingLength` = 10

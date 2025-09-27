# Trend Confirmation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining SuperTrend, MACD and VWAP to confirm trends.

## Details
- **Entry Criteria**: SuperTrend direction with MACD confirmation and price relative to VWAP.
- **Long/Short**: Both directions.
- **Exit Criteria**: MACD crossing its signal line against the position.
- **Stops**: None.
- **Default Values**: ATR Length 10, Factor 3, MACD Fast 12, Slow 26, Signal 9.
- **Filters**: SuperTrend and VWAP.

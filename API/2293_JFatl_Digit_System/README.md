# JFATL Digit System
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy built around the Jurik moving average (JFATL) slope. It opens long positions when the moving average turns upward and short positions when it turns downward. The idea imitates the color-coded digital system from the original MQL version.

## Details
- **Entry Criteria**: Slope of Jurik moving average changes sign. Upward slope opens a long position, downward slope opens a short position.
- **Long/Short**: Both directions are traded.
- **Exit Criteria**: Position is reversed on opposite slope or closed by risk management.
- **Stops**: Percentage-based take profit and optional stop loss configured through `StartProtection`.
- **Default Values**: Length = 5, Phase = -100, Timeframe = 4 hours.
- **Filters**: None. The strategy relies solely on JMA slope.

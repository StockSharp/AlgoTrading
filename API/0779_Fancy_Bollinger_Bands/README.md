# Fancy Bollinger Bands
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades Bollinger Band breakouts. It buys when the close price crosses above the upper band and sells when the close price crosses below the lower band.

## Details

- **Entry Conditions**:
  - **Long**: close crosses above upper band
  - **Short**: close crosses below lower band
- **Exit Conditions**: reverse crossover
- **Type**: Breakout
- **Indicators**: Bollinger Bands
- **Timeframe**: 1 minute (default)
- **Stops**: none

# RSI CCI Williams %R
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines RSI, CCI, and Williams %R to capture reversal opportunities. It buys when all three indicators reach oversold levels and sells when they all reach overbought levels. Each trade uses percentage-based take profit and stop loss protection.

## Details

- **Entry Conditions**:
  - **Long**: `RSI < RSI oversold` && `CCI < CCI oversold` && `Williams %R < Williams oversold`
  - **Short**: `RSI > RSI overbought` && `CCI > CCI overbought` && `Williams %R > Williams overbought`
- **Exit Conditions**: Positions exit via take profit or stop loss.
- **Type**: Reversal
- **Indicators**: RSI, CCI, Williams %R
- **Timeframe**: 45 minutes (default)
- **Stops**: Percentage-based take profit and stop loss

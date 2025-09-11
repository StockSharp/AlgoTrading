# Max Profit Min Loss Options Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines fast and slow moving averages with RSI, MACD and a volume filter. It enters long when trend and momentum conditions align and uses a stop loss and trailing profit for exits.

## Details

- **Entry criteria**:
  - **Long**: fast MA > slow MA, MACD crosses above signal, RSI > oversold with rising pattern, volume above average.
  - **Short**: fast MA < slow MA, MACD crosses below signal, RSI < overbought with falling pattern, volume above average.
- **Exit**: opposite signal or stop-loss/trailing profit.
- **Stops**: percent stop loss and trailing profit.
- **Default values**:
  - Fast MA length = 9
  - Slow MA length = 21
  - RSI length = 14
  - Volume SMA length = 20
  - Stop loss = 1%
  - Trailing profit = 4%
- **Indicators**: MA, RSI, MACD, Volume SMA
- **Timeframe**: 5-minute candles by default

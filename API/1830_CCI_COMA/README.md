# CCI COMA
[Русский](README_ru.md) | [中文](README_cn.md)

Uses Commodity Channel Index and multi-timeframe moving averages to follow the prevailing trend.

## Details

- **Data**: Price candles from multiple timeframes.
- **Entry**: Long when CCI is above zero, RSI above 50, candle closes above open, and all monitored timeframes show an uptrend; short when opposite.
- **Exit**: Position closes on the opposite signal.
- **Instruments**: Any instruments.
- **Risk**: No explicit stop loss or take profit.

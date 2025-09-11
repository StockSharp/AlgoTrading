# CCI MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Combines Commodity Channel Index crossovers with a MACD filter and EMA/ATR bands to trade in the trend direction.

## Details

- **Data**: Price candles.
- **Entry**: Long when CCI crosses above zero, MACD above zero, price above EMA125 and EMA750 but below upper ATR band; short when opposite.
- **Exit**: Position closes on opposite signal.
- **Instruments**: Any instruments.
- **Risk**: No stop loss or take profit.

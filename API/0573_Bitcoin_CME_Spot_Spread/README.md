# Bitcoin CME-Spot Spread
[Русский](README_ru.md) | [中文](README_cn.md)

Trades the spread between CME Bitcoin futures and Bitfinex BTCUSD spot using Bollinger Bands.
Long when the spread drops below the lower band, short when it rises above the upper band.
Positions scale out at four take-profit levels and close after a fixed number of bars.

## Details

- **Data**: CME Bitcoin futures and Bitfinex BTCUSD spot.
- **Entry**: Long on oversold spread, short on overbought spread.
- **Exit**: Scale take profits or after hold bars.
- **Instruments**: Bitcoin futures.
- **Risk**: Partial exits and timed close.


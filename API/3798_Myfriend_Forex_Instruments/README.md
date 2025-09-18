# Myfriend Forex Instruments Strategy

The **Myfriend Forex Instruments Strategy** reproduces the 2006 "MyFriend" MetaTrader expert. It trades EUR/USD on 30-minute candles by combining daily pivot levels, Donchian channel expansions and a short-vs-long momentum spread measured from closing prices. The system looks for candles that pierce the daily pivot with a wide real body or for abrupt Donchian width expansions. When one of these impulses aligns with the intraday momentum bias, the strategy opens a single position with pre-defined protective levels.

## Trading logic

1. **Daily pivot map** – The previous day's high, low and close build the classical pivot ladder (`Pivot`, `R1`, `S1`). These levels remain unchanged for the entire trading session and define the expected trading range.
2. **Momentum pulse** – Two simple moving averages on the closing price (3 and 9 periods) form a short/long momentum spread. The spread is multiplied by 1000 to mimic the MetaTrader "MP" calculation and determines whether bullish or bearish pressure dominates.
3. **Breakout filters**
   - *Pivot thrust*: after a candle closes across the pivot with a body larger than 12 points and the next candle closes in the same direction, the strategy flags a potential trade.
   - *Donchian expansion*: when the 16-period Donchian channel widens beyond the `R1 - S1` range and its direction agrees with price action, the signal is also triggered.
4. **Order management** – Only one position is allowed at a time. Long entries use the previous candle low minus a buffer as the stop and a fixed 70-point take profit. Short entries mirror this logic with the previous high plus a buffer.
5. **Exit tactics**
   - *Time-based exit*: between the 3rd and 4th candle after entry, if the last closed bar moves 3 points against the position, the trade is closed early.
   - *Trailing stop*: once open profit exceeds 5 points and the Donchian boundary continues to move in the trade's favor, the stop is trailed along the channel plus/minus a 1-point buffer.
   - *Hard targets*: price touching the calculated stop or take-profit immediately closes the position.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `BaseVolume` | Order volume used for each new trade. | `1` |
| `TakeProfitPoints` | Distance of the take-profit from the entry in MetaTrader points. | `70` |
| `StopLossBufferPoints` | Additional buffer added beyond the previous candle extremum for the protective stop. | `13` |
| `ChannelPeriod` | Donchian channel period used for width expansion tests and trailing. | `16` |
| `UseTrailingStop` | Enables or disables the Donchian-based trailing stop. | `true` |
| `TrailingStartPoints` | Required open profit (points) before the trailing stop can tighten. | `5` |
| `TrailingBufferPoints` | Buffer (points) applied to the Donchian boundary when trailing. | `1` |
| `UseTimeClose` | Enables the 3–4 candle rejection exit. | `true` |
| `CandleType` | Primary candle type (default 30-minute time frame). | `M30` |
| `DailyCandleType` | Daily candle type used to rebuild pivot levels. | `D1` |

## Notes

- The strategy is designed for EUR/USD and 30-minute candles, mirroring the original expert. Different instruments or time frames may require parameter adjustments.
- Point-based parameters rely on the instrument's `PriceStep`. If it is not provided by the market data, the strategy falls back to a unit price increment.
- Only completed candles are processed, matching the MetaTrader behaviour of the source algorithm.

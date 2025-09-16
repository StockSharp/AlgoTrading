# Pipsover

## Overview
Pipsover is a momentum-reversal strategy that reacts to strong extremes of the Chaikin oscillator. The original MetaTrader 5 Expert Advisor opens a new trade when the oscillator prints a pronounced spike while the previous candle retraces to the 20-period simple moving average. The C# port keeps the same idea by rebuilding the Chaikin oscillator with the accumulation/distribution line and two exponential moving averages. Each trade is protected with the same stop-loss and take-profit distances defined in the script so that risk control matches the reference implementation.

## Indicators and Tools
- **Simple Moving Average (SMA 20)** – provides the mean reversion anchor. The strategy requires the previous candle to touch or cross the average before it becomes eligible for a trade.
- **Chaikin Oscillator (EMA 3 – EMA 10 of ADL)** – measures the pressure between price and volume. Extreme negative readings trigger long opportunities and extreme positive values trigger short opportunities.
- **Accumulation/Distribution Line (ADL)** – feeds the Chaikin oscillator. The fast and slow EMAs run on this value stream to mimic the `iChaikin` indicator from MQL5.

## Trading Logic
### Long Entry
1. Wait for a completed candle so that all indicator values are final.
2. Check that the previous candle closed bullish (`Close > Open`).
3. Confirm that the previous low dipped below the SMA20, signalling a pullback.
4. Read the Chaikin oscillator value from the previous bar. It must be lower than `-OpenLevel` to reflect an oversold spike.
5. When all conditions are met and no position is currently open, send a market buy order.

### Short Entry
1. Wait for a completed candle.
2. Check that the previous candle closed bearish (`Close < Open`).
3. Confirm that the previous high exceeded the SMA20.
4. Ensure that the Chaikin oscillator on the previous bar is greater than `OpenLevel`.
5. If there is no active position, place a market sell order.

### Exit Logic
- **Long positions** close when the next candle after the entry shows a bearish structure (close below open), its high remains above the SMA20 and the Chaikin oscillator rises above `CloseLevel`.
- **Short positions** close when the next candle shows a bullish structure, its low moves below the SMA20 and the Chaikin oscillator falls below `-CloseLevel`.
- Protective exits monitor every finished candle. A long closes if price trades at or below the calculated stop-loss or at or above the calculated take-profit. For shorts the comparison is inverted.

## Position Management
- Only one net position is allowed at any time. Pending orders are cancelled before opening a new trade to replicate the single-position behaviour from MQL5.
- Stop-loss and take-profit values are computed from the current security price step. For longs the stop is set `StopLossPoints * PriceStep` below the execution price and the take-profit `TakeProfitPoints * PriceStep` above it. Shorts use symmetrical but inverted distances.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | 0.1 | Order size used for every market order. |
| `MaLength` | 20 | Period of the pullback SMA. |
| `StopLossPoints` | 65 | Stop-loss offset in price steps from the entry. |
| `TakeProfitPoints` | 100 | Take-profit offset in price steps from the entry. |
| `OpenLevel` | 100 | Absolute Chaikin threshold that enables new entries. |
| `CloseLevel` | 125 | Absolute Chaikin threshold that forces position exit. |
| `ChaikinFastLength` | 3 | Fast EMA length of the Chaikin oscillator. |
| `ChaikinSlowLength` | 10 | Slow EMA length of the Chaikin oscillator. |
| `CandleType` | 1 hour | Timeframe used for candle subscription; adjust it to match the trading session of interest. |

## Implementation Notes
- The strategy binds the accumulation/distribution line and SMA to the candle feed through `SubscribeCandles().Bind(...)`, ensuring that indicator values arrive already synchronised with each finished candle.
- Chaikin values are reconstructed manually inside `ProcessCandle` to avoid low-level buffer access forbidden by the conversion guidelines.
- The algorithm stores the latest completed candle, the SMA value and the Chaikin reading to reproduce the `shift=1` logic (`iClose(...,1)`, `iLow(...,1)`, `iChaikin(...,1)`) used in the MQL5 script.
- Protective target levels are tracked inside the strategy class instead of relying on broker-managed stops, so behaviour is consistent across simulations and live trading.

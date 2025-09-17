# Pipsover 8167 Strategy

## Overview
The **Pipsover 8167** strategy is a StockSharp port of the MetaTrader 4 expert advisor `Pipsover.mq4` distributed with build 8167. The expert searches for strong Chaikin oscillator spikes that appear immediately after a pullback to the 20-period simple moving average on the previous candle. When that combination happens, the script opens a position in the direction of the impulse and protects it with fixed stop-loss and take-profit distances (70 and 140 points respectively in the original MQL code). This C# version rebuilds the exact same logic using high-level StockSharp components so that no direct buffer access is required.

The implementation uses the Accumulation/Distribution Line (ADL) indicator and two exponential moving averages to reconstruct the Chaikin oscillator values produced by `iCustom("Chaikin", ...)` in MetaTrader. All trading decisions are delayed until the candle is fully closed, replicating the `OrdersTotal()` and `Close[1]` / `Open[1]` checks from the source script.

## Indicators and Signals
- **Simple Moving Average (SMA 20)** – applied to candle closes. The previous candle must pierce the SMA (low below for longs, high above for shorts) while keeping a body in the direction of the setup.
- **Chaikin Oscillator (EMA 3 – EMA 10 of ADL)** – rebuilt internally from the ADL stream to mirror `iCustom("Chaikin", 0, 0, 1)` readings. Entry and exit thresholds are expressed in absolute oscillator units.
- **Price Action Filter** – the strategy checks the previous candle body direction: bullish bodies enable long trades while bearish bodies enable shorts.

## Trading Rules
### Long Entry
1. Previous candle closes bullish (`Close[1] > Open[1]`).
2. Previous low breaks below the SMA20 value from that candle.
3. Previous Chaikin value is below `-OpenLevel` (default 55).
4. No position is currently open.

### Short Entry
1. Previous candle closes bearish (`Close[1] < Open[1]`).
2. Previous high is above the SMA20 value from that candle.
3. Previous Chaikin value is above `OpenLevel`.
4. No position is currently open.

### Exit Conditions
- **Long positions** close when the next candle satisfies: bearish body, high above SMA20, and Chaikin above `CloseLevel` (default 90).
- **Short positions** close when the next candle has a bullish body, low below SMA20, and Chaikin below `-CloseLevel`.
- Additionally, every trade carries a protective stop at `StopLossPoints` and a take-profit at `TakeProfitPoints`, both expressed in price steps of the selected instrument.

## Risk Management
- Stop-loss distance: `StopLossPoints × PriceStep` (defaults to 70 points).
- Take-profit distance: `TakeProfitPoints × PriceStep` (defaults to 140 points).
- Position size: configurable via `TradeVolume`, mapped directly to the `Volume` property of the StockSharp strategy and used for all market orders.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | 0.1 | Market order volume (lots or contracts, depending on the security). |
| `MaLength` | 20 | Period of the SMA used for the pullback check. |
| `StopLossPoints` | 70 | Stop-loss distance measured in price steps. |
| `TakeProfitPoints` | 140 | Take-profit distance measured in price steps. |
| `OpenLevel` | 55 | Absolute Chaikin oscillator threshold that unlocks new entries. |
| `CloseLevel` | 90 | Absolute Chaikin oscillator threshold that forces exits. |
| `ChaikinFastLength` | 3 | Fast EMA length in the Chaikin reconstruction. |
| `ChaikinSlowLength` | 10 | Slow EMA length in the Chaikin reconstruction. |
| `CandleType` | H1 | Time-frame used to subscribe for candles and calculate indicators. |

## Implementation Notes
- Candles and indicators are connected via `SubscribeCandles().Bind(...)`, so the strategy stays within the high-level API guidelines.
- Chaikin values are computed in memory by feeding ADL readings into two EMA objects, avoiding prohibited calls such as `GetValue()` on indicator buffers.
- Previous candle information is cached inside the strategy state to reproduce the MQL access pattern `Close[1]`, `Low[1]`, `High[1]`, and `iCustom(...,1)`.
- Stop-loss and take-profit levels are tracked manually because the original expert sent plain market orders with static offsets instead of server-side protective orders.

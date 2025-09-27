# Simple FX Crossover Strategy

## Summary
- High-level port of the MetaTrader 4 expert advisor *simplefx2.mq4* (Simple FX 2.0).
- Trades crossovers between a fast and a slow simple moving average on finished candles.
- Keeps only one position open and flips when the dominant trend reverses.

## Trading Logic
1. Build candles using the configurable timeframe parameter.
2. Compute two simple moving averages (fast and slow) on candle close prices.
3. Confirm a bullish trend when both the current and the previous candle show the fast MA above the slow MA. Confirm a bearish trend when both candles show the fast MA below the slow MA.
4. When the confirmed trend differs from the stored trend state, close any opposite position and immediately open a market order in the new direction using the configured volume.
5. Optional stop-loss and take-profit protections expressed in price steps can be enabled. They use StockSharp's built-in protection service to emulate the MT4 risk settings.

The strategy processes only finished candles, never intrabar ticks, to stay close to the original expert advisor behaviour. Logging is provided on each new entry so that every crossover decision can be audited.

## Parameters
| Name | Description | Default | Optimization |
| --- | --- | --- | --- |
| `ShortPeriod` | Length of the fast simple moving average. | 50 | 10 → 150 step 5 |
| `LongPeriod` | Length of the slow simple moving average. | 200 | 50 → 400 step 10 |
| `Volume` | Order volume submitted with each market trade. | 0.1 | 0.1 → 2 step 0.1 |
| `StopLossPoints` | Protective stop distance in instrument price steps (0 disables). | 0 | — |
| `TakeProfitPoints` | Profit target distance in instrument price steps (0 disables). | 0 | — |
| `CandleType` | Candle timeframe used for analysis. | 1 hour | — |

## Notes & Differences from MT4 Version
- The MT4 persistence file (`simplefx.dat`) is not required; the last trend direction is tracked in memory by the strategy state.
- Slippage, order comment, magic number, and arrow colour options from the original expert advisor are not exposed because StockSharp handles routing differently.
- Stop-loss and take-profit distances are interpreted in **price steps** (instrument ticks). Adjust them to match your broker's pip definition.
- Only one position can be open at any time; the strategy relies on `ClosePosition()` before switching direction, ensuring a clean flip between long and short trades.

## Usage
1. Attach the strategy to a security/instrument and set the desired candle timeframe.
2. Configure moving average periods and risk parameters.
3. Start the strategy; it will subscribe to candles, manage the trend state, and submit market orders when a crossover is confirmed on two consecutive candles.

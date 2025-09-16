# Example of MACD Automated Strategy

## Overview

The strategy replicates the "Example of MACD Automated" MetaTrader 4 expert advisor using the StockSharp high-level API. It monitors the MACD main line on two timeframes and opens a single position when both trend filters agree. Protective stop-loss and take-profit distances are applied in price steps, and the position size follows the original AdvancedMM logic that accumulates the volume of recent losing trades.

## Trading Logic

1. **Higher timeframe filter** – a MACD (12, 26, 9) computed on the higher timeframe (default: daily candles) must have a positive main line for long signals or a negative main line for short signals.
2. **Entry timeframe confirmation** – the same MACD settings on the entry timeframe (default: 15-minute candles) must point in the same direction as the higher timeframe filter.
3. **Single position** – the strategy trades one position at a time. New entries are skipped until the existing position is closed by protective levels.
4. **Protective orders** – stop-loss and take-profit levels are measured in multiples of the instrument price step, mirroring the original MT4 `StopLoss` and `TakeProfit` inputs. A value of `0` disables the corresponding protection.
5. **Advanced money management** – the trade volume increases after consecutive losing trades by summing the lot size of the losses, and reverts to the base volume after profitable trades, emulating the `AdvancedMM()` function from the source EA.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `BaseVolume` | Base order volume used by the AdvancedMM logic. | `0.01` |
| `StopLossPoints` | Stop-loss distance expressed in price steps. `0` disables the stop. | `50` |
| `TakeProfitPoints` | Take-profit distance expressed in price steps. `0` disables the target. | `30` |
| `MacdFastLength` | Fast EMA period of the MACD on both timeframes. | `12` |
| `MacdSlowLength` | Slow EMA period of the MACD on both timeframes. | `26` |
| `MacdSignalLength` | Signal line EMA period. | `9` |
| `EntryCandleType` | Timeframe for trade execution. | `15m` candles |
| `FilterCandleType` | Higher timeframe used as trend filter. | `1d` candles |

## Position Management

- Stop-loss and take-profit prices are recalculated on every new position based on the instrument price step.
- When either protective level is touched inside a bar, the strategy assumes the order is filled at that level and records the realized profit or loss.
- After each closed trade the AdvancedMM logic updates the next order size:
  - Less than two historical trades → use the base volume.
  - The most recent trade was a loss → repeat its volume.
  - Consecutive losses before the last win → sum their volumes to recover.
  - Otherwise → revert to the base volume.

## Notes

- The conversion keeps the original behaviour of holding a position until a protective level is hit; there is no exit on MACD crossovers.
- Ensure the instrument has valid `PriceStep` information so that point-based stop and target distances are calculated correctly.
- The strategy relies on completed candles and should be used with historical data or live feeds that provide finished candle updates.

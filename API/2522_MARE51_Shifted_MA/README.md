# MARE5.1 Shifted Moving Average Strategy

## Overview

The **MARE5.1 Shifted Moving Average Strategy** is a direct port of the original MetaTrader 5 expert advisor "MARE5.1" into the StockSharp high-level API. The system monitors one-minute candles (configurable) and compares two simple moving averages (SMA) that share a configurable forward shift. The logic looks for crossover patterns confirmed by historical SMA relationships and the direction of the latest completed candle.

## Trading Logic

- The strategy uses two SMAs: a fast SMA and a slow SMA. Both are shifted forward by the same number of bars, replicating the behaviour of the original expert advisor.
- A **short position** is opened when all of the following are true:
  1. The slow SMA is at least one price step above the fast SMA on the current candle.
  2. Two candles ago the fast SMA was at least one price step above the slow SMA.
  3. Five candles ago the fast SMA was at least one price step above the slow SMA.
  4. The most recent closed candle (previous bar) is bearish.
- A **long position** is opened when the opposite pattern occurs:
  1. The fast SMA is at least one price step above the slow SMA on the current candle.
  2. Two candles ago the slow SMA was at least one price step above the fast SMA.
  3. Five candles ago the slow SMA was at least one price step above the fast SMA.
  4. The most recent closed candle (previous bar) is bullish.
- Only one position can be open at a time. The default order size comes from the `TradeVolume` parameter.
- Trading is allowed only between the configured session hours (inclusive). This window replicates the hour-based filter of the original expert advisor.

## Risk Management

The strategy mirrors the original fixed take-profit and stop-loss distances. They are defined in "pips" (points adjusted for three- and five-digit instruments) and converted into absolute price units when the strategy starts. Protective orders are managed through `StartProtection` with market-order exits.

## Indicators and Data

- **Fast SMA** – length defined by `FastPeriod`.
- **Slow SMA** – length defined by `SlowPeriod`.
- **Data source** – by default one-minute candles, but any candle type supported by StockSharp can be selected via the `CandleType` parameter.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `TradeVolume` | 0.01 | Order volume used for entries. |
| `TakeProfitPips` | 35 | Take-profit distance in adjusted pips. Set to zero to disable. |
| `StopLossPips` | 55 | Stop-loss distance in adjusted pips. Set to zero to disable. |
| `FastPeriod` | 14 | Period of the fast SMA. |
| `SlowPeriod` | 79 | Period of the slow SMA. |
| `MovingAverageShift` | 4 | Forward shift (in bars) applied to both SMAs. |
| `SessionOpenHour` | 2 | Start of the allowed trading window (0–23, inclusive). |
| `SessionCloseHour` | 3 | End of the allowed trading window (0–23, inclusive). Must be greater than `SessionOpenHour`. |
| `CandleType` | 1-minute candles | Candle data type used by the strategy. |

## Notes

- Signals are evaluated on completed candles. Historical SMA values are internally buffered to replicate the index-based comparisons from the original MQL code.
- The price-step value of the active security is used when comparing SMA differences to ensure the required distance equals at least one tick.
- Stop-loss and take-profit levels rely on the security price step. For three- and five-decimal instruments the pip size is automatically expanded tenfold, matching the MetaTrader behaviour.
- No automated position scaling is implemented. The strategy waits for all open positions to be closed before looking for the next entry signal.
- This repository contains only the C# implementation; there is no Python port for this strategy.

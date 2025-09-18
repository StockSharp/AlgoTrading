# OsMA Four Colors Arrow Strategy

## Overview

This strategy recreates the behaviour of the MetaTrader expert advisor "OsMA Four Colors Arrow" inside the StockSharp framework. The original EA reacts to the coloured arrows produced by the accompanying indicator whenever the OsMA (MACD histogram) changes phase. In the StockSharp version the same behaviour is modelled by monitoring zero-crossings of the MACD histogram: a bullish cross (histogram moves from negative to positive) triggers long entries, while a bearish cross triggers short entries. Optional reverse mode turns the logic upside down for hedging or mean-reversion tests.

The template works with finished candles only and can enforce a daily trading session similar to the time filter offered by the MQL version. Built-in money management includes configurable trade volume, a cap on the number of aggregated positions, and automated stop-loss / take-profit / trailing protection expressed in pips.

## Trading Logic

1. Subscribe to the selected timeframe and compute a MACD histogram (OsMA) using configurable fast, slow, and signal EMA lengths.
2. When a candle closes, check the histogram sign:
   - Histogram crossing above zero → bullish arrow → buy signal.
   - Histogram crossing below zero → bearish arrow → sell signal.
3. Apply optional features before sending an order:
   - Direction filter (long-only, short-only, or both).
   - Reverse mode to invert signals.
   - Close existing opposite exposure before opening the new trade.
   - Limit to one active position or accumulate up to the configured maximum exposure.
4. Market orders are sent with the configured lot size. `StartProtection` converts pip inputs into absolute price offsets to run stop-loss, take-profit, and trailing management automatically.
5. Trades are ignored outside of the allowed intraday session when the time filter is enabled.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Timeframe used for calculations and signal generation. |
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | EMA lengths for the MACD histogram (OsMA). |
| `StopLossPips` / `TakeProfitPips` | Risk targets in pips. Set to zero to disable. |
| `TrailingActivatePips` | Profit (in pips) required before the trailing stop can move. |
| `TrailingStopPips` | Trailing distance in pips. Zero disables the trailing module. |
| `TrailingStepPips` | Extra pips that must be gained before tightening the trailing stop again. |
| `MaxPositions` | Maximum aggregated position units (`TradeVolume` multiples). Zero means unlimited. |
| `ReverseSignals` | Invert entry direction (buy ↔ sell). |
| `DirectionMode` | Restrict signals to long-only, short-only, or both. |
| `CloseOppositePositions` | Close any opposite exposure before acting on the new signal. |
| `OnlyOnePosition` | If `true`, prevents adding to an already open position in the same direction. |
| `UseTimeControl` | Enable the intraday trading session filter. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Session boundaries (end can be earlier than start to cover overnight sessions). |
| `TradeVolume` | Order volume in lots. |

## Notes

- Trailing-stop inputs mimic the EA: trailing becomes available only after `TrailingActivatePips` and moves in steps defined by `TrailingStepPips`.
- The strategy requires the security to have a valid `PriceStep` and `Decimals` to convert pips into price offsets. Defaults fall back to one absolute price unit if the instrument does not provide them.
- If `MaxPositions` is greater than one, the strategy can gradually scale in by repeatedly adding `TradeVolume` while respecting the maximum exposure limit.
- When `UseTimeControl` is enabled and the start and end times coincide, trading is disabled to avoid ambiguous sessions.
- The logic acts on closed candles only; there is no intra-bar order submission, matching the behaviour of the MQL template.

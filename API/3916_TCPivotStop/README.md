# TCPivotStop Strategy

## Overview

The **TCPivotStop Strategy** is a direct port of the MetaTrader expert advisor `gpfTCPivotStop`. The logic revolves around
classical floor pivot calculations performed on the previous trading day. At the start of every new daily session the strategy:

1. Aggregates the prior day's high, low, and close to compute the pivot point plus the first three support and resistance tiers.
2. Checks whether the latest completed hourly bar crossed the pivot from above or below.
3. Opens a market order in the direction of the crossover while attaching stop-loss and take-profit levels that mirror the
   original expert's behaviour.

Only one position can be active at a time. Optional session management allows flattening exposure when a new day begins.

## Trading Rules

- **Timeframe** – Designed for 1-hour candles (configurable).
- **Pivot calculation** – Uses the high, low, and close of the previous day to compute `Pivot`, `R1`, `R2`, `R3`, `S1`, `S2`, `S3`.
- **Entry conditions**
  - Enter *short* when the last completed bar closed below the pivot while the preceding bar closed above it.
  - Enter *long* when the last completed bar closed above the pivot while the preceding bar closed below it.
- **Position sizing** – Fixed lot size defined by the `OrderVolume` parameter.
- **Exit conditions**
  - Stop-loss and take-profit prices are mapped to the classic pivot levels.
  - If the `CloseAtSessionEnd` flag is enabled the strategy liquidates open trades before the next session starts.
  - Protective levels are monitored on candle highs/lows and executed with market orders when touched.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `OrderVolume` | Trade size for market entries. | `0.1` |
| `TakeProfitTarget` | Chooses which pivot tier acts as the profit target (`1` = nearest, `3` = farthest). | `1` |
| `CloseAtSessionEnd` | Close any open position once a new daily session begins. | `false` |
| `CandleType` | Timeframe used for all calculations (hourly by default). | `H1` |

## Notes

- The strategy executes orders only once per day when a new pivot set is available, just like the source EA that triggers on the
  first tick of the daily session.
- The MetaTrader version recalculated lot sizes using account margin history. This port keeps position sizing fixed and
  delegates money management to other components if needed.
- Protective orders are emulated by monitoring candle extremes and sending market orders once a threshold is crossed.

## Files

- `CS/TcpPivotStopStrategy.cs` – C# implementation of the trading logic.
- `README.md` – English documentation (this file).
- `README_cn.md` – Simplified Chinese translation.
- `README_ru.md` – Russian translation.

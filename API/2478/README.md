# WOC 0.1.2 Momentum Strategy

## Overview
This strategy is a high-level StockSharp port of the MetaTrader expert advisor "WOC.0.1.2". It listens to Level 1 best bid/ask updates and searches for fast price streaks on the ask side. When the ask price prints a configurable number of consecutive higher or lower ticks within a limited time window, the strategy opens a market position in the breakout direction. Only one position can be open at any moment, which mirrors the single-position behaviour of the original code.

## Data and Execution
- **Market data**: Level 1 best bid and best ask. The algorithm does not require candles or indicators.
- **Execution**: Market orders. Protective exits are emulated inside the strategy by checking bid/ask updates.

## Signal Logic
1. Track the latest ask price and measure how many consecutive new highs (up streak) or new lows (down streak) have been printed.
2. When an up streak or down streak reaches `SequenceLength`, check that the streak duration is less than or equal to `SequenceTimeoutSeconds` seconds.
3. If the down streak is longer than the up streak, send a sell order; otherwise send a buy order. The check reproduces the original MetaTrader logic where the streak with the highest counter defines the direction.
4. Reset all streak counters after each entry attempt to ensure the next signal starts from scratch.

## Position Management
- **Initial stop**: After an entry the strategy immediately records a stop-loss price that is `StopLossTicks` price steps away from the current bid (for longs) or ask (for shorts).
- **Trailing stop**: When price moves in favour of the trade by more than `TrailingStopTicks` price steps, the stop is tightened to `TrailingStopTicks` behind the latest bid/ask, as long as the stop remains at least double the trailing distance away from the current price. This reproduces the two-step trailing condition from the MQL expert.
- **Exit execution**: When the tracked bid/ask crosses the stored stop level the position is closed via a market order. After the exit the internal state is reset to accept new streaks.

## Volume Management
Two position sizing modes are supported:
- **Fixed lot**: Use the `LotSize` parameter as absolute order volume.
- **Auto Lots**: Enable `UseAutoLotSizing` to map the account balance to volume tiers. The balance is taken from `Portfolio.CurrentValue` and falls back to `Portfolio.BeginValue` if the current value is unavailable.

| Balance (greater than) | Volume |
| ---------------------- | ------ |
| 0 (default)            | `LotSize`
| 200                    | 0.04
| 300                    | 0.05
| 400                    | 0.06
| 500                    | 0.07
| 600                    | 0.08
| 700                    | 0.09
| 800                    | 0.10
| 900                    | 0.20
| 1 000                  | 0.30
| 2 000                  | 0.40
| 3 000                  | 0.50
| 4 000                  | 0.60
| 5 000                  | 0.70
| 6 000                  | 0.80
| 7 000                  | 0.90
| 8 000                  | 1.00
| 9 000                  | 2.00
| 10 000                 | 3.00
| 11 000                 | 4.00
| 12 000                 | 5.00
| 13 000                 | 6.00
| 14 000                 | 7.00
| 15 000                 | 8.00
| 20 000                 | 9.00
| 30 000                 | 10.00
| 40 000                 | 11.00
| 50 000                 | 12.00
| 60 000                 | 13.00
| 70 000                 | 14.00
| 80 000                 | 15.00
| 90 000                 | 16.00
| 100 000                | 17.00
| 110 000                | 18.00
| 120 000                | 19.00
| 130 000                | 20.00

## Parameters
- `StopLossTicks` – stop-loss distance measured in price steps.
- `TrailingStopTicks` – trailing distance measured in price steps (can be zero to disable trailing).
- `SequenceLength` – number of consecutive ask moves required before entering a trade.
- `SequenceTimeoutSeconds` – maximum duration of the streak in seconds.
- `LotSize` – fixed order size used when auto-lot sizing is disabled.
- `UseAutoLotSizing` – enables the balance-based volume table shown above.

## Usage Notes
- Works best on fast instruments where the best ask updates frequently; consider testing on tick-level data feeds.
- The strategy requires hedging accounts because it never holds opposite positions simultaneously.
- Ensure that `Security.PriceStep` is configured; otherwise the stop-loss and trailing calculations fall back to a distance of 1 monetary unit per tick.
- Only one open position is supported at a time, mirroring the original MQL behaviour.

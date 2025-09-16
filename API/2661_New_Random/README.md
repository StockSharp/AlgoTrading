# New Random Strategy

## Overview
The **New Random Strategy** emulates the original MetaTrader "New Random" expert by offering three distinct entry selection modes. It opens only a single position at a time and waits until the current position is closed before generating the next order direction. Market entries are triggered on top-of-book updates (Level 1 data) using the best bid/ask prices as execution anchors. The strategy automatically calculates stop-loss and take-profit offsets in pips, adapting to 3 and 5 digit forex quotes the same way as the MQL version.

## Entry Modes
1. **Generator** – the next direction is chosen by a pseudo random generator seeded on strategy start. Each opportunity is an independent coin toss between buying and selling.
2. **Buy-Sell-Buy** – positions alternate strictly between buy and sell. The very first order is a buy, followed by a sell, and so on.
3. **Sell-Buy-Sell** – positions alternate strictly starting from a sell, followed by a buy, and repeating.

## Parameters
- **Random Mode** (`Mode`) – selects one of the three entry mechanisms described above. Defaults to the random generator.
- **Minimal Lot Count** (`MinimalLotCount`) – multiplies the instrument's minimum tradable volume. A value of `1` means the strategy trades exactly `Security.VolumeMin`, while higher values scale the order size by whole multiples.
- **Stop Loss (pips)** (`StopLossPips`) – distance in pips below/above the filled price where the strategy will exit the position. Set to `0` to disable the stop-loss.
- **Take Profit (pips)** (`TakeProfitPips`) – distance in pips where the strategy will realize profits. Set to `0` to disable the take-profit.

## Trading Logic
1. Subscribes to Level 1 data for the configured security and constantly stores the latest bid, ask, and last trade prices.
2. When no position is open and no order is pending, the strategy evaluates the selected mode to determine the next direction.
3. Orders are placed at market using the most recent best bid/ask snapshot. The stop-loss and take-profit targets are calculated immediately from the entry price using the pip distance parameters.
4. Only one position may exist at a time. Subsequent entries are suppressed until the active position is completely closed.

## Position Management
- Long positions exit early when the current price falls to or below the stop-loss, or rises to or above the take-profit.
- Short positions exit when the current price rises to or above the stop-loss, or falls to or below the take-profit.
- Price comparisons always use the freshest Level 1 information: the last trade price if available, otherwise the best bid/ask for the respective side.
- After closing a trade the strategy resets the internal state, optionally alternates the next direction (for sequence modes), and waits for the next quote update before re-entering.

## Notes
- The strategy never pyramids positions and keeps the behavior deterministic for the sequence-based modes.
- Random mode is seeded with the current tick count so each run produces a unique order stream.
- All internal comments and logs are in English to align with the repository guidelines.

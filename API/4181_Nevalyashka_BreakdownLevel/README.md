# Nevalyashka Breakdown Level Strategy

## Overview
The Nevalyashka Breakdown Level strategy is a direct conversion of the MT4 expert advisor *Nevalyashka_BreakdownLevel*. The system builds an opening range between two configurable times and trades breakouts of that range. When a breakout fails and the trade is stopped out, the strategy immediately reverses direction using a martingale multiplier to recover the loss. Profitable trades block any further entries for the rest of the trading day, matching the original EA behaviour.

## Key Concepts
- **Opening range:** Highest high and lowest low printed between `RangeStart` and `RangeEnd` define the breakout channel for the current day.
- **Breakout entries:** A long position is opened when the closing price exceeds the range high; a short position is opened when it falls below the range low.
- **Protective orders:** The stop-loss is always placed at the opposite side of the range. The take-profit is positioned at a distance equal to the range width.
- **Breakeven move:** When enabled, the stop is moved to the entry price once the trade advances halfway towards the target.
- **Martingale recovery:** After a stop-loss the strategy reverses direction, multiplies the order volume by `MartingaleMultiplier` and uses a symmetric target/stop sized to recoup the previous loss.
- **Daily lockout:** Any profitable close (take-profit or manual exit above zero) prevents new trades until the trading day changes.
- **Forced flat:** When `OrdersCloseTime` is later than `RangeEnd`, all open positions are closed at that time and new entries are blocked for the remainder of the day.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `RangeStart` | Start time (inclusive) of the reference range. | `04:00` |
| `RangeEnd` | End time (inclusive) of the reference range. | `09:00` |
| `OrdersCloseTime` | Time of day to forcefully close positions. When this time is later than `RangeEnd` it also blocks new trades afterwards. | `23:30` |
| `OrderVolume` | Volume used for every breakout trade. | `0.1` |
| `MartingaleMultiplier` | Multiplier applied to the next order after a stop-loss to recover the previous loss. | `2` |
| `UseBreakeven` | Enables moving the stop to break-even once the trade has travelled half of the target distance. | `true` |
| `CandleType` | Candle type used to build the range and generate signals. | `1 hour` candles |

## Trading Rules
1. **Range calculation**: For every new trading day the strategy records the highs and lows of finished candles between `RangeStart` and `RangeEnd` (inclusive).
2. **Entry conditions**:
   - Go long when the closing price of the current candle is above the recorded range high.
   - Go short when the closing price of the current candle is below the recorded range low.
   - Entries are skipped if a martingale reversal is pending, a profitable trade already occurred on the same day, or the current time is past `OrdersCloseTime` (when `OrdersCloseTime > RangeEnd`).
3. **Risk management**:
   - Stop-loss is anchored at the opposite side of the opening range.
   - Take-profit is set at the entry price plus/minus the opening range width.
   - When `UseBreakeven` is enabled the stop moves to the entry price after half the target distance has been covered.
4. **Martingale reversal**:
   - If the stop-loss is hit, the position is closed, the volume is multiplied by `MartingaleMultiplier`, and an immediate market order in the opposite direction is sent.
   - The new stop and target are both placed at a distance equal to the loss per lot divided by the multiplier, matching the original EA's recovery logic.
5. **Daily trade lock**:
   - If a trade closes with non-negative profit or the target is hit, no new trades are allowed until the trading date changes.
6. **Forced exit**:
   - When `OrdersCloseTime` is after the range window and the current time reaches this value, all open positions are flattened and the day is locked.

## Notes
- The strategy uses the high-level StockSharp API (`Strategy.SubscribeCandles().Bind(...)`) to stay close to the framework conventions.
- All stateful calculations (range limits, pending martingale orders, breakeven state) are stored inside the strategy class to avoid historical lookups.
- The conversion preserves the original EA's behaviour of counting trading days by calendar date and managing martingale steps immediately after a stop.

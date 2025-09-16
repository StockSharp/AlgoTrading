# Cheduecoglioni Alternating Strategy

## Overview
This strategy is a StockSharp port of the MQL5 expert advisor "cheduecoglioni". It always keeps the trader in the market by alternating between short and long positions. Every entry is protected with fixed take-profit and stop-loss levels that are defined in pips and converted to price offsets according to the instrument precision.

## Trading Rules
- The strategy listens to the configured candle series (1 minute by default) and only reacts once a candle is fully closed. This event replaces the tick-based loop of the original expert advisor.
- When there is no open position and no market order awaiting execution, the strategy sends a market order in the direction stored in the `_nextSide` state. The very first trade after start is a sell, matching the MQL5 implementation.
- As soon as a position becomes active, the algorithm waits for it to close either by the protective orders or manual intervention. Once the position returns to zero, the next direction flips, so the following trade will be in the opposite direction.
- Stop-loss and take-profit distances are applied automatically by `StartProtection`, ensuring that every trade carries the configured risk-reward distances.

## Parameters
- `Trade Volume` – volume used for each market entry. This mirrors the `InpLots` input.
- `Take Profit (pips)` – distance in pips for the take-profit order. The strategy converts it to absolute price distance using the detected pip size.
- `Stop Loss (pips)` – distance in pips for the protective stop loss, converted with the same pip size logic.
- `Candle Type` – timeframe of the candles that drive the decision loop. Any supported `DataType` can be supplied.

## Implementation Details
- The pip size is derived from `Security.PriceStep`. For 3- or 5-digit FX symbols the value is multiplied by 10 to move from fractional pip to standard pip, replicating the MQL adjustment.
- A waiting flag prevents duplicate market orders while a previous order is awaiting execution. If the broker rejects the order, `OnOrderFailed` clears the flag so the next candle can retry.
- `OnPositionChanged` keeps track of the side of the active position and toggles `_nextSide` after each flat state. This mirrors the MQL logic that opened the opposite side after every exit.
- Protective orders are managed by `StartProtection` with market exits, matching the immediate stop-loss and take-profit assignment that the expert advisor performed on order placement.

## Notes
- The Python version is intentionally not created yet.
- The strategy does not modify unit tests.

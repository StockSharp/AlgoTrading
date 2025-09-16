# Frank Ud Hedging Grid Strategy

## Overview
The **Frank Ud Hedging Grid Strategy** is a direct port of the MetaTrader expert advisor "Frank Ud" into the StockSharp high-level API. The bot keeps simultaneous long and short baskets on the same instrument and then performs martingale-style averaging whenever price drifts against the active basket. All signal handling is performed on top of best bid/ask (Level 1) updates, making the strategy suitable for low-latency execution or tick-by-tick backtesting.

## Trading Logic
1. **Initial hedge** – when no positions are open the strategy immediately opens both a buy and a sell market order with the same volume. Each order receives a stop-loss and take-profit expressed in pips.
2. **Stop/take management** – as long as both baskets exist, their protective levels are respected. Whenever price hits a protective level the matching basket is closed.
3. **Single-sided management** – when only buy or only sell positions remain the strategy:
   - Calculates the volume-weighted average entry price of the active basket.
   - Re-assigns the common take-profit to the average price ± configured distance.
   - Removes the stop-loss (the original EA relies purely on the take-profit from this point).
4. **Martingale step** – if price moves against the active basket by more than the configured step, the strategy doubles the multiplier and opens a new market order. The helper method `AdjustVolume` keeps each order aligned with the instrument’s volume step, minimum, and maximum volume.
5. **Cycle reset** – once all baskets are closed the multiplier resets to 1 and a new hedged cycle begins.

## Parameters
- `TakeProfitPips` – distance between the basket average price and the collective take-profit target (default 12 pips).
- `StopLossPips` – protective stop distance used only for the very first hedge orders (default 12 pips).
- `StepPips` – adverse movement required before adding the next martingale order (default 16 pips).
- `AutoLot` – when `true` the strategy uses `LotSize`; otherwise it trades with the instrument minimum volume.
- `LotSize` – custom base lot size used together with the martingale multiplier when `AutoLot` is enabled.

## Implementation Notes
- The conversion uses the high-level `Strategy` API: Level 1 subscriptions drive the logic, and order placement relies on `BuyMarket`/`SellMarket` helpers.
- Position tracking is internal: the strategy stores the entry price and volume of each basket order so that it can reproduce the original MetaTrader averaging rules.
- The multiplier (`_multiplier`) mirrors the EA’s `Coefficient` variable and doubles after each additional order. Once every trade is closed the multiplier resets to `1`.
- `AdjustVolume` emulates the MQL5 `LotCheck` function by clamping requested volumes to the allowed trading step and contract limits.
- The strategy expects a hedging-enabled account, because it keeps long and short baskets simultaneously just like the source EA.

## Files
- `CS/FrankUdStrategy.cs` – main strategy implementation with English inline comments explaining each block.
- `README.md` – this document.
- `README_ru.md` – Russian translation.
- `README_cn.md` – Simplified Chinese translation.

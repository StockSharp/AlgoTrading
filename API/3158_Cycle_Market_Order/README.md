# Cycle Market Order Strategy

Converted from the MetaTrader 4 expert advisor "CycleMarketOrder_V181". The strategy organises a fixed number of slots inside a price ladder and opens market orders when the live bid/ask trades through an individual slot. Each slot carries its own volume, break-even threshold and trailing stop value so the grid can gradually scale into a position while protecting profits that already reached the required distance.

## Trading logic

1. The pip size is derived from the instrument price step and decimal precision (5/3-digit symbols map to 10 points per pip). The `MaxPrice`, `SpanPips` and `MaxCount` parameters are then used to pre-compute the price range handled by each slot.
2. Level-1 market data is consumed to mirror the tick-based behaviour of the original Expert Advisor. Each update refreshes the cached best bid/ask prices.
3. If `UseWeekendMode` is enabled the strategy refuses to trade outside the configured weekend window (Saturday from `WeekendHour`, the whole Sunday and Monday before `WeekstartHour`).
4. For long cycles (`EntryDirection = 1`) the algorithm scans slots from lowest to highest identifier. Whenever the current ask price falls between the slot's `startPrice` and `endPrice`, a market buy order with `OrderVolume` volume is sent. Short cycles (`EntryDirection = -1`) mirror this logic and use the bid price.
5. Slot states track pending entry/exit orders, filled volume and the average entry price. Logging uses `MagicNumberBase + index` to match the MT4 "magic" identifiers.
6. Trailing management is executed on every level-1 update before new entries are evaluated. Once the profit on a long slot exceeds `BreakEvenPips + TrailingStopPips`, the stop is pushed to `Bid - TrailingStopPips`. Short slots use `Ask + TrailingStopPips` and the mirrored break-even condition. When the market price crosses the stored stop the slot is closed with a market order.
7. Because only market orders are used there are no pending orders to cancel. Partial fills adjust the remaining slot volume so the strategy can continue to trail or re-arm the slot once it becomes flat.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `EntryDirection` | Trading direction: `1` buys the ladder, `-1` sells it, `0` disables new entries while keeping trailing active. |
| `MaxPrice` | Upper anchor price used to calculate the slot ranges. |
| `MaxCount` | Total number of active slots inside the grid. |
| `SpanPips` | Distance in pips between consecutive slot boundaries. |
| `OrderVolume` | Volume submitted when a slot triggers. |
| `BreakEvenPips` | Profit distance that must be exceeded before the trailing stop is armed. |
| `TrailingStopPips` | Trailing distance applied once break-even is achieved. |
| `UseWeekendMode` | Enables the weekend trading blackout window. |
| `WeekendHour` | Hour on Saturday (terminal time) when trading is halted. |
| `WeekstartHour` | Hour on Monday when trading resumes. |
| `MagicNumberBase` | Identifier offset used in log messages to match the original magic numbers. |

## Implementation notes

* Slot management keeps track of pending entry and exit orders so that repeated fills do not register duplicate volume.
* The strategy resets its trailing stop whenever a new fill increases the slot's exposure, ensuring that the stop reflects the most recent average entry price.
* Weekend protection simply skips both trailing and entry logic; existing positions remain untouched while the blackout is active.
* Level-1 data is required because the logic compares raw bid/ask prices instead of candle closes, closely replicating the tick-by-tick behaviour of the MT4 version.

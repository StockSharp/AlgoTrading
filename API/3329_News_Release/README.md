# News Release Strategy

This strategy reproduces the core behaviour of the original **NewsReleaseEA** expert advisor by preparing a bracket of pending orders around a scheduled news release and actively managing the resulting position.

## Key ideas

- Five inputs (news time, lead/lag windows, order distances and spacing) define when and where the stop orders are placed.
- A symmetric set of buy stop and sell stop orders is submitted shortly before the configured news time. The first pair is placed `DistancePips` away from the current ask/bid and additional pairs are offset by `StepPips`.
- Pending orders remain active until `PostNewsMinutes` minutes after the event. At the end of the window the strategy cancels every active order and, if requested, closes any open position.
- When an order is filled, the opposite pending orders are cancelled automatically and the open position is managed via stop-loss, take-profit, break-even and trailing rules expressed in pips.
- Break-even protection arms after the price moves `BreakEvenTriggerPips` in favour of the position and then forces an exit if the price returns to the entry price plus `BreakEvenOffsetPips` (longs) or minus that offset (shorts).
- Trailing management keeps track of the best price reached after entry. Once the distance between the current price and the extreme exceeds `TrailingPips`, the position is closed to protect accrued profit.
- The `TradeOnce` flag mirrors the “trade one time per news” behaviour of the MQL program by preventing a second activation after the first trade has completed.

## Parameters

- `NewsTime` – scheduled time of the news release.
- `PreNewsMinutes` – how many minutes before the release pending orders are placed.
- `PostNewsMinutes` – how many minutes after the release pending orders are kept alive before cancellation.
- `OrderPairs` – number of buy stop/sell stop pairs that form the bracket.
- `DistancePips` – distance in pips of the first pair from the current best ask/bid at the moment of placement.
- `StepPips` – additional spacing in pips between consecutive pairs.
- `OrderVolume` – volume submitted with each pending order.
- `TradeOnce` – if enabled, the strategy can trade only once per event window.
- `UseStopLoss` / `StopLossPips` – enable and configure stop-loss distance in pips.
- `UseTakeProfit` / `TakeProfitPips` – enable and configure take-profit distance in pips.
- `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` – configure the break-even module.
- `UseTrailing` / `TrailingPips` – enable trailing exit logic and define the trailing distance in pips.
- `CloseAfterEvent` – close any open position when the post-news window finishes.

## Notes

- The strategy works exclusively with level1 data (`SubscribeLevel1`) so it can react to the latest bid/ask prices without waiting for candles.
- Price distances expressed in pips are converted to absolute prices by using the instrument `PriceStep`. If `PriceStep` is unavailable, a value of 1 is used as a safe fallback.
- Stop-loss, take-profit, break-even and trailing conditions close the position at market by calling `ClosePosition()`. This mirrors the reactive management in the original expert while keeping the implementation compact.
- No Python version is provided, as requested.

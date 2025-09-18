# Straddle Trail v2.40 Strategy

The **Straddle Trail v2.40 Strategy** is a StockSharp port of the MetaTrader 4 expert advisor "Straddle&Trail" (version 2.40). The algorithm prepares a symmetrical pair of stop orders ahead of a high-impact event, automatically manages the triggered position with break-even and trailing-stop logic, and can react to manual trades that already exist on the account.

## Core workflow

1. **Preparation**
   - The strategy subscribes to order book updates to keep track of the best bid/ask and to minute candles (configurable) for scheduling decisions.
   - Pips are calculated from the instrument settings so that all distances defined in pips are properly converted to prices.
2. **Straddle placement**
   - At the configured lead time before the event (`PreEventEntryMinutes`), or immediately if `PlaceStraddleImmediately` is enabled, a buy-stop and a sell-stop order are placed at `DistanceFromPrice` pips above and below the market.
   - Before the event, pending orders can be recentered every minute if `AdjustPendingOrders` is enabled. Adjustments stop `StopAdjustMinutes` before the event.
3. **Order management**
   - Once one side is triggered, the optional removal of the opposite pending order (`RemoveOppositeOrder`) prevents double exposure.
   - `ShutdownNow` together with `ShutdownOption` makes it possible to flatten open positions and/or cancel pending orders on demand.
4. **Position protection**
   - Initial stop-loss and take-profit levels are derived from the pip-based parameters.
   - When the price reaches the break-even trigger, the stop is moved to lock in `BreakevenLockPips` of profit.
   - Trailing starts either immediately or after break-even (depending on `TrailAfterBreakeven`).
   - If `ManageManualTrades` is true, any manual positions detected by the strategy will be protected using the same rules.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `ShutdownNow` | Forces the shutdown logic to execute on the next candle close. |
| `ShutdownOption` | Chooses what to close: everything, only triggered positions, long-only, short-only, all pending orders, only buy stops, or only sell stops. |
| `DistanceFromPrice` | Distance in pips between the current price and the pending stop orders. |
| `StopLossPips` | Initial stop-loss distance in pips. |
| `TakeProfitPips` | Initial take-profit distance in pips. Set to 0 to disable the take-profit level. |
| `TrailPips` | Trailing-stop distance in pips. Set to 0 to disable trailing. |
| `TrailAfterBreakeven` | If true, trailing will only start after the break-even trigger is hit. |
| `BreakevenLockPips` | Profit (in pips) locked in once the break-even trigger fires. |
| `BreakevenTriggerPips` | Profit threshold (in pips) that activates the break-even move. |
| `EventHour` / `EventMinute` | Scheduled news event time (broker time). Set both to 0 to disable the schedule and use the manual/immediate mode. |
| `PreEventEntryMinutes` | Minutes before the event when the straddle is placed. |
| `StopAdjustMinutes` | Minutes before the event when order adjustments stop. Minimum value is 1 minute. |
| `RemoveOppositeOrder` | Removes the opposite pending order after one side of the straddle is filled. |
| `AdjustPendingOrders` | Re-centers the pending orders every minute until the stop-adjust window is reached. |
| `PlaceStraddleImmediately` | Places the straddle as soon as the strategy starts, ignoring the event schedule. |
| `ManageManualTrades` | Extends the break-even and trailing logic to manual positions. |
| `CandleType` | Candle series used for the timing and scheduling logic (default is 1-minute time frame). |

## Usage notes

- Always configure the correct pip size for the instrument through the security settings so that pip-based distances translate to prices accurately.
- The strategy closes positions using market orders when a stop-loss or take-profit condition is met, which mirrors how the original EA performed manual stop adjustments.
- When `PlaceStraddleImmediately` is disabled and the schedule is active, the straddle is placed only once per trading day. Reset the strategy to prepare for another event on the same day.
- The shutdown controls can be used as an emergency brake to quickly flatten exposure and remove pending orders across scenarios.

## Conversion details

- All comments in the code have been translated to English and expanded with additional explanations for clarity.
- High-level StockSharp API methods (`BuyStop`, `SellStop`, `ClosePosition`) are used to keep the implementation close to the framework best practices.
- The algorithm avoids direct indicator lookups and instead relies on the bound candle and order book subscriptions, as required by the project guidelines.

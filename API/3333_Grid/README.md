# Grid
[Русский](README_ru.md) | [中文](README_cn.md)

High-level StockSharp port of the Mission Automate "Grid" expert advisor. The strategy alternates between long and short grid cycles and always keeps a ladder of limit orders in the active direction. Once the aggregate position reaches a common take-profit level the cycle is closed, all pending orders are removed, and the next cycle starts in the opposite direction.

## How it works
1. **Cycle start** – When there are no positions or pending orders, the strategy opens a market position in the direction defined by `FirstTradeSide` using `StartVolume` lots.
2. **Placing the grid** – After every filled order in the active direction the algorithm places a new limit order at a distance of `GridStepPoints` (converted to price by the instrument `PriceStep`). The volume of the next order equals the volume of the latest filled order multiplied by `LotMultiplier`.
3. **Average-based take-profit** – For every filled order the weighted average entry price is recalculated. The take-profit for the whole basket is set to the average price plus/minus `TargetPoints` (also converted via `PriceStep`). Candle highs and lows are used to model the broker-side trigger behaviour.
4. **Cycle completion** – When the take-profit level is reached the strategy closes the entire position with a market order, cancels remaining pending orders, remembers the direction of the finished cycle and flips the direction for the next one.

## Parameters
- `FirstTradeSide` – direction of the first cycle (`Buy` or `Sell`). Every completed cycle automatically flips the direction.
- `StartVolume` – lot size of the initial market order in each cycle.
- `LotMultiplier` – multiplier applied to the most recent filled order volume when preparing the next grid level. Values greater than one create a martingale-like progression.
- `GridStepPoints` – distance between grid levels expressed in points. The strategy multiplies it by `Security.PriceStep` to obtain the absolute price difference.
- `TargetPoints` – take-profit distance from the weighted average entry price, measured in points.
- `CandleType` – candle series used to monitor price extremes for triggering exits.

## Risk management and behaviour
- No explicit stop-loss is used; the grid keeps adding exposure while the market moves against the position.
- Only one pending order is active at a time. When the order is filled the next level is immediately scheduled.
- The cycle cannot start until both the position and the pending queue are empty and the instrument has a valid `PriceStep`.
- The conversion keeps all calculations inside the strategy without touching global collections or indicator buffers, following the project rules.
- Pending orders are cancelled whenever a cycle ends, preventing orphaned limits from previous cycles.

## Notes
- All point-based settings are converted to prices with `Security.PriceStep`. If the step is zero the strategy waits until the instrument provides it.
- The implementation relies purely on the high-level API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`, `BuyLimit`, `SellLimit`) as required.
- A Python version is intentionally not included in this task.

# News Pending Orders Strategy

This strategy places a pair of pending stop orders around the current price and manages them as the market evolves. It is intended for trading during news releases where sharp moves are expected.

## How it works

- When flat, the strategy places:
  - A **buy stop** order at `Ask + Step`.
  - A **sell stop** order at `Bid - Step`.
- Pending orders are repriced every `TimeModify` seconds if the market moved by at least `StepTrail`.
- When an order is executed, the opposite pending order is cancelled.
- A protective stop-loss and optional take-profit are created based on entry price.
- The stop-loss can be moved to break-even after a defined profit and then trailed as price moves further.

The strategy operates on Level1 data and does not rely on any indicators.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Step` | 10 | Distance in ticks for placing pending stop orders. |
| `StopLoss` | 10 | Initial stop-loss in ticks. |
| `TakeProfit` | 50 | Take-profit in ticks (0 disables). |
| `TrailingStop` | 10 | Trailing stop distance in ticks. |
| `TrailingStart` | 0 | Profit in ticks before trailing is activated. |
| `StepTrail` | 2 | Minimum change in stop price (in ticks) to send a new stop order. |
| `BreakEven` | false | Move stop to entry after reaching `MinProfitBreakEven`. |
| `MinProfitBreakEven` | 0 | Profit in ticks required to move stop to break-even. |
| `TimeModify` | 30 | Seconds between pending order repricing attempts. |

## Notes

- Orders are managed using the high-level StockSharp API.
- The strategy cancels protective orders when the position is closed.
- Only the C# version is provided; no Python implementation is included.


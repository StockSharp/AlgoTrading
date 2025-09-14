# Simple News Strategy

This strategy places pending stop orders around a specified news time to capture sharp moves caused by news releases.

## How it works

- Starting five minutes before `NewsTime`, the strategy submits pairs of buy stop and sell stop orders.
- The first pair is placed `Distance` pips away from the current ask and bid prices.
- Additional pairs are offset by `Delta` pips from the previous ones for a total of `Deals` pairs.
- Ten minutes after the news release the strategy cancels any orders that have not been triggered.
- When a position is opened, the strategy monitors stop-loss, take-profit and trailing stop levels. If any level is reached the position is closed.

## Parameters

- `NewsTime` – moment of the news release.
- `Deals` – number of buy/sell stop pairs.
- `Delta` – spacing between orders in pips.
- `Distance` – distance from current price for the first pair in pips.
- `StopLoss` – initial stop-loss in pips.
- `Trail` – trailing stop in pips.
- `TakeProfit` – take-profit in pips.
- `Volume` – order volume.

## Notes

The strategy does not rely on indicators and works purely with level1 data. It is intended for demonstration purposes and may require adjustments for real trading.

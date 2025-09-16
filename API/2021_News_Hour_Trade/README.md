# News Hour Trade Strategy

The **News Hour Trade** strategy places pending buy and sell stop orders around scheduled high impact news events. Orders are offset from the current price by a fixed number of steps and include stop-loss, take-profit and optional trailing stop management.

## Idea

1. At the configured start hour and minute the strategy prepares for an upcoming news release.
2. A buy stop and a sell stop order are placed `PriceGap` steps above and below the current price.
3. When one order triggers, the opposite pending order is cancelled automatically.
4. The open position is protected with fixed stop-loss and take-profit levels. If `TrailStop` is enabled the stop level follows the price when it moves in favor of the position.
5. Only one trade per day is allowed.

## Parameters

- **StartHour / StartMinute** – time to start trading.
- **DelaySeconds** – pause before orders are placed (currently informational).
- **Volume** – order size in lots.
- **StopLoss** – distance to stop-loss in price steps.
- **TakeProfit** – distance to take-profit in steps.
- **PriceGap** – offset from current price for pending orders.
- **Expiration** – pending order lifetime in seconds (0 means no expiration).
- **TrailStop** – enable trailing stop.
- **TrailingStop** – distance from current price for trailing stop.
- **TrailingGap** – minimum gap before updating trailing stop.
- **BuyTrade / SellTrade** – enable buy or sell side orders.
- **CandleType** – timeframe used for time tracking.

## Notes

The strategy is intended for the M5 timeframe but can be applied to any instrument with low spreads. Use with caution around major news events.

# Close At Profit Strategy

This strategy monitors the floating profit or loss of current positions and closes them once predefined thresholds are reached.
It is useful as a risk‑management addon for other trading strategies.

## How it works
- Subscribes to candle data to get the latest closing price.
- Calculates the floating profit based on the average entry price and current price.
- When the profit exceeds `ProfitToClose` or the loss goes below `-LossToClose`, all open positions are closed.
- Optionally cancels all pending orders before closing positions.
- Can close only the strategy's symbol or all symbols traded by the strategy.

## Parameters
- **UseProfitToClose** – enables profit target closing (default `true`).
- **ProfitToClose** – profit in currency units to trigger closing (default `100`).
- **UseLossToClose** – enables loss based closing (default `false`).
- **LossToClose** – loss in currency units to trigger closing (default `100`).
- **AllSymbols** – if `true`, close positions for all symbols traded by the strategy (default `true`).
- **PendingOrders** – if `true`, cancel active orders before closing positions (default `true`).
- **CandleType** – candle series used to track market price (default `1m`).

## Notes
This strategy does not open trades by itself. It should be combined with other strategies that manage entries.

# Close on Profit or Loss in Account Currency

This strategy reproduces the MetaTrader expert advisor *Close_on_PROFIT_or_LOSS_inAccont_Currency*. It continuously monitors the portfolio equity that the strategy is attached to and, once a configured profit target or drawdown floor is reached, it liquidates every open position and cancels all pending orders managed by the strategy. The class relies on StockSharp's high level API: a candle subscription provides the heartbeat, `CancelActiveOrders()` removes working orders, and `ClosePosition()` flattens the exposure through market orders.

## How it works

1. The strategy keeps polling the current equity (`Portfolio.CurrentValue`) whenever a heartbeat candle closes.
2. If the equity is greater than or equal to **Positive Closure**, the strategy sends a full close request.
3. If the equity is less than or equal to **Negative Closure**, the same liquidation routine is executed to cap the losses.
4. During liquidation the strategy cancels every pending order, sends market orders to close all active positions, and finally stops itself (mirroring the `ExpertRemove()` call from the original EA).

> **Important:** set the thresholds in account currency. To emulate the original behaviour, choose a **Positive Closure** value above the current equity and a **Negative Closure** value below it; otherwise the exit will be triggered immediately on start.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `PositiveClosureInAccountCurrency` | Equity level that triggers a full liquidation when exceeded. | `0` |
| `NegativeClosureInAccountCurrency` | Equity floor that forces liquidation when reached. | `0` |
| `CandleType` | Timeframe used for the heartbeat candles that drive the equity checks. Reduce it for faster reactions. | `1 minute` |

## Notes

- `StartProtection()` is activated on start to copy the original safety behaviour.
- The strategy only interacts with positions and orders that it manages; attach it to the portfolio that holds the trades you wish to guard.
- There is no separate spread/slippage input because StockSharp market orders already account for connector-specific execution costs.

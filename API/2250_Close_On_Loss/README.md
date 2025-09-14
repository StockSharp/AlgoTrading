# Close On Loss Strategy

This risk-management helper strategy tracks the account's realized profit and closes all open positions once the accumulated loss exceeds a specified amount. It does not generate new trades on its own; it can be combined with other strategies to enforce a hard loss limit.

## Parameters

- **MaxLoss**: Maximum permitted loss in account currency. When the realized PnL falls below `-MaxLoss`, the strategy sends market orders to close existing long and short positions.
- **CandleType**: Candle type used to schedule periodic PnL checks.


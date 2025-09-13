# TPSL Insert Strategy

This strategy is a StockSharp translation of the MetaTrader 4 script **TPSL-Insert.mq4**. It does not generate entry or exit signals. Its only purpose is to attach take-profit and stop-loss orders to existing positions.

## How It Works

1. At start, the strategy reads `TakeProfitPips` and `StopLossPips` parameters.
2. The values are converted from pips to price using the security's `PriceStep`.
3. `StartProtection` is called to place protective orders.
   - If a position already exists, protective orders are sent immediately.
   - Future positions opened by the strategy will be protected automatically.

This behavior is useful when positions are opened manually or by external systems and you need to insert risk limits quickly.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `TakeProfitPips` | Take-profit distance in pips. | `35` |
| `StopLossPips` | Stop-loss distance in pips. | `100` |

## Notes

- The strategy does not subscribe to market data and contains no trade logic.
- `StartProtection` handles creation and cancellation of protective orders.

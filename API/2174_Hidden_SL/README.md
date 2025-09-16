# Hidden SL Strategy

This strategy monitors open positions and closes them when the profit or loss reaches hidden levels that are not exposed to the broker.

## Logic

- Subscribes to tick trades to receive real-time price updates.
- Calculates the current profit of the position using the entry price and latest trade price.
- If the profit is greater than the `Take Profit` parameter or less than the `Stop Loss` parameter, the position is closed with a market order.
- The strategy does not place protective stop orders, keeping exit levels hidden.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TakeProfit` | Profit target in currency. | `113` |
| `StopLoss` | Loss limit in currency (negative value). | `-100` |

## Usage

1. Start the strategy with an already opened position or combine it with other entry logic.
2. The strategy will manage the position and exit when hidden profit or loss thresholds are met.
3. No take-profit or stop-loss orders are sent to the exchange.

## Notes

- Position commission and swap are not taken into account.
- The strategy uses market orders for exits.

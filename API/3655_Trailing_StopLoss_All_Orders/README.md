# Trailing StopLoss All Orders Strategy

This strategy replicates the behaviour of the MetaTrader script "Trailing_StopLoss_for All_orders_and_symbols" inside StockSharp. It does not open positions on its own. Instead, it follows any existing net position on the configured security and progressively advances a hidden trailing stop once the floating profit exceeds a configurable threshold.

## How it works

1. The strategy subscribes to trade ticks of the selected instrument and waits for an existing long or short position.
2. As soon as the price moves in the profitable direction by at least `Trail Start (pips)`, the trailing logic becomes active.
3. The best price reached after activation is tracked. The protective level is moved to keep a fixed distance of `Trail Distance (pips)` from that best price. The stop never crosses the entry price, preserving at least a break-even exit.
4. When the market prints a trade beyond the trailing level, the strategy closes the remaining volume with a market order.

The algorithm mirrors the original script idea of trailing every order globally, but in StockSharp the trailing is applied to the aggregated position of the strategy.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Trail Start (pips)` | Profit in pips required before the trailing stop activates. Set to `0` to start trailing immediately. | `20` |
| `Trail Distance (pips)` | Distance in pips maintained between the best price and the trailing stop. Must be positive. | `10` |

Both parameters are converted to price units with the instrument `PriceStep`, preserving the pip-based configuration from the MQL script.

## Notes

- The strategy only manages risk for positions opened manually or by other components.
- The trailing stop is virtual: it closes the position with a market order instead of submitting a stop order to the exchange.
- The logic requires real-time trade data. In backtesting, use tick-based data or aggregated trades for accurate behaviour.

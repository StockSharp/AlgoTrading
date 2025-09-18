# Conditional Position Opener Strategy

## Overview
The **Conditional Position Opener Strategy** reproduces the behaviour of the original MetaTrader script *"Open a buy position if there's no open position"*. The logic is intentionally simple: when manual switches enable the long or short side the strategy sends a market order only if there is no open exposure in that direction. This prevents duplicate entries and keeps the position aligned with the selected side.

The StockSharp port keeps the implementation broker-neutral by relying on the framework's high-level candle subscription and built-in protection helper. Stop-loss and take-profit distances are provided in pip units (price steps) so they can be adapted to any instrument.

## Strategy Logic
1. Subscribe to the configured candle series to act as a timing heartbeat.
2. On each finished candle check the current net position.
3. If the long switch is enabled and the position is flat or short, send a buy market order.
4. If the short switch is enabled and the position is flat or long, send a sell market order.
5. Protective orders are managed automatically through `StartProtection`, which converts the pip distances into actual price offsets.

Because StockSharp uses net positions, enabling both sides at the same time will first open the long trade and then, if still flat after fills, the short trade. This mirrors the intent of the MQL code that avoided multiple orders per direction.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `1` | Order size for each market entry. |
| `StopLossPips` | `100` | Stop-loss distance expressed in price steps. Set to zero to disable. |
| `TakeProfitPips` | `200` | Take-profit distance expressed in price steps. Set to zero to disable. |
| `EnableBuy` | `false` | When `true` the strategy may open long positions if no long exposure exists. |
| `EnableSell` | `false` | When `true` the strategy may open short positions if no short exposure exists. |
| `CandleType` | `1 minute timeframe` | Candle series that drives the periodic evaluation. |

## Notes
- Distances are converted to actual price increments using the instrument's `PriceStep`. If the exchange does not report it, the raw pip value is used as an absolute offset.
- `StartProtection` automatically attaches stop-loss and take-profit orders after every fill, so no manual order management is required.
- The strategy focuses on manual-style triggering and is intended as a template for discretionary execution via parameters.

# Volume Calculator Strategy

## Overview
The **Volume Calculator Strategy** reproduces the logic of the original MetaTrader expert advisor that calculates a recommended trade volume based on stop-loss and take-profit levels. When the strategy starts it reads the configured stop prices, evaluates the current market price of the selected security, and derives the risk metrics using the available portfolio capital.

The strategy does not place any orders. Its sole purpose is to provide detailed money management statistics in the log and expose the computed values through read-only properties. This makes it useful for manual traders who want to validate position sizing rules before sending a trade.

## Parameters
- **Stop Loss Price** – absolute price level of the protective stop used for the planned position.
- **Take Profit Price** – absolute price level of the take-profit target.
- **Max Loss %** – maximum share of the portfolio value that can be risked on a single trade. The strategy multiplies this percentage by the portfolio equity to obtain the maximum acceptable loss in currency terms.
- **Is Long Position** – determines whether the planned position is long (`true`) or short (`false`). The direction is required to compute the distance between the current price and the stop/target levels.

All parameters except *Max Loss %* are excluded from optimization to keep them strictly manual inputs, mirroring the behaviour of the original expert.

## Calculation details
1. **Portfolio value** – the strategy fetches `Portfolio.CurrentValue` (falling back to `Portfolio.BeginValue`) to estimate the available capital. If the value is not provided, the calculation stops with a warning.
2. **Price step validation** – the `Security.PriceStep` and `Security.StepPrice` values must be defined because they convert price distances into contract steps and cash amounts. Missing metadata prevents the calculation.
3. **Current price detection** – the strategy searches for the last trade price. When unavailable it approximates the price by averaging the best bid/ask quotes, finally falling back to the last known price.
4. **Distance in steps** – both stop-loss and take-profit distances are measured in price steps. The distances are rounded up (`decimal.Ceiling`) to stay conservative, the same way the MetaTrader script relies on `MathCeil`.
5. **Money at risk** – maximum acceptable loss equals `PortfolioValue * MaxLoss% / 100`.
6. **Suggested volume** – loss per step is `MaxLoss / StopSteps`. Dividing this value by `StepPrice` produces the position volume that keeps the loss under control.
7. **Expected profit** – multiplying the take-profit steps by `StepPrice` and the suggested volume yields the projected cash gain if the target is hit.
8. **Risk-to-reward ratio** – ratio between take-profit and stop-loss step counts, equivalent to the original pip-based calculation.

Each calculated value is stored inside the strategy and printed to the log with informative English messages. If the risk-to-reward ratio is greater or equal to 3 the strategy signals "You can trade"; otherwise it prints a warning that the trade is too risky.

## Usage workflow
1. Attach the strategy to the desired security and portfolio in the StockSharp environment.
2. Configure the stop-loss and take-profit prices that match the planned manual trade.
3. Set the acceptable risk percentage and the intended direction.
4. Start the strategy – the output with all metrics will appear immediately in the log.
5. Review the suggested volume and the risk-to-reward ratio before executing the trade manually.

## Notes
- If any of the required security metadata fields (price step or step price) are missing, request them from the exchange or adjust the security settings manually.
- The calculation is static; it does not refresh automatically after start. Restart the strategy if market conditions or risk parameters change.
- Because the strategy does not send orders it is safe to run in both backtesting and live environments purely for analytical purposes.

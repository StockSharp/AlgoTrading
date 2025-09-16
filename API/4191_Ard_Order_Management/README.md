# ARD Order Management
[Русский](README_ru.md) | [中文](README_cn.md)

## Strategy overview
The **ARD Order Management** strategy ports the MetaTrader 4 expert advisor `ARD_ORDER_MANAGEMENT_.mq4` to StockSharp's high-level strategy framework. The original script exposed a set of manual commands—buy, sell, close, and modify—that could be triggered from external scripts or UI buttons. Every command recalculated the trade volume from available free margin, opened or reversed market positions, and attached protective stop-loss and take-profit levels at fixed point distances.

The StockSharp version keeps the same interaction model. You drive the behaviour through the `Command` parameter; once a non-`None` value is set the strategy performs the requested action on the next Level 1 update and automatically resets the command back to `None`. Protective orders are recreated with every new entry or modify request so that the stop-loss and take-profit always reflect the current parameter values.

## Command lifecycle
1. **Command dispatch** – when `Command` is set to `Buy` or `Sell`, the strategy stores the request and immediately calls `ClosePosition()` to flatten any open exposure. Active protective orders are cancelled before the new trade is considered, mirroring the MQL loop that closed all tickets via `OrderClose`.
2. **Volume calculation** – the volume is recomputed for every command. It uses `Portfolio.CurrentValue` (fallback to `Portfolio.BeginValue`) divided by `LotSizeDivisor` and scaled by `1/1000`, exactly as `AccountFreeMargin()/lotsize/1000` was used in MetaTrader. The result is rounded to `LotDecimals` and constrained by `MinimumVolume`.
3. **Waiting for a flat position** – if a position was open when the command arrived, the new entry is deferred until `Position` drops to zero. The strategy checks this condition on every Level 1 tick to avoid racing the asynchronous execution pipeline.
4. **Market execution** – once flat, the strategy sends `BuyMarket` or `SellMarket`. The last known best bid/ask prices are stored so that protective orders are derived from realistic execution prices.
5. **Protection placement** – stop-loss and take-profit levels are materialised as separate stop and limit orders. For long trades the stop sits at `bid − StopLossPoints * PriceStep` and the target at `ask + TakeProfitPoints * PriceStep`. Short trades invert those calculations. Modify commands reuse the same routine but with `ModifyStopLossPoints` and `ModifyTakeProfitPoints`.
6. **Close command** – setting `Command` to `Close` cancels all protective orders and calls `ClosePosition()`. If the strategy is already flat the command simply logs the fact and does nothing else.

## Money management
- **Margin driven volume** – the code inspects the current portfolio value so the volume shrinks or grows with the available capital. If the divisor parameter accidentally drops to zero the algorithm falls back to the configured `MinimumVolume` and emits a warning.
- **Rounding** – `LotDecimals` defines how many decimal places are retained after rounding. The implementation uses `Math.Round` with `MidpointRounding.AwayFromZero` so that positive and negative adjustments behave like MetaTrader's `NormalizeDouble`.
- **Minimum lot** – after rounding, the size is clamped with `MinimumVolume`, reproducing the original behaviour where values below `lotmax` were promoted to `0.1`.

## Stop-loss and take-profit handling
- Protective orders are always recreated from scratch. Existing stop or take orders are cancelled before new ones are submitted.
- The strategy checks `Security.PriceStep` before calculating absolute prices. If the step is missing or non-positive, protection orders are skipped and a warning is logged.
- Modify commands (`Command = Modify`) rebuild protection using the dedicated modify distances without changing the current position size.

## Data and execution requirements
- Subscribes to Level 1 data via `SubscribeLevel1()` to mirror the MetaTrader quote updates (`Bid`/`Ask`).
- Does not require candles or indicators; all logic runs on tick/quote updates.
- Uses high-level helpers (`BuyMarket`, `SellMarket`, `BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`, `CancelOrder`) to stay within the StockSharp recommended API layer.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `SlippageSteps` | int | 4 | Permitted slippage expressed in price steps. Stored for compatibility; StockSharp market orders execute immediately and do not consume this value. |
| `LotDecimals` | int | 1 | Number of decimal places retained after rounding the calculated volume. |
| `StopLossPoints` | decimal | 50 | Distance (in price points) from entry to the initial stop-loss. |
| `TakeProfitPoints` | decimal | 100 | Distance (in price points) from entry to the initial take-profit. |
| `LotSizeDivisor` | decimal | 5 | Divides the portfolio value before scaling to lots (`freeMargin / divisor / 1000`). |
| `ModifyStopLossPoints` | decimal | 20 | Stop-loss distance applied when `Command = Modify`. |
| `ModifyTakeProfitPoints` | decimal | 100 | Take-profit distance applied when `Command = Modify`. |
| `MinimumVolume` | decimal | 0.1 | Lower bound for the final volume after rounding. |
| `OrderComment` | string | `"Placing Order"` | Comment inserted into every order for easier auditing. |
| `Command` | `ArdOrderCommand` | `None` | Manual command to execute. Automatically reset to `None` once processed. |

## Usage notes
- Set `Command` through the UI or programmatically. The command is processed only once per change; to repeat an action set it back to `None` and then to the desired value again.
- Because stop-loss and take-profit are placed as independent orders, brokers/exchanges must support native stop/limit orders for the same security. If they do not, consider replacing them with synthetic exits in code.
- Slippage is kept as a parameter for documentation parity with the MT4 version. StockSharp's high-level market helpers do not expose an explicit slippage parameter, so the value is only informational.
- The strategy logs every important action (`LogInfo`/`LogWarn`) to assist with audit trails during discretionary execution.

## Differences compared with the original MQL expert advisor
- MetaTrader attached stops and targets directly to the market ticket. StockSharp issues separate stop and limit orders instead.
- The port uses the asynchronous event model of StockSharp. When reversing a position the entry waits until the previous position reports as closed, preventing order overlap.
- Portfolio information replaces `AccountFreeMargin`. Ensure that the portfolio adapter populates `CurrentValue` or configure `BeginValue` before starting the strategy.
- Error handling relies on StockSharp logging rather than repeated `OrderSend` retries because order submission exceptions are surfaced by the framework itself.

## Testing tips
- Run the strategy in simulation with Level 1 data to confirm that protection orders appear at the expected distances.
- Experiment with different `LotSizeDivisor` and `LotDecimals` values to match the broker's contract specifications before using the strategy in live environments.

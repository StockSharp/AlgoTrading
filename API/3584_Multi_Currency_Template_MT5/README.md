# MultiCurrency Template MT5 Strategy

## Overview

The **MultiCurrency Template MT5 Strategy** replicates the behaviour of the MetaTrader expert adviser with the same name. It trades a simple two-candle pattern on the daily timeframe while allowing the user to operate a basket of instruments simultaneously. The strategy opens an initial position only when the previous daily candle is bullish or bearish enough to trigger the pattern, then manages the trade on a faster control timeframe. A martingale averaging block adds additional tickets when price moves against the position by a configurable number of MetaTrader points, while the exit logic combines fixed take profit, break-even averaging and an optional trailing stop.

The StockSharp port keeps the multi-symbol management by letting the user define a comma-separated list of securities. Each symbol is handled independently with its own tracking context, position basket and money-management values. When the `TradeMultipair` parameter is disabled the strategy trades the main `Security` attached to the strategy instance.

## Signal generation

* The strategy subscribes to the `SignalCandleType` (daily by default) and stores two consecutive finished candles.
* A **long** setup is detected when the latest close is below the previous open and the previous candle closed above its open.
* A **short** setup is detected when the latest close is above the previous open and the previous candle closed below its open.
* Only one direction can be active at any time. New trades are ignored until the current basket is fully closed.

## Order execution

* Entries are submitted at market with the volume defined by `Lots`.
* When `NewBarTrade` is enabled the strategy waits for a finished candle on `TradeCandleType` before arming a new entry. The flag is consumed on the first trade decision to replicate the MetaTrader "trade only on a new bar" behaviour.
* Stop-loss and take-profit targets are initialised using MetaTrader pips (multiplied by the detected pip size) so the distance matches the original expert.
* If `EnableMartingale` is true, the strategy adds averaging tickets whenever price drifts by `StepPoints` away from the best entry of the current basket. Volumes are scaled by `NextLotMultiplier` raised to the number of already open tickets on that side.

## Trade management

* Take-profit behaviour depends on `EnableTakeProfitAverage`:
  * When disabled, the take-profit remains at the initial distance defined by `TakeProfitPips` from the best price in the basket.
  * When enabled and the basket contains at least two tickets, the target is shifted to the break-even price plus `TakeProfitOffsetPoints`.
* Stop-loss levels are recalculated after every fill so they reflect the worst price in the basket.
* A trailing stop acts when only one ticket is open. It reproduces the MetaTrader logic by first jumping to break-even plus `TrailingStopPoints` once the move exceeds `TrailingStopPoints + TrailingStepPoints`, then by following price with the same distance once the trade keeps advancing.
* Risk exits trigger a market order that closes the full basket in one transaction per side.

## Parameters

| Parameter | Description |
| --- | --- |
| `Lots` | Base trading volume for the first ticket in each basket. |
| `StopLossPips` | Initial stop-loss distance expressed in MetaTrader pips. |
| `TakeProfitPips` | Initial take-profit distance in MetaTrader pips. |
| `TrailingStopPoints` | Trailing distance (MetaTrader points) when only one ticket is active. |
| `TrailingStepPoints` | Extra buffer (points) required before the trailing stop is moved again. |
| `SlippagePoints` | Reserved for analytics to mimic the MetaTrader slippage input (not used for execution). |
| `NewBarTrade` | Enables the trade-on-new-bar filter based on the `TradeCandleType` candles. |
| `TradeCandleType` | Heartbeat timeframe that drives new-bar detection and money management. |
| `TradeMultipair` | When true, activates multi-symbol mode. |
| `PairsToTrade` | Comma-separated list of additional security identifiers resolved through `GetSecurity`. |
| `Commentary` | Order comment preserved for reference. |
| `EnableMartingale` | Activates the averaging block that adds tickets on adverse moves. |
| `NextLotMultiplier` | Multiplier applied to the previous ticket volume when a new averaging order is placed. |
| `StepPoints` | Distance in MetaTrader points that triggers the next averaging order. |
| `EnableTakeProfitAverage` | Enables the break-even + offset target for baskets with multiple tickets. |
| `TakeProfitOffsetPoints` | MetaTrader points added above (long) or below (short) the break-even price when averaging is active. |
| `SignalCandleType` | Timeframe used to build the two-candle pattern (daily by default). |

## Notes

* The strategy relies on market orders for both entries and exits; broker-side protective orders from MetaTrader are emulated internally.
* `PairsToTrade` must contain identifiers that the connected connector can resolve. Unknown symbols are skipped silently.
* The martingale and trailing blocks operate per symbol context, therefore each security maintains an independent basket.
* `SlippagePoints` is preserved for completeness but does not affect execution in StockSharp.


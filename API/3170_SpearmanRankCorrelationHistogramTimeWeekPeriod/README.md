# Spearman Rank Correlation Histogram Time Window Strategy

## Overview
This strategy reproduces the MetaTrader expert **Exp_SpearmanRankCorrelation_Histogram_TimeWeekPeriod** on the StockSharp high-level API. It subscribes to a single candle stream (default: 4-hour bars) and evaluates the Spearman rank correlation histogram published in the original MQL indicator. The histogram color determines whether the short-term trend is bullish (values above zero) or bearish (values below zero). A dedicated trading window keeps activity between a configurable weekday/time range, mirroring the `TimeTrade` controls of the source code.

## Trading logic
1. **Indicator calculation**
   - On every finished candle the strategy stores the close price and computes the Spearman rank correlation over `RangeLength` closes.
   - The histogram color is assigned exactly as in the indicator: `4` when the correlation is above `HighLevel`, `3` when it is between `0` and `HighLevel`, `1` when it is between `LowLevel` and `0`, `0` when it is below `LowLevel`, and `2` when it is exactly zero.
   - Signals are evaluated on the closed bar number `SignalBar` (default: the bar that just closed). The previous closed bar is used to detect color transitions.

2. **Trade modes** – the `TradeMode` parameter controls how colors are interpreted:
   - **Mode1** – open longs when the color jumps above `2` after being below `3`; open shorts when the color drops below `2` after being above `1`. Each bullish color also requests a short close, each bearish color a long close.
   - **Mode2** – open longs on color `4` (transition from anything below `4`), open shorts on color `0` (transition from anything above `0`). Colors greater than `2` close shorts; colors smaller than `2` close longs.
   - **Mode3** – open longs on color `4` and close shorts at the same time; open shorts on color `0` and close longs simultaneously.
   - After a successful entry the strategy enforces a cooldown equal to the candle length (the next order in the same direction is deferred until the next bar would have closed in MetaTrader).

3. **Money management and order size**
   - `MoneyManagement` combined with `MarginMode` converts equity or risk fractions into an order volume. Positive values follow the original money-management rules, zero falls back to the strategy `Volume`, and negative numbers are interpreted as a fixed lot size.
   - Risk-based modes (`LossFreeMargin`, `LossBalance`) require a positive `StopLossPoints`. If the stop is zero the strategy reverts to `Volume` just like the EA would refuse the trade.

4. **Risk management**
   - `StopLossPoints` and `TakeProfitPoints` are translated into price levels using `Security.PriceStep`. Exits are checked on every finished candle using the candle high/low and all open positions are reversed to flat when a level is touched.
   - `DeviationPoints` is preserved for UI completeness; StockSharp market orders ignore the value.

5. **Weekly trading window**
   - When `TimeTrade` is `true` the current time must stay between (`StartDay`, `StartHour`, `StartMinute`, `StartSecond`) and (`EndDay`, `EndHour`, `EndMinute`, `EndSecond`). Outside that window all positions on the strategy instrument are closed immediately, matching the original emergency exit.
   - The implementation assumes `StartDay` is not later than `EndDay`. For overlapping sessions (for example Friday → Monday) adjust the parameters accordingly.

6. **Miscellaneous behaviour**
   - At least `RangeLength + SignalBar + 1` completed candles must be available before signals can be generated.
   - `Direction` is a reserved switch from the MQL indicator; it is kept for parameter parity but has no effect in this port.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `MoneyManagement` | Fraction of capital or fixed lot size used for position sizing. | `0.1` |
| `MarginMode` | Interpretation of `MoneyManagement` (`FreeMargin`, `Balance`, `LossFreeMargin`, `LossBalance`, `Lot`). | `Lot` |
| `StopLossPoints` | Stop-loss distance in price points. | `1000` |
| `TakeProfitPoints` | Take-profit distance in price points. | `2000` |
| `DeviationPoints` | Informational slippage allowance in points. | `10` |
| `BuyOpen` / `SellOpen` | Enable opening long or short positions. | `true` |
| `BuyClose` / `SellClose` | Allow closing long or short positions on signals. | `true` |
| `TradeMode` | Histogram interpretation mode (`Mode1`, `Mode2`, `Mode3`). | `Mode1` |
| `TimeTrade` | Toggle the weekly trading window. | `true` |
| `StartDay`, `StartHour`, `StartMinute`, `StartSecond` | Window start (weekday and time). | `Tuesday`, `8`, `0`, `0` |
| `EndDay`, `EndHour`, `EndMinute`, `EndSecond` | Window end (weekday and time). | `Friday`, `20`, `59`, `40` |
| `CandleType` | Timeframe of the processed candles. | `H4` |
| `RangeLength` | Number of closes used by the Spearman correlation. | `14` |
| `MaxRange` | Maximum allowed `RangeLength` (safety guard). | `30` |
| `Direction` | Reserved indicator flag, no effect in the port. | `true` |
| `HighLevel`, `LowLevel` | Upper and lower histogram thresholds. | `0.5`, `-0.5` |
| `SignalBar` | Number of closed bars to look back when reading the color buffer. | `1` |

All other strategy configuration (portfolio selection, security assignment, base `Volume`, risk rules) follows the standard StockSharp workflow.

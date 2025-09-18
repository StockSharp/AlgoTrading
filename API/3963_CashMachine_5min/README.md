# Cash Machine 5 min Legacy

## Overview
Cash Machine 5 min Legacy is a StockSharp port of the MetaTrader 4 expert advisor `CashMachine_5min`. The system reacts to momentum reversals detected by the DeMarker oscillator and the fast Stochastic oscillator on five-minute candles. Once a position is open, the strategy hides its protective stop-loss and take-profit levels, revealing them only to the internal logic so that broker-side stops are not visible. Profit is protected incrementally across three user-defined milestones.

## Strategy logic
### Entry conditions
* **Long setup** – wait for the DeMarker value to rise through the 0.30 threshold while the Stochastic %K line simultaneously crosses above 20. Both conditions must change state from the previous finished candle to the current one. When flat, the strategy buys at market using the configured order volume.
* **Short setup** – mirror of the long case: DeMarker must fall through 0.70 and Stochastic %K must cross below 80. The signal is valid only when the previous candle was on the opposite side of both boundaries. The strategy sells short by market when no position is open.

### Trade management
* **Hidden risk limits** – a long position closes if price drops by the `Hidden Stop Loss` distance or rallies by the `Hidden Take Profit` distance. Shorts use the symmetric conditions with the limits inverted. The levels are monitored internally without placing real stop orders.
* **Staged trailing stop** – three take-profit checkpoints (`Target TP1`, `Target TP2`, `Target TP3`) tighten the stop as price advances. For longs, once price hits a checkpoint, the stop is raised to the candle high minus `(target − 13)` pips. For shorts, the stop is lowered to the candle low plus `(target + 13)` pips. Each stage is applied only once and never loosened.
* **Trailing execution** – after at least one stage is armed, touching the trailing stop closes the position by market order.

### Supporting mechanics
* The strategy automatically estimates the pip size from the security’s price step, supporting both 4/2-digit and 5/3-digit forex symbols.
* Indicator calculations and signals are driven by the selectable candle type (five-minute candles by default). Only finished candles are processed.

## Parameters
* **Hidden Take Profit** – hidden take-profit distance in pips (default: `60`).
* **Hidden Stop Loss** – hidden stop-loss distance in pips (default: `30`).
* **Target TP1 / TP2 / TP3** – profit milestones in pips that arm the staged trailing stop (default: `20`, `35`, `50`).
* **Order Volume** – market order volume used for entries (default: `0.2`).
* **DeMarker Length** – averaging period for the DeMarker oscillator (default: `14`).
* **Stochastic Length** – base lookback for the Stochastic oscillator (default: `5`).
* **Stochastic %K** – smoothing factor for the %K line (default: `3`).
* **Stochastic %D** – smoothing factor for the %D line (default: `3`).
* **Candle Type** – timeframe used to compute indicators (default: five-minute candles).

## Additional notes
* The strategy opens only one position at a time and will not reverse immediately; it waits for the current trade to close before a new signal is acted upon.
* Protective levels are enforced in code via market exits, so there are no pending stop orders in the order book.
* The package contains only the C# implementation; no Python version is provided.

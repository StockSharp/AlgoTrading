# Rubberbands 3 Strategy

This strategy is a StockSharp port of the MetaTrader 4 expert advisor **RUBBERBANDS_3**. It maintains two running price extremes, opens additional positions whenever price expands by a configurable distance, and liquidates the entire sequence once a counter-move of a given size occurs. After a retracement the strategy optionally flips to the opposite direction while monitoring a session-level profit and loss target.

> **Note:** StockSharp operates on netted positions. The original MT4 script can keep long and short orders simultaneously, but the port closes the active sequence before flipping direction. The general behaviour of scaling into trends and unwinding on pullbacks is preserved.

## Trading Logic

1. Record the current close price as both the running maximum and minimum (or reuse saved values when restarting).
2. When the price rises by `PipStep` points above the current maximum, submit a market buy order of size `OrderVolume` and update the maximum to the new price.
3. When the price falls by `PipStep` points below the current minimum, submit a market sell order of size `OrderVolume` and update the minimum.
4. If the market pulls back by `BackStep` points against the active direction, close all positions in that direction and set up a reversal. The opposite side is opened once the previous sequence is fully liquidated.
5. Monitor the cumulative session result. If the realised plus open profit reaches `SessionTakeProfit` × `OrderVolume`, close the session. When the drawdown while reversing exceeds `SessionStopLoss` × `OrderVolume`, close everything as well.
6. The `QuiesceNow` toggle prevents new trades when the strategy is flat. The `StopNow` flag pauses all logic, and `CloseNow` requests an immediate flattening of the portfolio.

Orders are generated from finished candles of the configured `CandleType`. The default timeframe is one minute, matching the timing of the original EA which triggered checks at the beginning of each minute.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base size of each market order. | `0.02` |
| `MaxOrders` | Maximum number of concurrent positions in a single direction. Additional entries are blocked when the limit is reached. | `10` |
| `PipStep` | Expansion distance in points that adds a new trade. | `100` |
| `BackStep` | Counter-move in points that forces an exit and prepares a reversal. | `20` |
| `QuiesceNow` | When `true`, the strategy stays idle while no positions are open. | `false` |
| `DoNow` | Opens the very first long sequence immediately after the strategy starts. | `false` |
| `StopNow` | Hard stop flag that prevents any further processing. Existing positions remain untouched. | `false` |
| `CloseNow` | Requests an immediate flat position, triggering sequential closures. | `false` |
| `UseSessionTakeProfit` | Enables the cumulative session take-profit. | `true` |
| `SessionTakeProfit` | Target profit in account currency per lot used to close the session. | `2000` |
| `UseSessionStopLoss` | Enables the cumulative session stop-loss. | `true` |
| `SessionStopLoss` | Maximum tolerated loss per lot while reversing before the session is closed. | `4000` |
| `UseInitialValues` | When restarting, reuse the manually supplied `InitialMax` and `InitialMin` instead of the latest close price. | `false` |
| `InitialMax` | Stored upper extreme reused when `UseInitialValues` is enabled. | `0` |
| `InitialMin` | Stored lower extreme reused when `UseInitialValues` is enabled. | `0` |
| `CandleType` | Candle series processed by the strategy. Defaults to one-minute candles. | `TimeFrame(1m)` |

## Session Management

- **Profit aggregation:** realised profits are accumulated after every full closure, while unrealised gains are recomputed from the weighted average entry prices of all open positions.
- **Session take-profit:** once `SessionTakeProfit` is reached, the strategy closes all trades and resets the stored extremes.
- **Session stop-loss:** during a reversal sequence (`BackStep` triggered) the strategy tracks the floating loss. If the drawdown exceeds `SessionStopLoss`, all positions are liquidated and the session restarts with cleared statistics.

## Usage Notes

- The price-step used to convert points into prices is taken from `Security.PriceStep`. Configure the instrument metadata accordingly; otherwise a fallback of `0.0001` is applied.
- Because orders are netted, the strategy executes closing trades before opening the opposite direction. When migrating legacy data, be aware that the order history may differ from hedged platforms.
- The `DoNow` flag only opens the very first long position. Additional entries follow the regular breakout conditions.
- Use `QuiesceNow` when you want to leave the strategy loaded but inactive after it flattens the book.


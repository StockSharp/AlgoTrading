# RsiBoosterStrategy

## Overview

`RsiBoosterStrategy` is a StockSharp port of the MetaTrader expert advisor *RSI booster*. The strategy compares the fast RSI value calculated on the current candle with a delayed RSI that uses the previous candle. When the difference exceeds a user-defined ratio, the strategy opens a market position and then manages the trade using fixed stops, take-profit targets, an optional trailing stop, and a loss-recovery reverse order chain.

The strategy is built on StockSharp's high-level API. It subscribes to a single candle series, relies on built-in `RelativeStrengthIndex` indicators, and uses the strategy parameter system so that all inputs are available for optimization inside Designer.

## Trading Logic

1. Two RSI indicators are calculated on each finished candle.
   * The fast RSI uses `FirstRsiPeriod` and `FirstRsiPrice` and reads the latest candle.
   * The delayed RSI uses `SecondRsiPeriod` and `SecondRsiPrice`, but the strategy keeps the previous value so it acts as a one-bar lag.
2. When `fast RSI - delayed RSI` is greater than `Ratio`, the strategy buys if no long position is open. When the difference is below `-Ratio`, it sells if no short position is open.
3. `OnlyOnePositionPerBar` ensures that at most one entry per direction occurs for the same candle time stamp.
4. After every candle the strategy evaluates stop-loss, take-profit, and trailing rules. If one of the conditions is triggered the position is closed immediately.
5. When a position is closed with a negative realized PnL, the optional recovery logic can enter a reverse position (opposite direction) with the same volume. The number of chained recovery trades is limited by `ReturnOrdersMax`.

## Risk Management

* **Stop-loss** – expressed in instrument points via `StopLossPips`. The position is closed when price crosses the stop level.
* **Take-profit** – expressed in instrument points via `TakeProfitPips`.
* **Trailing stop** – if enabled by `TrailingStopPips`, the stop starts trailing once the profit exceeds the configured distance. `TrailingStepPips` defines the minimum improvement before the trailing level is moved.
* **Return order** – activated when `ReturnOrderEnabled` is `true`. After a losing trade the strategy instantly opens a market order in the opposite direction while keeping count of how many recovery orders were issued.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Trading volume used for each market order (lots or contracts). |
| `Ratio` | Minimum RSI difference required to open a position. |
| `StopLossPips` | Stop-loss distance in instrument points. |
| `TakeProfitPips` | Take-profit distance in instrument points. |
| `TrailingStopPips` | Trailing stop distance in instrument points. |
| `TrailingStepPips` | Minimum improvement before moving the trailing stop. |
| `OnlyOnePositionPerBar` | Prevents multiple entries during the same candle. |
| `ReturnOrderEnabled` | Enables the reverse order recovery logic. |
| `ReturnOrdersMax` | Maximum number of consecutive recovery orders. |
| `FirstRsiPeriod` | Period of the fast RSI. |
| `FirstRsiPrice` | Price source for the fast RSI (matches MetaTrader applied price modes). |
| `SecondRsiPeriod` | Period of the delayed RSI. |
| `SecondRsiPrice` | Price source for the delayed RSI (matches MetaTrader applied price modes). |
| `CandleType` | Candle series used for analysis. |

## Notes

* The price-step conversion honours the instrument's `PriceStep` whenever available. If the instrument does not provide a price step, a fallback of `0.0001` is used.
* The recovery chain counter resets whenever a profitable trade occurs or when the configured maximum number of recovery orders is reached.
* The strategy draws both RSI indicators on the chart area for quick visual inspection alongside executed trades.

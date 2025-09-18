# Averaging By Signal Strategy

## Overview
The **Averaging By Signal Strategy** ports the MetaTrader expert `AveragingBySignal.mq4` to the StockSharp high-level API. The original advisor combined a moving-average crossover entry filter with Martingale-style averaging, a shared basket take-profit, and an optional trailing stop that only activates for the very first order. This C# version recreates the same building blocks while adapting them to StockSharp's netting execution model and indicator framework.

## Trading logic
1. Subscribe to the configured timeframe (`CandleType`) and feed two moving averages built with the requested periods and methods (`FastPeriod`/`FastMethod`, `SlowPeriod`/`SlowMethod`).
2. Wait for fully closed candles. When a bar completes, compare the previous and current values of both averages to detect a fast/slow crossover.
3. Generate signals:
   - a bullish crossover (fast rising above slow) yields a long signal;
   - a bearish crossover (fast dropping below slow) yields a short signal;
   - otherwise the strategy stays idle.
4. On a fresh long signal and while no long basket is active, submit a market buy order using the base volume returned by the position-sizing block.
5. On a fresh short signal and while no short basket is active, submit a market sell order.
6. Averaging rules:
   - distance to the next layer is controlled by `LayerDistancePips` converted to MetaTrader-style pips;
   - additional long layers require either a bullish signal (when `AveragingBySignal` is true) or only the price condition (when false);
   - additional short layers follow the symmetrical logic;
   - the lot size of each new layer is calculated with the `LotSizing` mode and capped at `MaxLayers` entries per direction.
7. Basket management:
   - every filled trade is tracked in FIFO order to reconstruct the average entry price of the long and short baskets;
   - the weighted average price plus/minus `TakeProfitPips` forms the shared take-profit. When the close price reaches that level the entire basket is closed;
   - if `EnableTrailing` is enabled and exactly one order exists in a basket, a trailing stop is armed after `TrailingStartPips` of floating profit. The stop is advanced whenever the price improves by at least `TrailingStepPips`.
8. The strategy works in a netting environment: opposite signals automatically offset the existing exposure before opening the next basket.

## Position sizing and pip calculation
- `InitialVolume` defines the base lot. When `LotSizing` is set to `Multiplier`, each additional layer multiplies the base lot by `Multiplier^layerIndex`, reproducing the MQL `LotType` logic.
- The helper adjusts the requested volume to the instrument's `VolumeStep`, `MinVolume`, and `MaxVolume` so that every order is exchange-compliant.
- Pip values are derived from `Security.PriceStep` and mimic the original "double-digits" adjustment: five-digit FX symbols use 0.0001 while four-digit symbols use 0.0001 as-is.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-hour time frame | Primary timeframe for indicator calculations. |
| `InitialVolume` | `decimal` | `0.1` | Base lot size for the first order in a basket. |
| `LotSizing` | `LotSizingMode` | `Multiplier` | Choose between fixed lots or geometric scaling. |
| `Multiplier` | `decimal` | `2` | Lot multiplier applied to every extra layer when `LotSizing` = `Multiplier`. |
| `FastPeriod` | `int` | `28` | Lookback of the fast moving average. |
| `FastMethod` | `MovingAverageMethod` | `LinearWeighted` | Moving-average method for the fast line. |
| `SlowPeriod` | `int` | `50` | Lookback of the slow moving average. |
| `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Moving-average method for the slow line. |
| `TakeProfitPips` | `int` | `15` | Shared take-profit distance for the entire basket (0 disables). |
| `AveragingBySignal` | `bool` | `true` | Require a fresh signal before adding new layers. |
| `LayerDistancePips` | `decimal` | `10` | Minimal adverse move (in pips) before averaging. |
| `MaxLayers` | `int` | `10` | Maximum simultaneous orders per direction, including the initial one. |
| `EnableTrailing` | `bool` | `false` | Enable the trailing stop for single-order baskets. |
| `TrailingStartPips` | `decimal` | `10` | Floating profit required before trailing starts. |
| `TrailingStepPips` | `decimal` | `1` | Additional progress needed to move the trailing stop. |

## Differences from the original expert advisor
- StockSharp operates in a netting mode, while MetaTrader 4 allowed independent hedging positions. When a signal flips direction, the new market order offsets the existing exposure before creating a fresh basket.
- The shared take-profit is implemented as an explicit exit command instead of modifying each ticket with `OrderModify`.
- The trailing stop is modelled with market exits triggered by candle close prices. The original expert relied on tick-level stop updates; therefore, the C# version may trail slightly later but follows the same thresholds.
- Risk checks such as `AccountFreeMarginCheck` and slippage handling are omitted because StockSharp brokers enforce margin/price rules directly.

## Usage tips
- Provide accurate instrument metadata (`PriceStep`, `VolumeStep`, minimum and maximum volume) for correct pip and volume conversions.
- Keep `FastPeriod` strictly lower than `SlowPeriod`; the strategy stops automatically if the configuration would prevent valid crossovers.
- Disable `AveragingBySignal` when you want a pure grid that reacts solely to price levels regardless of the latest crossover.
- Because the exit logic operates on closed candles, lower timeframes produce faster reactions but may also increase noise and the number of averaging layers.

# SwingTrader Strategy

## Overview
The **SwingTrader Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `SwingTrader.mq4`. The original EA looks for
Bollinger Band reversals: when price bounces from the outer band and the next bar crosses the middle line, the advisor opens a
position and starts building a martingale-style averaging grid. The translated strategy reproduces the same high-level behaviour
using StockSharp candles, Bollinger Bands from `StockSharp.Algo.Indicators`, and the framework's order helpers (`BuyMarket`,
`SellMarket`). Volume scaling, the width of the grid and the liquidation rules mirror the MT4 code while respecting the exchange
limits provided by the `Security` metadata.

## Trading logic
1. Subscribe to the configured timeframe (`CandleType`) and feed a Bollinger Bands indicator with `BollingerPeriod` length and a
   fixed standard deviation multiplier of `2`.
2. Work only with finished candles; the indicator callback ignores partially formed bars to replicate the MT4 `IsNewCandle()`
   guard.
3. Track whether the previous candle touched the upper or lower band. The boolean pair `_upTouch` / `_downTouch` follows the
   original toggling logic that keeps only one side active until the opposite band is touched.
4. When no basket is open:
   - open a long position if the last completed bar crossed above the middle band after previously touching the lower band;
   - open a short position if the bar crossed below the middle band after touching the upper band.
   The first order volume equals `InitialVolume` (after exchange rounding) and the initial grid width equals the latest distance
   between the upper and lower Bollinger bands.
5. When a basket exists, watch for adverse movement of one full band width from the very first fill:
   - for longs, if the candle's low is at least one band width below the anchor price, buy another slice whose size is multiplied
     by `Multiplier` with each new level;
   - for shorts, if the candle's high is one band width above the anchor price, sell an additional slice using the same
     multiplier logic.
6. Keep aggregating new orders until either the profit or the maximum tolerated loss target is hit.

## Money management and exits
- The helper `CalculateUnrealizedProfit` reproduces the MT4 floating PnL calculation by converting price differences to price
  steps (`Security.PriceStep`) and step value (`Security.StepPrice`).
- The invested capital proxy uses the original formula `Lots * Price / TickSize * TickValue / 30`, where `Lots` becomes the sum
  of grid volumes and the tick parameters are sourced from `Security`.
- Close the entire basket once the floating profit exceeds `TakeProfitFactor * invested capital`.
- Force an emergency liquidation when the floating loss reaches `10 * TakeProfitFactor * invested capital` (same ratio as the
  MT4 code).
- All exits are executed with market orders in the opposite direction; once flat, the grid state is reset and new touches must be
  detected before another entry can trigger.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TakeProfitFactor` | `decimal` | `0.05` | Multiplier applied to invested capital to define the profit target. |
| `Multiplier` | `decimal` | `1.5` | Volume multiplier for every additional averaging order. |
| `BollingerPeriod` | `int` | `20` | Number of candles used by the Bollinger Bands indicator. |
| `InitialVolume` | `decimal` | `1` | Base volume of the first trade in a new basket (rounded to venue limits). |
| `CandleType` | `DataType` | 15-minute timeframe | Timeframe used for signal generation. |

## Differences from the original EA
- StockSharp works with net positions; the strategy maintains explicit lists of grid entries to emulate MT4's ticket-based order
  handling.
- Exchange volume filters (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`) are applied automatically instead
  of manually calling `CheckVolumeValue`.
- Signals are evaluated on closed candles; intrabar triggers from the MT4 version are approximated by using candle highs and lows
  for averaging decisions.
- Orders are always sent as market instructions, whereas MT4 used `OrderSend` with explicit bid/ask parameters.

## Usage notes
- Provide realistic metadata for the traded instrument: `PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep` and `MaxVolume` must
  be populated for the profit, loss and volume calculations to match the MT4 behaviour.
- Because the averaging grid scales geometrically, test the configuration on historical data and consider broker margin
  requirements before running it live.
- The grid width equals the current Bollinger Band width; changing `BollingerPeriod` directly affects both entry timing and grid
  spacing. Validate the sensitivity during optimisation.

# Proper Bot Strategy

## Overview
The **Proper Bot Strategy** is a grid-based trading system converted from the original MetaTrader 4 "Proper Bot" expert advisor. The strategy opens a directionally biased basket of orders, expands that basket using a configurable distance/volume map and manages the whole cycle with a combination of time, volume and price based filters. The C# port relies on StockSharp high level candle subscriptions and indicators to keep the implementation close to the managed trading workflow.

## Operating principles
1. **Signal detection**
   - When the EMA filter is enabled the strategy tracks fast, mid and slow exponential moving averages on the selected candle series. Crossovers between the fast and slow averages generate direction, while the middle average blocks trades that have not yet confirmed the trend.
   - When the filter is disabled the algorithm simply reuses the previous finished candle body direction.
2. **Pre-trade filters**
   - A simple moving average of candle volume enforces a minimum average volume requirement.
   - Trading is only allowed between the configurable session start and finish time.
   - Hard upper and lower price levels prevent buying too high or selling too low. Extreme moves beyond those bands can also force an entry in the corresponding direction.
3. **Grid expansion**
   - The initial market order uses the `FirstVolume` parameter. Subsequent additions follow the `GridMap` parameter which contains a list of `distance/volume` pairs. When price moves against the current position by the configured distance a new order of the mapped volume is added.
   - Distances are interpreted in price steps using the instrument `PriceStep`. If the security does not provide that value the strategy falls back to `0.0001`.
4. **Risk management**
   - The whole basket shares an aggregated take profit and stop loss distance measured from the weighted average entry price.
   - A trailing exit monitors the sum of floating profit across the open orders. Once profit exceeds the activation threshold, any drawdown larger than `TrailStepPoints` closes the entire cycle.
   - If any exit condition triggers the strategy closes the full position with a market order and resets the grid state.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FastMaPeriod` | Length of the fast EMA used in the entry filter. | 10 |
| `MidMaPeriod` | Optional middle EMA length that must sit between the fast and slow lines to confirm a signal. Set to 0 to disable. | 25 |
| `SlowMaPeriod` | Length of the slow EMA used in the entry filter. | 50 |
| `DisableMaFilter` | When enabled the strategy ignores the EMA rules and follows the previous candle direction. | true |
| `VolumePeriod` | Number of candles used to average volume. A value of 0 disables the filter. | 1 |
| `VolumeMinimum` | Minimum average volume required to allow new entries. | 69 |
| `HighLevel` | Price threshold that blocks long entries above it and can force shorts. | 1.50001 |
| `LowLevel` | Price threshold that blocks short entries below it and can force longs. | 1.40001 |
| `FirstVolume` | Volume used for the very first order of each grid cycle. | 0.08 |
| `GridMap` | List of `distance/volume` pairs (points separated by spaces) defining how far price must move before adding the next order and what volume to use. | `120/0.1 ... 120/0.19` |
| `TakeProfitPoints` | Profit distance (in price steps) applied to the weighted average entry price for the whole basket. | 10000 |
| `StopLossPoints` | Loss distance (in price steps) applied to the weighted average entry price for the whole basket. | 30000 |
| `TrailStartPoints` | Minimum floating profit required before the trailing logic can arm itself. | 52 |
| `TrailDistancePoints` | Profit distance that must be reached (minus the trail step) before the trailing logic activates. | 52 |
| `TrailStepPoints` | Maximum profit giveback tolerated once the trailing logic is active. | 2 |
| `StartHour` / `StartMinute` | Start of the trading session (inclusive). | 06:00 |
| `FinishHour` / `FinishMinute` | End of the trading session (inclusive, supports overnight windows). | 21:00 |
| `CandleType` | Candle data type processed by the strategy. | 1 minute timeframe |

## Usage notes
- `GridMap` values are parsed using invariant culture decimals. Ensure distances are expressed in instrument points before the slash and volumes after the slash.
- All risk distances are converted using the instrument `PriceStep`. If the security exposes a different tick size configure `PriceStep` accordingly before starting the strategy.
- The trailing implementation aggregates floating profit across every open order (matching the original EA) but performs the check on completed candles. Fast intrabar exits can be enabled by running the strategy on smaller time frames.
- Forced entries produced by breaching `HighLevel` or `LowLevel` use the candle close price as a proxy for bid/ask values.
- The StockSharp port closes the entire basket when a take profit, stop loss or trailing condition is met. This differs from the MT4 implementation where each ticket carries its own stop/target but simplifies high level order management.

## Differences vs. the MT4 version
- The MT4 EA sent individual protective levels with every order. The StockSharp implementation calculates exits against the combined position to stay within the high level API.
- Bid/ask prices are approximated with the candle close price because StockSharp candle subscriptions do not deliver per-tick spreads by default.
- The trailing block uses the larger of `TrailDistancePoints - TrailStepPoints` and `TrailStartPoints` as the activation threshold so that the behaviour remains stable even when parameters overlap.
- Trade hours rely on the `DateTimeOffset` of the incoming candle. Make sure the data feed supplies timestamps in the desired time zone.

## Files
- `CS/ProperBotStrategy.cs` – strategy implementation.
- `README.md` – English description (this document).
- `README_cn.md` – Chinese translation.
- `README_ru.md` – Russian translation.


# Renko Level Strategy

## Overview
Renko Level Strategy is a faithful conversion of the MetaTrader 5 expert advisor “Renko Level EA”. It reconstructs the indicator-driven logic inside StockSharp and trades whenever the rounded Renko level jumps to a new block. The strategy interprets each level shift as a breakout of the synthetic Renko brick and enters in the breakout direction or in the opposite direction when the reverse mode is enabled.

The system uses regular time-based candles (1 minute by default) only as a data feed. Candle closes are rounded to a configurable block size that emulates Renko bricks without the need for Renko data subscriptions. Every time the rounded block changes, the strategy closes any opposite exposure and opens a new position aligned with the detected move.

## Trading Logic
1. **Initialization**
   - Detect the pip size from the instrument (`PriceStep`).
   - Convert the `Block Size` parameter from pips to price units (3-digit and 5-digit instruments automatically multiply the pip value by 10).
   - Round the first finished candle close to the nearest block to create the initial upper and lower Renko levels.
2. **Level Maintenance**
   - On each finished candle the close price is rounded to the nearest block size.
   - When the close stays within the current block, the stored levels remain unchanged.
   - When the close breaks below the lower bound, the algorithm rounds the price down and shifts the block lower (`lower = round`, `upper = round + size`).
   - When the close breaks above the upper bound, the block is shifted higher (`upper = round`, `lower = round - size`).
3. **Signal Generation**
   - A rising upper level indicates a bullish breakout of the Renko block. A falling upper level indicates a bearish breakout.
   - If `Reverse` is disabled the strategy buys on bullish shifts and sells on bearish shifts. When `Reverse` is enabled the actions are swapped.
   - When a signal is triggered, existing exposure in the opposite direction is flattened automatically (buy order closes shorts, sell order closes longs). If `Allow Increase` is disabled, the strategy refuses to add size on top of an already open position in the same direction.
4. **Order Execution**
   - Orders are sent with the strategy `Volume` setting. When reversing an existing position the order size equals the absolute position plus the configured volume so that the flip happens immediately.
   - `StartProtection()` is called during start-up so risk protections configured in Designer or via composition are active.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Block Size` | Renko block size in pips. The strategy multiplies it by the pip value of the instrument to obtain the actual price increment. Larger values reduce trade frequency. | 30 |
| `Reverse` | When `true`, invert all trading signals (buy on bearish shift, sell on bullish shift). | `false` |
| `Allow Increase` | When `true`, permits pyramiding by adding additional orders in the same direction on every signal. When `false`, a new order is only sent if the net position is flat after closing the opposite side. | `false` |
| `Candle Type` | Source candle data. Any supported `DataType` can be used; by default the strategy subscribes to 1-minute candles. | `TimeFrame(1m)` |
| `Volume` *(inherited)* | Order size used when sending market orders. Set this property on the strategy instance before starting it. | Depends on portfolio |

## Usage Notes
- Choose the block size according to the instrument volatility. For major FX pairs 30–50 pips emulate the behaviour of the original EA. On indices or crypto assets use larger block sizes.
- The strategy works with any candle feed (tick, time frame, range) as long as the candle close reflects the desired price sampling. For a pure Renko feed you can switch the candle type to a Renko data series.
- Enable `Reverse` to transform the breakout system into a mean-reversion system that fades every Renko level change.
- `Allow Increase` can be switched on to mimic the original EA’s “Increase” parameter that adds contracts on every new level in the same direction.
- Risk and money management (stop-loss, take-profit, drawdown control) can be configured through StockSharp protections or wrapper strategies. The sample keeps the logic identical to the MT5 expert and does not impose fixed exits beyond level flips.

## Data Requirements
- Historical and real-time candle data for the configured `Candle Type`.
- The instrument metadata must provide `PriceStep` and `Decimals` so that pip conversion works correctly. When these values are not available, the strategy falls back to a 0.0001 default step.

## Suggested Workflow
1. Add the strategy to Designer or create it programmatically through StockSharp API.
2. Set `Security`, `Portfolio`, `Volume`, and optionally adjust the parameters listed above.
3. Start the strategy. It will wait for the first finished candle to establish the initial Renko block.
4. Monitor the built-in trades chart or subscribe to logs to verify that orders are triggered only when the rounded level changes.

This documentation mirrors the behaviour of the original Renko Level EA while explaining how it is implemented inside StockSharp so that you can further customise or extend it.

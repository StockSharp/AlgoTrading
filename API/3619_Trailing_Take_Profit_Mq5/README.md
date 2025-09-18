# Trailing Take Profit MQ5 Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 expert advisor `TrailingTakeProfit.mq5`. The original bot continuously refreshes the take-profit level of existing positions so that the target price follows the market once a trade moves into drawdown. The StockSharp version keeps the same behaviour by monitoring the best bid/ask stream and dynamically exiting positions when the trailing target is reached.

The strategy does **not** open new positions. It only manages an existing exposure that was entered manually or by another strategy. Once the trailing logic is enabled, the component calculates activation thresholds and refreshes the effective take-profit price following the original MQL rules.

## Key Features
- Uses best bid/ask quotes via the high-level `SubscribeLevel1()` API.
- Trailing is activated only after price moves against the entry by a configurable amount of points.
- Maintains a maximum distance between the current price and the trailing target, mirroring the MQL take-profit modification.
- Closes long or short positions with market orders once the refreshed trailing level is touched.
- Automatically resets its internal state when all positions are closed or the feature is disabled.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `TrailingEnabled` | `true` | Enables or disables the trailing manager. |
| `TrailingStartPoints` | `200` | Number of price steps that price must move against the entry before the trailing target becomes active. |
| `TrailingDistancePoints` | `200` | Maximum number of price steps between the current price and the refreshed take-profit level. |

All parameters use the instrument price step to convert points to actual price distances. Optimisation ranges are provided for both numeric inputs.

## Logic Breakdown
1. Subscribe to Level1 quotes and keep track of the latest bid and ask values.
2. When a position exists and trailing is enabled, compute the adverse move from the average entry price. Once it exceeds `TrailingStartPoints`, calculate a candidate take-profit level at `TrailingDistancePoints` from the current quote.
3. For long positions, the new target is set only if it tightens the previous level (moving it lower). For short positions, the target is lifted only when it moves higher.
4. If the current market quote touches the maintained target, the strategy immediately exits the full position using a market order.
5. Whenever the position is closed or trailing is disabled, internal targets are cleared to match the MetaTrader expert behaviour.

## Usage Notes
- Attach the strategy to an instrument with an active position that you want to protect.
- Ensure the security has a valid `PriceStep` configured; otherwise the distance calculations are skipped.
- Because the strategy closes positions with market orders, make sure the volume parameter matches the exposure you intend to manage.
- The component can be combined with other entry strategies, with this manager focusing solely on trailing take profit adjustments.

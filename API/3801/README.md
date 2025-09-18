# Order Stabilization Strategy

## Overview
The **Order Stabilization Strategy** is a conversion of the MetaTrader expert advisor `hjueiisyx8lp2o379e_www_forex-instruments_info.mq4`. The original robot places a pair of stop orders around the current price and waits for a breakout. Once a position is opened, the system monitors recent candle bodies to determine whether price action has stalled ("stabilized") and exits the trade when the market loses momentum or when a predefined profit threshold is reached.

This C# port keeps the same logic by using the high-level StockSharp API. It relies on completed candles instead of raw ticks, making the behaviour deterministic during backtesting and live trading.

## Trading Rules
1. When there are no open positions or active orders, the strategy submits a **buy stop** above the market and a **sell stop** below the market. The distance is measured in MetaTrader points (usually equal to one pip).
2. If a stop order executes:
   - The filled order opens a position of `OrderVolume` lots.
   - The opposite stop order remains pending to catch a breakout in the other direction.
3. While a position is open the strategy checks the body size of the two most recent finished candles:
   - If the latest candle body is smaller than `StabilizationPoints` and the floating profit is higher than `ProfitThreshold`, the position is closed and the opposite pending order is cancelled.
   - If two consecutive candles are smaller than `StabilizationPoints`, the trade is closed regardless of current profit.
   - If the profit reaches `AbsoluteFixation`, the trade is closed immediately.
4. Pending orders are cancelled and recreated after `ExpirationMinutes` unless the value is set to zero (infinite lifetime).

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Trade volume in lots used for both stop entries. | `0.1` |
| `OrderDistancePoints` | Distance between the current close price and each stop order, expressed in MetaTrader points. | `20` |
| `ProfitThreshold` | Minimum floating profit (account currency) required before an exit triggered by stabilization is allowed. | `-2` |
| `AbsoluteFixation` | Profit level (account currency) that forces an immediate exit. | `30` |
| `StabilizationPoints` | Maximum candle body size (points) that signals a flat market. | `25` |
| `ExpirationMinutes` | Lifetime of pending stop orders in minutes. `0` disables expiration. | `20` |
| `CandleType` | Candle type used to evaluate stabilization (defaults to 5-minute time frame). | `TimeFrame(5m)` |

## Conversion Notes
- The original expert advisor operated on chart ticks. This port evaluates only finished candles, preserving the logic while ensuring reproducible backtests.
- MetaTrader "points" are mapped to the StockSharp `PriceStep`. If the instrument lacks a price step, a step of `1` is assumed.
- Profit is approximated using `PriceStep` and `StepPrice` to translate price movement into account currency.
- All code comments were rewritten in English, and parameter metadata includes user-friendly descriptions with grouping.

## Usage
1. Add the strategy to your StockSharp solution and assign the desired security and portfolio.
2. Configure the parameters, especially the candle time frame and distance in points to match the instrument characteristics.
3. Start the strategy. It will submit paired stop orders and manage positions according to the stabilization logic described above.

## Further Ideas
- Experiment with different candle intervals to balance responsiveness and noise filtering.
- Combine the strategy with volatility filters (ATR, Bollinger Bands) to avoid trading during extremely quiet sessions.
- Extend the logic with trailing stops or partial position exits once the absolute profit target is approached.

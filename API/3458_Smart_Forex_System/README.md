# Smart Forex System Strategy

## Overview
The Smart Forex System Strategy is a StockSharp port of the MetaTrader expert advisor "Smart Forex System". The robot blends a single-candle momentum filter with a martingale-style averaging grid. The first trade is opened when the previous candle shows a strong directional close and the current price has moved sufficiently away from the reference close. Additional entries are added at fixed pip intervals in the adverse direction, with the position size increasing by a configurable multiplier. The strategy manages exits through averaged take-profit levels and a safety stop-loss linked to the latest grid order.

## Trading Logic
- **Signal generation**
  - Evaluate the last completed candle on the selected timeframe.
  - Calculate a momentum ratio: `(current close - previous close) / previous close * 10,000`.
  - If the previous candle is bearish and the momentum is lower than the negative threshold, a long basket may start.
  - If the previous candle is bullish and the momentum exceeds the positive threshold, a short basket may start.
  - Trading can be limited to long-only, short-only, both directions, or disabled entirely via the `Trading Mode` parameter.
- **Grid expansion**
  - Once a basket exists, new entries are added whenever price moves against the position by at least `Grid Step` pips from the price of the last order.
  - Each new order volume is multiplied by `Lot Multiplier`. Volumes are clamped to broker limits and the configured `Max Volume`.
  - The basket stops growing when the number of orders reaches `Max Trades`.
- **Exit management**
  - A hard stop-loss is placed `Stop Loss` pips away from the price of the latest order. Breaching that distance closes the entire basket.
  - Take-profit levels depend on the basket size:
    - A single order uses `First Take Profit` pips from the volume-weighted average entry price.
    - Multiple orders use `Grid Take Profit` pips from the same average entry price to capture smaller rebounds.
  - Exits are processed on finished candles to ensure the indicators have final values.

## Risk Management Notes
- The martingale-like position sizing dramatically increases exposure in adverse trends. Use conservative multipliers and basket sizes on highly volatile instruments.
- The default stop-loss (400 pips) is intentionally wide to mirror the original EA. Consider aligning it with the instrument's ATR if smaller losses are required.
- Grid trading consumes margin quickly. Ensure the account leverage, contract size and `Start Volume` parameters are consistent with the broker specifications.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| Trading Mode | Allowed trade direction (long-only, short-only, both, or disabled). | Long & Short |
| Momentum Threshold | Minimum momentum in pseudo-pips required to trigger a signal. | 1 |
| Start Volume | Volume of the very first order in a new basket. | 0.01 |
| Max Volume | Hard cap applied to any single order volume. | 2 |
| Lot Multiplier | Multiplier used when sizing subsequent grid orders. | 1.5 |
| Grid Step | Minimum distance in pips before adding the next order. | 26 |
| Max Trades | Maximum number of orders allowed per direction. | 12 |
| First Take Profit | Take-profit distance in pips when only one order is open. | 30 |
| Grid Take Profit | Take-profit distance in pips once the basket holds multiple orders. | 7 |
| Stop Loss | Stop distance in pips from the latest order price. | 400 |
| Candle Type | Timeframe used for signal evaluation. | 1-hour candles |

## Recommended Usage
1. Attach the strategy to a forex symbol with sufficient liquidity and a predictable spread.
2. Set the `Candle Type` to match the original EA's operating timeframe (H1 by default) or adapt it to your preferred horizon.
3. Optimise the grid spacing, multiplier, and momentum filter on historical data before live deployment.
4. Monitor margin usage closely. The basket can grow rapidly, so consider coupling the strategy with account-wide equity protection.
5. Avoid overlapping with other grid-based systems on the same instrument to reduce the risk of compounding drawdowns.

## Differences Compared to the MetaTrader Version
- The StockSharp port works with finished candles instead of tick-by-tick updates, which reduces noise and makes the logic deterministic.
- Order volumes are adjusted using StockSharp security metadata (min, max, and step), ensuring compatibility with a wide range of brokers.
- Take-profit and stop-loss checks are handled inside the strategy logic instead of submitting individual order modifications for every grid level.

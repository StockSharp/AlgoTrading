# Ten Pips EURUSD Strategy

## Overview
The **Ten Pips EURUSD Strategy** is a breakout system that reproduces the logic of the original MetaTrader expert advisor. It watches the most recent completed candle and places stop orders above and below that range. Orders are sized in pips, adjusted for the current instrument tick size, and optionally managed by a trailing stop. The implementation uses StockSharp's high-level candle subscriptions together with order book updates to keep the behaviour close to the MQL version while remaining broker-neutral.

## Strategy Logic
1. Subscribe to the selected candle type and wait until a new bar becomes active.
2. Capture the previous candle high and low when that bar finishes. Pending orders are cancelled at this moment because the original EA limits them to one bar.
3. On the first tick of the new bar check that:
   - The current open lies inside the previous candle range (gap filtering).
   - The current price is at least three pip units away from both extremes (a proxy for the broker stop level).
4. Calculate the current spread using the best bid/ask. If there is no level-one data the strategy falls back to the pip size.
5. Place two pending stop orders:
   - **Buy Stop**: activation at `previousHigh + 2 × spread` with stop loss below the entry price by `StopLossPips` and, if trailing is disabled, take profit at `previousHigh + 2 × spread + TakeProfitPips`.
   - **Sell Stop**: activation at `previousLow − spread` with symmetric exit levels.
6. As soon as the candle completes, or both orders are filled/cancelled, the process repeats for the next bar.

### Position Management
- While a position is open the strategy monitors the best bid/ask on each order book update.
- If trailing is disabled, the position closes when price touches the fixed stop or take-profit level.
- If trailing is enabled:
  - For long trades the trailing stop activates once price advances by `TrailingStopPips`. The stop is set to `bid − TrailingStopPips` and moves every time price improves by at least `TrailingStepPips`.
  - For short trades the logic mirrors the long side using the ask price.
- Manual exits reset all protective levels and keep any outstanding opposite-side stop order alive until the candle ends, reproducing the straddle behaviour of the EA.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `0.01` | Order volume in lots (or contract units for non-FX symbols). |
| `StopLossPips` | `50` | Distance between entry and protective stop, expressed in pips. |
| `TakeProfitPips` | `150` | Take-profit distance in pips, used only when trailing is disabled. |
| `UseTrailing` | `false` | Enables the trailing stop logic. |
| `TrailingStopPips` | `50` | Initial distance for the trailing stop, measured in pips. |
| `TrailingStepPips` | `25` | Minimum gain (in pips) required to move an active trailing stop. |
| `CandleType` | `15 minute timeframe` | Candle series used to detect the breakout levels. |

## Notes and Recommendations
- The pip size is derived automatically from `Security.PriceStep` and emulates the MQL digits adjustment, so the strategy adapts to 3- and 5-digit FX symbols.
- All distances are recalculated in price units before placing orders, which keeps the strategy compatible with non-FX assets as long as the tick size is defined.
- The minimal stop level fallback (three pip units) mimics the behaviour of the original EA when the broker does not report a stop level.
- Because pending orders expire at the end of each candle you should run the strategy on the desired timeframe without gaps in the incoming candle stream.
- Risk management is crucial. Consider testing with realistic spreads and commission models before trading live capital.

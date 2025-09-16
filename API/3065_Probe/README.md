# Probe Strategy

## Overview
The Probe strategy reproduces the MetaTrader 5 expert advisor "Probe" inside the StockSharp high level framework. It monitors the Commodity Channel Index (CCI) on a configurable timeframe and reacts when the oscillator breaks out of a symmetric channel. When a breakout happens the strategy places a stop order offset from the current market price by a pip based indent. The approach seeks to capture momentum continuation following the breakout while keeping risk limited by pip based protective levels and an adaptive trailing stop.

## Trading Logic
1. Calculate the CCI on the configured candle type.
2. Track the previous and current CCI values to detect when the indicator exits the lower or upper channel boundary.
3. When CCI crosses upward through `-CCI Channel`, submit a buy stop order above the latest close using the `Indent (pips)` distance.
4. When CCI crosses downward through `+CCI Channel`, submit a sell stop order below the latest close using the same pip indent.
5. Only one pending stop order can remain active at a time. Opposite orders are cancelled and new signals are ignored while an order is active.

## Order Management
- Pending stop orders are withdrawn if the market moves away from the entry price by more than `1.5 * Indent (pips)`. This mirrors the MetaTrader logic that prevents stale orders from remaining in the book when momentum fades.
- Once a stop order is filled the strategy stores the executed price as the entry reference. Any opposing pending orders are cancelled immediately.

## Risk Management
- An initial stop loss is derived from `Stop Loss (pips)` and attached to the active position using internal monitoring. When price touches the stop the position is exited with a market order.
- Trailing behaviour starts after the floating profit exceeds `Trailing Stop (pips) + Trailing Step (pips)`. The stop is then moved to lock in profits while respecting the minimum trailing distance.
- All pip based distances automatically adjust for 3 and 5 digit quotes by scaling the exchange tick size.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Primary timeframe used to build candles and compute the CCI. |
| `CciLength` | Averaging period of the CCI oscillator. |
| `CciChannelLevel` | Absolute CCI threshold that forms the symmetric breakout channel. |
| `IndentPips` | Pip distance added to the last close when placing the pending stop order. |
| `StopLossPips` | Protective stop loss distance measured in pips. |
| `TrailingStopPips` | Profit threshold in pips required before the trailing stop activates. |
| `TrailingStepPips` | Additional profit distance needed before the trailing stop is moved again. |

## Notes
- Use the `Volume` property of the strategy to control the traded size.
- The strategy is designed for single position netting, matching the original Expert Advisor behaviour.
- Chart rendering draws candles, the CCI indicator and executed trades when a chart area is available.

# Coin Flip Strategy

## Overview

The Coin Flip strategy emulates the original MetaTrader expert advisor where entries are determined by a pseudo random coin toss. Only one position can be opened at a time. Each finished candle acts as a decision point: when the previous trade is flat the strategy flips a coin and immediately opens a long or short position using the calculated trade volume. Every trade is protected with both stop loss and take profit levels, while an optional trailing stop can tighten risk as the market moves in favour of the position.

A martingale style position sizing model is implemented. If the previous position was stopped out, the next trade will increase its size by a configurable multiplier. Successful trades reset the volume back to the base size. A user defined maximum volume prevents uncontrolled growth of trade size.

## Trading Rules

1. On every finished candle the strategy evaluates the current position.
2. When no position is open, a pseudo random number selects either the long or short direction. Both sides are allowed with equal probability.
3. Each new trade uses the base volume unless the previous trade ended with a stop loss. In that case the volume is multiplied by the martingale factor, respecting the maximum volume limit.
4. Protective stop loss and take profit prices are attached to every position. When the closing price reaches those thresholds, the position is exited with a market order.
5. The trailing stop monitors favourable movement. Once the profit exceeds the trailing distance plus step, the stop level is moved towards the price to secure gains.

## Parameters

- **Stop Loss** – distance in price steps used to calculate the stop loss from the entry price.
- **Take Profit** – distance in price steps added to the entry price for the take profit.
- **Trailing Stop** – profit distance that activates the trailing stop mechanism. Set to zero to disable trailing.
- **Trailing Step** – minimum additional profit required before the trailing stop is moved again.
- **Base Volume** – volume of the first trade in a martingale cycle.
- **Martingale Mult** – multiplier applied to the last stopped-out volume to determine the next order size.
- **Max Volume** – hard cap for the order size. When exceeded the trade is skipped and a warning is logged.
- **Candle Type** – candle series that defines when coin flips and risk management checks are executed.

## Notes

- The strategy uses market orders for both entries and exits to mimic the behaviour of the original expert advisor.
- Trailing stop calculations rely on the security price step. If a price step is not available, raw point values are used instead.
- Random numbers are generated with a deterministic seed based on the current time to avoid identical sequences in simultaneous runs.

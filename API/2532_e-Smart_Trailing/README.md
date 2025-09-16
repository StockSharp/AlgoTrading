# e-Smart Trailing
[Русский](README_ru.md) | [中文](README_cn.md)

The e-Smart Trailing strategy recreates the MetaTrader trailing stop utility from the original "e-Smart Trailing" expert.
It does not generate entries on its own. Instead, it supervises the currently opened position and automatically adjusts a
virtual stop level whenever the market moves in the trade's favour. The stop is moved in fixed pip increments, mirroring
the behaviour of the MQL implementation while making use of StockSharp's high-level API.

## Concept

* Works with the strategy's main security and listens to tick trades to react immediately to intraday price changes.
* When a long position is profitable, the protective level is placed below the last trade price by the configured trailing
  distance. Once price rises by at least the trailing step, the stop is pushed higher.
* When a short position is profitable, the stop is mirrored above the last trade price and tightened in the same manner
  after each trailing step.
* If price touches or crosses the maintained stop level, the strategy sends a market order to close the remaining position.
* If the position is flat, all trailing state is cleared so that the next position starts with a fresh calculation.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Trailing Stop (pips)** | Distance between the current price and the trailing stop. Values are expressed in pips (the strategy automatically adjusts for 3- and 5-digit quotes). |
| **Trailing Step (pips)** | Minimum favourable movement, in pips, before the stop level is moved again. |

## Practical Notes

* Combine this strategy with another StockSharp strategy or with manual trading to provide entries while e-Smart Trailing
takes care of the exit management.
* The stop is only advanced when the floating profit is non-negative, matching the original expert's logic that stops
  trailing during drawdowns.
* Because orders are generated as market exits, they are executed immediately once the stop is crossed, without placing
  resting stop orders on the exchange side.

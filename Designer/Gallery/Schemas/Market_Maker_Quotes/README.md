# Market Maker Quotes Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram turns a mean-reversion signal into a complete quote lifecycle. When price leaves a band around a slow average, it posts a limit order on its side of the book, follows the best quote with replacements, cancels a stale or contradicted order, and crosses the spread only after the passive attempt has timed out.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed a 100-period SimpleMovingAverage. Two formulas place the lower and upper bands 0.8% around the average.
- Market depth supplies BestBid and BestAsk. Variables sample both prices on the candle clock so book updates cannot race the candle-based signal.
- The current close must be outside a band while the previous close was still inside it. Position comparisons also prevent adding to an existing position on the same side.
- Order registering posts a buy limit at the sampled best bid or a sell limit at the sampled best ask, using one shared Quote Volume.
- Combination keeps the current order reference. Order replacing feeds every replacement back into that stream and moves a quote when its relative drift exceeds the Re-quote Threshold.
- Order cancellation pulls the opposite quote when the other band is crossed and pulls stale quotes after 12 candles. If price is still outside the band while flat, Modify position then opens at market.
- Position protection closes a filled entry at 0.8% profit or 0.4% loss. Its closing fill triggers Mass order cancellation to clear any remaining quote.

## Entry and Exit Rules

- **Long entry**: The close falls below the lower band after the previous close was at or above it, Position is not long, and a sampled best bid is available. A buy limit is posted at that bid and is replaced at a new bid when drift exceeds 0.001. If the signal reaches 12 candles while Position is still flat and price remains below the band, the quote is cancelled and an OpenPosition market buy is sent.
- **Short entry**: The close rises above the upper band after the previous close was at or below it, Position is not short, and a sampled best ask is available. A sell limit is posted at that ask and follows it through replacements. If the signal reaches 12 candles while Position is flat and price remains above the band, the quote is cancelled and an OpenPosition market sell is sent.
- **Exit**: Every entry fill from registration, replacement, or the market fallback enters Position protection. Candle closes drive a 0.8% take profit and a 0.4% stop loss. After a protective order fills, Mass order cancellation removes any quote that is still active.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Time frame used for the average, band signals, sampled book prices, quote ageing, and protection checks. |
| SMA Length | 100 | Length of the SimpleMovingAverage at the center of the bands. |
| Band Deviation | 0.008 | Fractional half-width of each band; 0.008 means 0.8% above and below the average. |
| Quote Volume | 1 | Quantity used by limit quotes, replacements, and market fallback entries. |
| Re-quote Threshold | 0.001 | Relative distance between a live quote and the current best price that triggers replacement. |
| Quote Life, candles | 12 | Number of finished candles allowed before the passive attempt becomes stale. |
| Take Profit, % | 0.8 | Favorable distance from an entry fill, in percent. |
| Stop Loss, % | 0.4 | Adverse distance from an entry fill, in percent. |

## Diagram Details

- Previous value stores the preceding candle before its close is converted, so the setup is an edge event rather than a condition that repeats on every candle outside the band.
- BestBid, BestAsk, and Position are latched by variables and released by the finished candle. All comparisons and order triggers therefore carry the current candle time.
- Each registered or replaced order enters a Combination<Order> bus. That bus feeds the price converter, replacement, cancellation, and chart, so a second replacement acts on the latest order rather than the original one.
- The age counter resets when either band is first left, advances once per candle, and is capped one step above its limit. Equality at 12 therefore emits one stale event instead of repeating forever.
- Limit registration and replacement keep the supplied prices unchanged. The market fallback is restricted to OpenPosition, while Position protection and Mass order cancellation return the diagram to a clean state after an exit.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.

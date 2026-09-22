# Bollinger Breakout with Session Order Lifecycle
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram turns a two-sided Bollinger breakout into a visible pending-order lifecycle. A finished close outside Bollinger Bands(20, 1) registers an entry at the breached band, an executed entry creates an opposite limit at the moving middle line, Order replacing follows that line, and the 07:00-20:00 session ends with Mass order cancellation and a flat position.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed Bollinger Bands with period 20 and width 1, plus the close used by both breakout comparisons.
- Working time admits new entries only from 07:00 through 20:00, and a shared Position == 0 gate deliberately makes the diagram flat-only.
- A confirmed upper breakout registers a buy limit at the upper band; a lower breakout registers the mirrored sell limit at the lower band.
- The entry fill's actual Trade.Volume sizes the opposite middle-line exit, so partial or non-default fills are not replaced by a hard-coded quantity.
- Combination keeps the newest order returned by Order replacing, while an exit fill or the end of working time removes every remaining live order.

## Entry and Exit Rules

- **Long entry**: Inside working time, a finished close above the upper band while Position == 0 registers a buy limit at that upper-band value.
- **Short entry**: Inside working time, a finished close below the lower band while Position == 0 registers a sell limit at that lower-band value.
- **Exit**: After an entry fills, an opposite limit for exactly the executed volume is registered at the Bollinger middle line and moved to each fresh middle value. Its fill cancels any leftover orders. Outside 07:00-20:00, all orders are cancelled and Modify position closes any open long or short at market.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval used by the diagram; adapted from the C# four-hour default for monthly replay. |
| Bollinger Period | 20 | Bollinger Bands Length and a real C# strategy parameter. |
| Bollinger Width | 1 | Bollinger standard-deviation multiplier and a real C# strategy parameter. |
| Session Start | 07:00:00 | Beginning of Working time, added from the source README rather than the C# constructor. |
| Session End | 20:00:00 | End of Working time; outside it the diagram cancels orders and flattens the position. |
| Order Volume | 1 | Size of each entry order; exit size is taken from the actual entry fill. |

## Diagram Details

- The executable C# uses four-hour candles. Five minutes is an explicit replay adaptation: one month contains enough completed values and breakout events for the gallery acceptance run.
- Only BandPeriod, BandWidth, and CandleType are constructor parameters in C#. Session Start and Session End come from the adjacent README; the diagram adds them through Working time and also exposes its order volume.
- The C# accepts Position <= 0 for a long signal and Position >= 0 for a short signal, closing and reversing an opposite position with market orders. This teaching diagram intentionally permits entries only at Position == 0 and does not implement reversal.
- Execution form is also adapted: the source enters and exits at market, whereas the diagram places the entry at the breached band and maintains a limit exit at the middle. An entry limit can wait for a retracement; it is not guaranteed to fill immediately.
- Order replacing returns a new order object. Each side therefore feeds both the original and replacement outputs into Combination<Order>, rather than wiring a replacement output back into its own input.
- Price shrinking is disabled on replay orders because the acceptance security does not provide a price step; the indicator levels themselves are unchanged.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.

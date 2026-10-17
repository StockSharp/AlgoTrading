# Dark Pool Prints Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades large off-exchange reports in the direction of their reported aggressor side, with the execution instrument's SMA20 as a trend filter.

Set `Security` to the execution instrument and `DarkPoolSecurity` to a separate report stream for the same underlying instrument. The latter must have a different security ID and provide genuine off-exchange ticks (`IsSystem = false`), price, volume, timestamp, and `OriginSide`. Missing or identical sources are rejected at startup. Exchange prints, unknown-side reports, invalid prices/volumes, duplicates and out-of-order reports cannot trigger entries.

A report qualifies when its volume is strictly greater than `1.5 × mean(previous 20 report volumes)`. The current report is excluded from the baseline. Buy on a reported buy above SMA20; sell on a reported sell below SMA20. Candle colour and candle volume do not determine the reported side or activity. An opposite entry reverses the whole filled position with an order of `Volume + abs(Position)`.

SMA uses finished 15-minute candles. Close a long below SMA20 and a short above SMA20, independently of report volume and entry cooldown. `CooldownBars = 0` by default; a positive value delays subsequent entries by that many finished candles. Pending orders block overlapping signals.

`StopLoss = 2` means local native protection at 2% from the actual filled entry price. Protection monitors the execution instrument's bid/ask updates and submits a market exit; a quote feed is required for protection between candles. No take-profit is configured.

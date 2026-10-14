# Premarket Gap MomoTrader
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A long-only candle momentum example with one entry per New York calendar day. “Gap” here means a close-to-previous-close increase; it does not require an overnight opening gap or an EMA crossover.

## Entry and sizing

Only finished candles are used, five minutes by default. The first candle seeds the previous close and volume.

From a flat position, the current candle must be strictly bullish, have a positive high-low range and exceed `MinVolume` in total volume. Its gain must satisfy `100 * (close - previousClose) / previousClose >= MinGainPct`, with a positive previous close. Compute `bodyPercent = 100 * (close - open) / (high - low)`:

| Body percent, inclusive | Requested entry size |
|---|---:|
| ≥ 90 | 100% of strategy `Volume` |
| ≥ 85 and < 90 | 50% |
| ≥ 75 and < 85 | 25% |
| < 75 | No entry |

Round the requested size down to the security's positive `VolumeStep`, when supplied. A zero size or a size outside supplied instrument minimum/maximum limits is rejected. No order increases an existing position.

With `UseSession=true`, submission must occur in New York time within `[SessionStart, SessionEnd)`, default 04:00–09:30. New York daylight saving time is observed. The daily entry limit applies even with the session filter disabled. A submitted entry consumes that date's allowance, including a subsequently failed order.

## Holding and exit

After entry, the first red candle (`close < open`) or candle with `volume <= previousVolume` closes the entire actual long position. Bullish candles with increasing volume hold it; a doji also holds if volume increases. Exit checks run outside the entry session and do not depend on the entry gain, body or minimum-volume filters. The exit candle cannot also enter. Pending orders block duplicate submissions, while previous close/volume still update for every finished candle.

There are no indicators, short entries, six-hour cooldown, separate stop-loss or take-profit. A new entry requires the next eligible New York date.

## Defaults

| Parameter | Default | Meaning |
|---|---:|---|
| `MinGainPct` | 5 | Inclusive minimum gain in percent; nonnegative |
| `MinVolume` | 15000 | Strict candle-volume lower bound; nonnegative, in the source data's volume units |
| `UseSession` | true | Entry session filter; C# property `IsUseSession` |
| `SessionStart` / `SessionEnd` | 04:00 / 09:30 | Ordered times within one New York day; start included, end excluded |
| `CandleType` | 5 minutes | Finished signal candles |

Behavioral tests use the existing BTC/TON archive to check every submitted order and actual filled position, all three body tiers, lot rounding, one entry per date, both exit reasons, bullish holds and New York sessions across the spring DST change. The published 5%/15000 defaults have a qualifying candle on TON; BTC mechanics fixtures use explicitly adjusted thresholds. These checks do not establish trading profitability.

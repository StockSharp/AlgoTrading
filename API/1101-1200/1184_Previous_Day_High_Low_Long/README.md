# Previous Day High Low Long
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long-only upward crosses of the previous UTC day's observed high or low, confirmed by rising native ADX inside a UTC entry session. Exits use an actual-fill profit target, observed-price trailing stop or session end.

## Levels and entries

Only finished candles, five minutes by default. Group their highs/lows by UTC candle-open date. Freeze a day's observed maximum high and minimum low when the next date starts. A previous day is available only when its first observed candle opened at midnight and the dates are consecutive; a partial initial day or missing date supplies no invented level.

A high cross is `previousClose <= previousDayHigh && close > previousDayHigh`; a low cross uses the previous day's low in the same upward direction. Current-day candles never enlarge these frozen previous-day levels. Native `AdxLength=11` ADX must be formed and strictly greater than its previous formed value. No extra ADX threshold or DI-direction test is added.

Enter only from flat, with a market buy of strategy `Volume`, while submission UTC time is in `[SessionStart, SessionEnd)`, default 09:30–16:00. Rejected crosses are not queued: close, day and ADX history advance on each finished candle. New crosses can enter after a previous position has closed; there is no daily entry cap or six-hour cooldown. No short is opened.

## Protection and session close

Distances are absolute price units: `MaxProfit=150`, `MaxStopLoss=15`, not percentages or security ticks. Initial price and high-water mark use the actual buy fill; additional partial buys update the entry's volume-weighted price. Reducing a position retains its entry basis; becoming flat resets it.

On each positive executable bid or finished close, advance high-water to the greater of its previous value and the current observed price. Close the full actual long with a market sell if price ≤ `highWater - MaxStopLoss`, or if price ≥ `entryPrice + MaxProfit`. Session-end closing takes precedence, then trailing stop, then profit target. Candle highs are not used as imagined executable prices.

A one-minute market-time timer, bid events and finished candles close any held long at/after `SessionEnd` or on the next UTC date. Session times must be ordered within one UTC day; 00:00–24:00 closes at the next midnight. Pending orders block duplicate orders until terminal; completed-candle day/ADX history continues. A candle callback that closes a position returns without entering again.

This is local monitoring, not an exchange-native stop order. Quote-based intrabar protection requires Level1 bid; candle closes remain available without quotes. Exit execution can differ from the trigger threshold.

## Defaults and checks

| Parameter | Default |
|---|---:|
| `MaxProfit` / `MaxStopLoss` | 150 / 15, positive absolute price distances |
| `AdxLength` | 11, positive |
| `SessionStart` / `SessionEnd` | 09:30 / 16:00 UTC, start included and end excluded |
| `CandleType` | 5 minutes |

C#/Python tests independently group observed days and calculate Wilder ATR/DI/DX/ADX. They check every actual entry/exit, both previous-day levels, rising-strength rejection, partial initial history, actual fills, trailing/profit quote exits and mandatory session closing on the packaged BTC/TON archive. No profitability result is asserted.

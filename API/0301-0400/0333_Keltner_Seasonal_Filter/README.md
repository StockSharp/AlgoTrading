# Keltner Seasonal Filter
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades Keltner Channel breakouts filtered by historical monthly returns. On finished 5-minute candles, use `E = EMA(20)`, `A = ATR(14)`, `M = AtrMultiplier = 2`, `U = E + M × A`, `L = E − M × A`, and `SeasonalThreshold = 0.5` percentage points. Wait for formed indicators and trading permission.

Buy when `S > SeasonalThreshold`, `Close > U` and `Position <= 0`; sell when `S < -SeasonalThreshold`, `Close < L` and `Position >= 0`. The order volume is `Volume + abs(Position)` to reverse an opposite position fully. Store a stop at signal `Close − M × A` for a long or `Close + M × A` for a short.

Check the stored stop first on each finished candle: close a long at `Close <= stop` or a short at `Close >= stop`, clear the stop and stop processing that candle. Otherwise apply entries, then close a remaining long below E or short above E. This is a manual stop checked at candle close, based on the signal close; there is no intrabar exchange stop, take-profit or trailing stop.

Requires `SeasonalityHistoryFile`: a UTF-8 CSV of monthly open/close prices with header `SecurityId,Month,Open,Close`. `Month` is `yyyy-MM`, numbers use a decimal point, prices must be positive, and each month must be unique for the execution instrument. Rows for other security IDs are ignored; missing input, malformed rows or no matching observations prevent startup.

At each signal candle, `S` is the mean of `100 × (Close / Open − 1)` for the same calendar month in prior years, within `SeasonalityLookbackYears = 10`. At least `MinimumSeasonalSamples = 2` observations are required. The current month and future months are excluded even if present in the file. Without sufficient history, no new position is opened. Supply actual monthly prices for the instrument; no fixed monthly score table is supplied.

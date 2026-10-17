# Seasonality Adjusted Momentum
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combines momentum with historical monthly returns. On finished 5-minute candles, calculate `M = Momentum(14)` and `A = SMA(14)` of M. From flat, buy when `S >= SeasonalityThreshold` and `M > A`; sell when `S <= -SeasonalityThreshold` and `M < A`. Default `SeasonalityThreshold = 0.5` is in percentage points.

Close a long if its seasonal condition is no longer satisfied or `M < A`; close a short if its seasonal condition fails or `M > A`. Every signal order starts a `CooldownBars = 120` pause in finished candles for subsequent signal decisions. Filled positions have separate local native `StopLossPercent = 2` protection from the actual execution price, monitored on bid/ask and executed at market; it does not wait for signal cooldown.

Requires `SeasonalityHistoryFile`: a UTF-8 CSV of monthly open/close prices with header `SecurityId,Month,Open,Close`. `Month` is `yyyy-MM`, numbers use a decimal point, prices must be positive, and each month must be unique for the execution instrument. Rows for other security IDs are ignored; missing input, malformed rows or no matching observations prevent startup.

At each signal candle, `S` is the mean of `100 × (Close / Open − 1)` for the same calendar month in prior years, within `SeasonalityLookbackYears = 10`. At least `MinimumSeasonalSamples = 2` observations are required. The current month and future months are excluded even if present in the file. Without sufficient history, no new position is opened. Supply actual monthly prices for the instrument; no fixed monthly score table is supplied.

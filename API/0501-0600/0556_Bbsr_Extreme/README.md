# BBSR Extreme

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A band reentry strategy with an EMA slope filter and ATR protection. `C`, `L`, `U`, `E` denote raw close, lower band, upper band and EMA; `p` denotes the preceding formed candle.

## Trading rules

Long: `C[p] < L[p] && C > L && E > E[p]`. Short: `C[p] > U[p] && C < U && E < E[p]`. Equality does not pass. Enter only while flat, by `Volume`; no pyramiding, reversal or fixed bar cooldown.

Freeze `A = ATR` from the signal candle before submitting the entry. Native local market protection uses the actual fill: long stop/target `fill - 2A` / `fill + 3A`; short `fill + 2A` / `fill - 3A`. Multipliers are parameters. Later ATR values do not move these levels. Bid/ask subscriptions allow exits between signal candles and close the actual filled volume.

Only finished candles and fully formed indicators can generate entries. An existing position is managed until its exit; pending market orders prevent duplicate submissions.

Bollinger deviation is the population standard deviation of the rolling closes, including the current finished candle.

## Parameters

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerMultiplier` | `2` |
| `MaLength` | `7 (EMA)` |
| `AtrLength` | `14 (Wilder)` |
| `AtrStopMultiplier` | `2` |
| `AtrProfitMultiplier` | `3` |
| `CandleType` | `5m` |

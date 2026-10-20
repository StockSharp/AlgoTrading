# Bias Sentiment Strength

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

On finished 15-minute candles, combine MACD(12,26,9), Wilder RSI(14), stochastic(21,14,14), Awesome Oscillator(5,34), Alligator and volume-weighted bias. The raw stochastic K uses a 21-bar high/low; `Ks` is its 14-value SMA and `D` is a further 14-value SMA. Alligator uses median-price SMMAs: jaw 13 shifted 8 bars, teeth 8 shifted 5 and lips 5 shifted 3.

`BASS = 2 * (MACD - Signal) + (RSI - 50) / 5 + 0.15 * (Ks - D) + 0.6 * AO + (Lips - Teeth) + (Teeth - Jaw) + VWMA(30) - SMA(30)`. All inputs must be formed; defaults require at least 47 completed bars. Invalid configuration fails explicitly.

A score strictly above `BassThreshold` targets a long `Volume`; below `-BassThreshold` targets a short `Volume`. Default threshold is zero. Signals use score levels, so a stopped position may re-enter without a fresh zero crossing. Opposite signals fully reverse using `Volume + abs(Position)`; a pending order blocks resubmission. Local native market protection stops the entire filled exposure at `StopLossPercent` percent (default 1); there is no take-profit.

Native local protection uses the instrument's candle and subscribed price updates. Distances are anchored to actual fills. Exits are market orders; execution prices depend on the available market and price grid.

## Parameters

| Parameter | Default |
|---|---|
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `RsiPeriod` | `14` |
| `StochK` | `21` |
| `StochSmooth` | `14` |
| `StochD` | `14` |
| `AoShort` | `5` |
| `AoLong` | `34` |
| `VolumeLength` | `30` |
| `BassThreshold` | `0` |
| `StopLossPercent` | `1` |
| `CandleType` | `15m` |

# Anomalous Holonomy Field Theory

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A six-component technical signal with fixed EMA20/EMA50, Wilder RSI14, MACD(12,26,9), Wilder ATR and volatility-normalized ROC. `C` is raw close; `A=ATR`; `clip(x,k)=max(-k,min(k,x))`. All windows include the current finished candle.

## Trading rules

`T` is +1.5 above both EMAs, +1 above EMA20 only, -1.5 below both, -1 below EMA20 only, otherwise zero. The bar VWAP is `sum(((High+Low+Close)/3)*Volume)/sum(Volume)` over `VwapPeriod` bars. `V=clip((C-VWAP)/A,2)`. From `B=T+V`, RSI adds `R=+1.5` below 30 or `+0.5` below 40 only when `B>0`; it adds `-1.5` above 70 or `-0.5` above 60 only when `B<0`; otherwise `R=0`.

`M=clip((MACD-EMA9(MACD))/A,1)`. `Q=clip((C-C[RocPeriod bars ago])/A,1)` is ROC divided by ATR%, using the same lagged price denominator. Final `S=T+V+R+M+Q`. While flat, buy `Volume` when `S>=SignalThreshold`, sell when `S<=-SignalThreshold`. Missing warmup, nonpositive ATR or zero volume blocks entry. Subsequent entries need a bar-index gap strictly greater than `CooldownBars`.

Before each entry, freeze `ATR*AtrStopMultiplier`; native local market stop protection anchors that absolute distance to actual fills and reacts to bid/ask between candles. The default stop is 2 ATR. An open position exits through protection; opposite scores do not pyramid or reverse it. The entry cooldown never postpones protection. There is no profit target.

Only finished candles and fully formed indicators can generate entries. An existing position is managed until its exit; pending market orders prevent duplicate submissions.

## Parameters

| Parameter | Default |
|---|---|
| `SignalThreshold` | `2` |
| `CooldownBars` | `50` |
| `AtrLength` | `14` |
| `AtrStopMultiplier` | `2` |
| `RocPeriod` | `10` |
| `VwapPeriod` | `20` |
| `CandleType` | `5m` |

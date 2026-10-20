# Beta Adjusted Pairs Trading

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Beta-neutral two-instrument mean reversion. Supply distinct `Asset2` and explicit `Asset2Portfolio` (it may equal the primary portfolio). There is no implicit TON instrument or account. Both betas must be positive. Use finished candles with exactly equal `OpenTime`; missing, stale and duplicate pairs never mix different timestamps.

## Trading rules

Let `h=BetaAsset1/BetaAsset2`, and let `B1,B2` be the first matched positive closes. Spread `s=ln(P1/B1)-h*ln(P2/B2)`. Compute rolling mean and population deviation of the last `LookbackPeriod` matched spreads, including the current one; `z=(s-mean)/deviation`. A zero deviation blocks entry. Long the pair only when `z[p]<=-EntryThreshold && -EntryThreshold<z<0`; short only when `z[p]>=EntryThreshold && 0<z<EntryThreshold`. Merely remaining beyond a threshold does not enter.

A long pair buys `Volume` of the primary and sells `q2=floor(Volume*P1*h/P2/VolumeStep2)*VolumeStep2` of Asset2; a short pair reverses both sides. Prices come from the matched signal bar. Secondary lot limits must allow this hedge. Its quantity remains fixed during the position. Entries require both legs flat, no pending orders and an entry-index gap strictly greater than `CooldownBars`.

At every matched finished pair, use actual weighted entry fills `F1,F2` and signed filled quantities `q1,q2`: `PnL=q1*(P1-F1)+q2*(P2-F2)` and `G=abs(q1)*F1+abs(q2)*F2`. Close both actual positions if `PnL<=-G*StopLoss/100`; otherwise exit a long at `z>=-ExitThreshold`, a short at `z<=ExitThreshold`. Loss has priority. Entry cooldown never delays exits. This is a bar-close pair loss limit before fees, not an intrabar exchange stop.

Legs are separate market orders, not an atomic pair. Pending orders block new decisions. After orders settle, a missing, rejected or incomplete leg causes actual residual positions to be closed at the next matched pair; failed closing orders are retried there. The packaged BTC/TON replay tests mechanics and does not establish profitability or a stable economic relationship.

Only finished candles and fully formed indicators can generate entries. An existing position is managed until its exit; pending market orders prevent duplicate submissions.

## Parameters

| Parameter | Default |
|---|---|
| `Asset2` | `required` |
| `Asset2Portfolio` | `required` |
| `BetaAsset1` | `1` |
| `BetaAsset2` | `1` |
| `LookbackPeriod` | `20` |
| `EntryThreshold` | `2` |
| `ExitThreshold` | `0.15` |
| `StopLoss` | `2 (%)` |
| `CooldownBars` | `120` |
| `CandleType` | `5m` |

# Anomalous Holonomy Field Theory

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

固定 EMA20/EMA50、Wilder RSI14、MACD(12,26,9)、Wilder ATR、ボラティリティ正規化 ROC からなる6成分シグナルです。`C` は元の終値、`A=ATR`、`clip(x,k)=max(-k,min(k,x))`。全窓に現在の確定足を含みます。

## 取引ルール

`T`：両 EMA より上なら +1.5、EMA20 のみより上なら +1、両方より下なら -1.5、EMA20 のみより下なら -1、他は0。足 VWAP は `VwapPeriod` 本の `sum(((High+Low+Close)/3)*Volume)/sum(Volume)`。`V=clip((C-VWAP)/A,2)`。`B=T+V` が正のときだけ RSI が30未満なら +1.5、40未満なら +0.5、負のときだけ70超なら -1.5、60超なら -0.5 を `R` とし、他は0です。

`M=clip((MACD-EMA9(MACD))/A,1)`、`Q=clip((C-C[RocPeriod 本前])/A,1)` は同じ過去価格を分母にする ROC/ATR% です。`S=T+V+R+M+Q`。保有なしで `S>=SignalThreshold` なら `Volume` 買い、`S<=-SignalThreshold` なら売り。準備不足、ATR 非正、出来高ゼロでは建てません。新規建て足インデックスの間隔は `CooldownBars` を厳密に超える必要があります。

各発注前に `ATR*AtrStopMultiplier` を固定し、実約定値基準のネイティブなローカル成行ストップが足の間でも bid/ask に反応します。初期値は2 ATR。保有中は保護で決済し、逆シグナルで追加や反転をしません。新規待機期間はストップを遅らせず、利確目標はありません。

指標のウォームアップ完了後、確定足だけで新規シグナルを生成します。保有ポジションは決済まで管理し、未完了の成行注文がある間は重複発注しません。

## パラメーター

| Parameter | Default |
|---|---|
| `SignalThreshold` | `2` |
| `CooldownBars` | `50` |
| `AtrLength` | `14` |
| `AtrStopMultiplier` | `2` |
| `RocPeriod` | `10` |
| `VwapPeriod` | `20` |
| `CandleType` | `5m` |

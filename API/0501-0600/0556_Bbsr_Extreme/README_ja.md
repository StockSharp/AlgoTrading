# BBSR Extreme

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

バンド内への回帰を EMA 傾きで絞り込み、ATR で保護します。`C,L,U,E` は元の終値、下側バンド、上側バンド、EMA、`p` は直前の指標形成済み足です。

## 取引ルール

買い：`C[p] < L[p] && C > L && E > E[p]`。売り：`C[p] > U[p] && C < U && E < E[p]`。等号は対象外です。ノーポジション時のみ `Volume` で新規建てし、追加、反転、固定待機足数はありません。

発注前にシグナル足の `A=ATR` を固定します。実約定価格を基準とするネイティブのローカル保護：買いの損切り/利確は `fill-2A` / `fill+3A`、売りは `fill+2A` / `fill-3A`。倍率は変更可能で、後続 ATR による移動はありません。bid/ask 購読により足の間でも実約定数量を成行決済します。

指標のウォームアップ完了後、確定足だけで新規シグナルを生成します。保有ポジションは決済まで管理し、未完了の成行注文がある間は重複発注しません。

ボリンジャーバンドは現在の確定足を含む終値の移動窓から母標準偏差を計算します。

## パラメーター

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerMultiplier` | `2` |
| `MaLength` | `7 (EMA)` |
| `AtrLength` | `14 (Wilder)` |
| `AtrStopMultiplier` | `2` |
| `AtrProfitMultiplier` | `3` |
| `CandleType` | `5m` |

# BB RSI Trailing Stop

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ボリンジャーバンド逸脱後、RSI が極端域から戻った場合のみ新規建てし、条件付きトレーリングストップを使います。`p` は直前の指標形成済み足、RSI は Wilder 平滑です。

## 取引ルール

買い：`Low < lowerBand` かつ `RSI[p] <= RsiOversold && RSI > RsiOversold`。売り：`High > upperBand` かつ `RSI[p] >= RsiOverbought && RSI < RsiOverbought`。極端域に留まるだけでは不十分です。ノーポジション時にのみ `Volume` で発注し、隠れた待機期間はありません。

加重平均実約定価格が基準で、買いは執行可能な bid、売りは ask を使います。初期距離は `StopLossPoints`。有利な気配値極値が `TrailOffsetPoints` 進んだ後に追従を開始し、極値から `TrailStopPoints` 離して初期ストップを緩めず更新します。気配値到達で実保有数量を全て成行決済します。距離は価格単位で、ティックや百分率ではありません。足の過去の高安を、その後に作ったストップ判定へ遡及利用しません。

指標のウォームアップ完了後、確定足だけで新規シグナルを生成します。保有ポジションは決済まで管理し、未完了の成行注文がある間は重複発注しません。

ボリンジャーバンドは現在の確定足を含む終値の移動窓から母標準偏差を計算します。

## パラメーター

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `25` |
| `BollingerDeviation` | `2` |
| `RsiPeriod` | `14` |
| `RsiOversold` | `33` |
| `RsiOverbought` | `60` |
| `StopLossPoints` | `50` |
| `TrailOffsetPoints` | `99` |
| `TrailStopPoints` | `40` |
| `CandleType` | `15m` |

# Bearish Wick Reversal

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定した 1 分足の下ひげ反転で買いだけを行います。建玉がなければ、`Close < Open` かつ `100 * (Low - Close) / Close <= Threshold` のとき `Volume` を買います。既定値 `-1` は、終値の 1% 以上の下ひげを要求します。

足の UTC 開始時刻が両端を含む `StartTime`–`EndTime` 内であることが新規条件です。`UseEmaFilter` 有効時は `EMA(EmaPeriod)` の形成と `Close > EMA` も必要です。無効時は EMA の準備完了を待ちません。

確定終値が直前の確定足の高値を上回れば、実際の買い建玉を全量決済します。新規の期間制限と EMA は決済を妨げません。売り建て、隠れた待機本数、自動利確・損切りはありません。未完了の注文がある間は重複発注しません。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `Threshold` | `-1` |
| `UseEmaFilter` | `false` |
| `EmaPeriod` | `200` |
| `CandleType` | `1m` |
| `StartTime` | `2014-01-01T00:00:00Z` |
| `EndTime` | `2099-01-01T00:00:00Z` |

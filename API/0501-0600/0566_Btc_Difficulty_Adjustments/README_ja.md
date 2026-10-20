# BTC Difficulty Adjustments

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

表頭が `Time,Difficulty` の `DifficultyHistoryFile` が必須です。公開時刻は厳密に増加する `Z` 付き ISO UTC 時刻、難易度は正である必要があります。起動時に読み込み、欠落や不正形式は明示的に失敗します。市場価格から採掘難易度を推定しません。

確定足では `OpenTime` 以前に公開された観測だけを使用します。最初の値は基準となり、変化なしではシグナルを出しません。変化率は `100*(difficulty/previous-1)`。閾値モード無効時は上昇で買い、低下で売りです。有効時は `abs(change)>=Threshold` のときのみ、低下で買い、上昇で売ります。条件を満たす最新の既知の変化で方向を決定します。新規は `Volume`、反転は `Volume+abs(Position)` です。未完了注文は判断の重複を防ぎ、自動ストップはありません。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `CandleType` | `1d` |
| `ThresholdMode` | `false` |
| `Threshold` | `10` |
| `DifficultyHistoryFile` | `必須` |

# BTCUSD Momentum After Abnormal Days

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定日足のリターンは `r=(Close-Open)/Open` です。現在を除く直前 `LookbackPeriod` 本の確定日次リターンで平均と母標準偏差を計算し、前窓全体の形成を要求します。`r>mean+k*sd` で買い、`r<mean-k*sd` で売り、`k=AbnormalThreshold`。閾値と等しい場合は入りません。

新規はフラット時のみです。数量は `floor(CapitalPerTrade/Close/VolumeStep)*VolumeStep`。最小未満は見送り、最大超過は拒否します。資金額はシグナル終値基準で、約定価格は異なる場合があります。約定済みの全数量を次の日足終値、欠損時には最初の利用可能な後続日足終値で決済します。同じ足では決済を優先し再入場しません。未完了注文は重複を防ぎ、価格ストップは設定しません。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `CandleType` | `1d` |
| `LookbackPeriod` | `5` |
| `AbnormalThreshold` | `1.6` |
| `CapitalPerTrade` | `1000` |

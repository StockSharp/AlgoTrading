# BTC Outperform

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定した UTC 日足を固定期間へ集約します。週は月曜日 1970-01-05、90日区間は 1970-01-01 を起点とします。期間の開始から終了まで全日足が揃ったときだけ終値を公開します。起動時の部分期間や欠損を含む期間は終値を公開しません。

直近の完全に観測された確定期間の終値を比較し、週が高ければ買い、低ければ売り、同値なら保有を維持します。新規数量は `Volume`、反転数量は `Volume+abs(Position)` で、反対方向の規定数量を残します。形成中の期間は比較に影響しません。未完了注文で重複発注を防ぎ、自動の価格ストップや利確目標はありません。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `CandleType` | `1d` |
| `WeeklyDays` | `7` |
| `QuarterlyDays` | `90` |

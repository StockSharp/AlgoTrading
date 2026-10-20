# BTC DCA AHR999

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定した UTC 日足を使用します。現在を含む直近 `Length` 本の正の終値から `G=exp(mean(ln(C)))` を計算します。BTC の年齢は 2009-01-03 からの整数日数で、`F=10^(5.84*log10(age)-17.01)`、`AHR999=(C/G)*(C/F)` です。窓全体の形成が必要です。

指定期間内の月曜日に、指数が厳密に 0.45 未満なら `UsdInvest2`、それ以外で 1.2 未満なら `UsdInvest1` の予算で購入します。他の指数、曜日、日付では購入しません。UTC 日付の両端を含み、日足ごとに最大1回判断します。数量は `floor(budget/C/VolumeStep)*VolumeStep` で、最小数量未満は見送ります。予算はシグナル終値基準で、成行約定価格は異なる場合があります。未完了注文は重複発注を防ぎます。自動決済はなく保有を積み立てます。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `CandleType` | `1d` |
| `Length` | `200` |
| `UsdInvest1` | `100` |
| `UsdInvest2` | `1000` |
| `StartDate` | `2024-02-01T00:00:00Z` |
| `EndDate` | `2025-12-31T00:00:00Z` |

## 参考資料

- [AHR999 geometric-cost implementation](https://github.com/alibaba-web3/quant-master/blob/main/ahr999_index.py)

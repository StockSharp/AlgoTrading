# Commodity Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

M は判断月、P[K] は完了した暦月 K の最後に観測した日次終値です。Momentum = P[M − SkipMonths − 1] / P[M − LookbackMonths − 1] − 1。端点間の全月が必要です。既定では三月に当年一月と前年二月を比較し、二月と未完了の三月を除きます。上位 TopN を均等配分で買い持ちします。負のモメンタムも対象で絶対収益率フィルターはありません。除外銘柄は次の月次調整で閉じます。

時刻が一致する確定足だけを評価します。統計は異なる UTC 日付を数え、同じ日の後続足が日次価格を更新します。毎月最初の条件を満たす同期確定足で一度リバランスします。データ不足時は延期し、同点は Security.Id 順に並べます。

## 入力データ

PriceHistoryFile は正の日次終値の任意 CSV です：SecurityId,Date,Close。日付は yyyy-MM-dd。現在・将来の日付で判断を先取りせず、実際の確定足で該当 UTC 日の価格を上書きします。重複日付は禁止です。短い約定履歴で長い期間を準備するために使えます。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `PriceHistoryFile` | 任意 — 未指定 |
| `TopN` | `5` |
| `LookbackMonths` | `12` |
| `SkipMonths` | `1` |

## 約定と保有数量

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

信号終値で数量に換算し、VolumeStep 単位でゼロ方向へ丸めます。実際の約定残高を使い、未完了注文を待ち、MinVolume/MaxVolume を守って大口調整を後続足へ分割します。MinTradeUsd は小口調整を除外しますが、全決済には適用しません。

## 検証

約定検証には既存 BTC/TON アーカイブのみを使います。明示した財務報告、オプション観測、準備価格は仕組みのテスト用で、実企業財務やオプション履歴ではありません。短縮期間、二分位、三月の年次調整は明示的な検証設定で、元の長期既定値の取引を証明しません。収益率は主張しません。

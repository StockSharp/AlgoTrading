# Betting Against Beta

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

Universe と BenchmarkSecurity の日付をそろえ、WindowDays + 1 共通日付を要求します。単純収益率 r[d] = P[d] / P[d−1] − 1。beta はベンチマークとの共分散をベンチマーク分散で割った値です。分散ゼロなら延期します。最低 beta の Deciles を買い、最高を売り、片側 floor(Universe 数 / Deciles)。日中の beta 閾値、ストップ、beta 中立化のレバレッジ調整はありません。

時刻が一致する確定足だけを評価します。統計は異なる UTC 日付を数え、同じ日の後続足が日次価格を更新します。毎月最初の条件を満たす同期確定足で一度リバランスします。データ不足時は延期し、同点は Security.Id 順に並べます。

日中 CandleType の日次統計は現在の信号日より前の UTC 日付のみを使い、未完了の当日は除外します。確定済み一日足は自身の完了日を含められます。

## 入力データ

PriceHistoryFile は正の日次終値の任意 CSV です：SecurityId,Date,Close。日付は yyyy-MM-dd。現在・将来の日付で判断を先取りせず、実際の確定足で該当 UTC 日の価格を上書きします。重複日付は禁止です。短い約定履歴で長い期間を準備するために使えます。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `PriceHistoryFile` | 任意 — 未指定 |
| `BenchmarkSecurity` | 必須 |
| `WindowDays` | `252` |

## 約定と保有数量

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

買い・売りの各バスケットに Capital × Leverage の半分を均等配分します。信号終値で数量に換算し、VolumeStep 単位でゼロ方向へ丸めます。実際の約定残高を使い、未完了注文を待ち、MinVolume/MaxVolume を守って大口調整を後続足へ分割します。MinTradeUsd は小口調整を除外しますが、全決済には適用しません。

## 検証

約定検証には既存 BTC/TON アーカイブのみを使います。明示した財務報告、オプション観測、準備価格は仕組みのテスト用で、実企業財務やオプション履歴ではありません。短縮期間、二分位、三月の年次調整は明示的な検証設定で、元の長期既定値の取引を証明しません。収益率は主張しません。

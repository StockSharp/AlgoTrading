# FX Carry Trade

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

毎月の carry = 観測された年率 BaseRate − QuoteRate です。最高 TopK 組を買い、最低 TopK 組を売ります。重複を防ぐため最低 2×TopK 組が必要です。各側に米ドル総予算の半分を均等配分します。決済通貨が USD 以外の場合も、明示的な観測値 UsdPerUnit で米ドル額をネイティブ取引数量へ変換します。次回に対象外となった通貨ペアを決済し、継続ポジションを調整します。

時刻が一致する確定足だけを評価します。統計は異なる UTC 日付を数え、同じ日の後続足が日次価格を更新します。毎月最初の条件を満たす同期確定足で一度リバランスします。データ不足時は延期し、同点は Security.Id 順に並べます。

全観測値はシグナル足の終値時刻までに公表されている必要があります。日中足では当日 UTC 日付より前の日足価格だけを使い、確定日足では自身の日付を使えます。将来の CSV 日付で判断を先取りしません。

公表情報の締切は、同期した確定足の実際の CloseTime の最大値です。取引セッションは設定足の全期間が経過する前に終了する場合があります。

## 入力データ

RatesFile は必須 CSV：`SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`。PublishedAt は UTC `yyyy-MM-ddTHH:mm:ssZ`、金利は同じ年率の小数単位です。UsdPerUnit はネイティブ取引単位当たりの正の米ドル価値です。公表済み最新値で経過日数 <= MaxRateAgeDays のものだけを使い、銘柄・公表時刻の重複を拒否します。金利を足の形から生成しません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `TopK` | `3` |
| `RatesFile` | 必須 |
| `MaxRateAgeDays` | `45` |

## 約定と保有数量

Capital は固定米ドル予算、Leverage は総エクスポージャーを制御します。数量と MinTradeUsd には RatesFile.UsdPerUnit を使います。VolumeStep に従いゼロ方向へ丸め、実約定と照合して未完了注文を待ちます。MinVolume/MaxVolume を守り、大きな調整を後続足へ分割します。全決済は MinTradeUsd を迂回します。ストップロスや利食い注文はありません。

## 検証

両言語を同梱 BTC/TON 約定データと明示的な金利・財務・評価・文章・事前履歴データで検証します。カレンダー検証はアーカイブの OHLC・出来高の日付を移し、決まった約定気配を与えたもので、複数年の市場履歴ではありません。独立計算、注文方向・数量、実約定、残高、禁止判断日を確認します。2区分、K=1、AssetDecile=1 は明示的な機構検証設定であり、全本番銘柄群の証明ではありません。収益を保証しません。

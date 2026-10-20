# Momentum Asset Growth

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

最新公表済み年次報告と前年同日の会計数値から資産成長率 = TotalAssets[current] / TotalAssets[previous] − 1 を求めます。上位 AssetDecile 区分 floor(財務有効数 / AssetDecile) 銘柄だけを残します。その中で、判断月の月初から SkipMonths 暦月前よりも前に終了する MomLook + 1 個の日足終値を使い、MomLook 日モメンタムを計算します。最上位 Quintile 区分を買い、最下位を売ります。選別後に最低 Quintile 銘柄の履歴が必要です。1月を除く毎月に組み替え、1月は保有を維持し、新規配分や強制決済を行いません。

時刻が一致する確定足だけを評価します。統計は異なる UTC 日付を数え、同じ日の後続足が日次価格を更新します。毎月最初の条件を満たす同期確定足で一度リバランスします。データ不足時は延期し、同点は Security.Id 順に並べます。

全観測値はシグナル足の終値時刻までに公表されている必要があります。日中足では当日 UTC 日付より前の日足価格だけを使い、確定日足では自身の日付を使えます。将来の CSV 日付で判断を先取りしません。

公表情報の締切は、同期した確定足の実際の CloseTime の最大値です。取引セッションは設定足の全期間が経過する前に終了する場合があります。

## 入力データ

FundamentalsFile は必須 CSV です：SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding。PeriodEnd は yyyy-MM-dd、PublishedAt は yyyy-MM-ddTHH:mm:ssZ（UTC）。資産と株数は正である必要があります。信号時点で公表済みの最新会計期間と最新改訂だけを選びます。重複改訂や期末より前の公表は禁止です。足から財務値を作りません。

PriceHistoryFile は正の日次終値の任意 CSV です：SecurityId,Date,Close。日付は yyyy-MM-dd。現在・将来の日付で判断を先取りせず、実際の確定足で該当 UTC 日の価格を上書きします。重複日付は禁止です。短い約定履歴で長い期間を準備するために使えます。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | 任意 — 未指定 |
| `MomLook` | `252` |
| `SkipMonths` | `1` |
| `AssetDecile` | `10` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | 必須 |

## 約定と保有数量

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

買い・売りの各バスケットに Capital × Leverage の半分を均等配分します。信号終値で数量に換算し、VolumeStep 単位でゼロ方向へ丸めます。実際の約定残高を使い、未完了注文を待ち、MinVolume/MaxVolume を守って大口調整を後続足へ分割します。MinTradeUsd は小口調整を除外しますが、全決済には適用しません。

## 検証

両言語を同梱 BTC/TON 約定データと明示的な金利・財務・評価・文章・事前履歴データで検証します。カレンダー検証はアーカイブの OHLC・出来高の日付を移し、決まった約定気配を与えたもので、複数年の市場履歴ではありません。独立計算、注文方向・数量、実約定、残高、禁止判断日を確認します。2区分、K=1、AssetDecile=1 は明示的な機構検証設定であり、全本番銘柄群の証明ではありません。収益を保証しません。

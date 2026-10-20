# F-Score Reversal

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

毎月 Lookback 日収益率 P[d] / P[d−Lookback] − 1 を計算します。公表済み FScore >= FHi の中で最低収益率を買い、IsShortEnabled なら FScore <= FLo の中で最高収益率を売ります。境界を含み、FHi > FLo が必要です。Piotroski 評価は公表済みの整数 0..9 とし、最新の既知期間・改訂と MaxReportAgeDays を適用します。各側に総予算の半分を配分し、欠けた側は現金のままです。旧ポートフォリオの全決済約定を待ってから当月分を建てます。

時刻が一致する確定足だけを評価します。統計は異なる UTC 日付を数え、同じ日の後続足が日次価格を更新します。毎月最初の条件を満たす同期確定足で一度リバランスします。データ不足時は延期し、同点は Security.Id 順に並べます。

全観測値はシグナル足の終値時刻までに公表されている必要があります。日中足では当日 UTC 日付より前の日足価格だけを使い、確定日足では自身の日付を使えます。将来の CSV 日付で判断を先取りしません。

公表情報の締切は、同期した確定足の実際の CloseTime の最大値です。取引セッションは設定足の全期間が経過する前に終了する場合があります。

## 入力データ

PriceHistoryFile は正の日次終値の任意 CSV です：SecurityId,Date,Close。日付は yyyy-MM-dd。現在・将来の日付で判断を先取りせず、実際の確定足で該当 UTC 日の価格を上書きします。重複日付は禁止です。短い約定履歴で長い期間を準備するために使えます。

FScoreFile は必須：`SecurityId,PeriodEnd,PublishedAt,FScore`。PeriodEnd は `yyyy-MM-dd`、PublishedAt は UTC `yyyy-MM-ddTHH:mm:ssZ`。整数 0..9、一意な改訂、PeriodEnd 以降の公表だけを認めます。最新の既知期間・改訂を使い、MaxReportAgeDays は PeriodEnd から数えます。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | 任意 — 未指定 |
| `Lookback` | `21` |
| `FHi` | `7` |
| `FLo` | `3` |
| `IsShortEnabled` | `true` |
| `MaxReportAgeDays` | `550` |
| `FScoreFile` | 必須 |

## 約定と保有数量

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

目標数量は VolumeStep に従いゼロ方向へ丸めます。新規建ての前に全決済の実約定を待ち、実保有数量と照合します。MinVolume/MaxVolume を守り、大きな調整を後続足へ分割します。MinTradeUsd は非ゼロ目標への調整を制限し、全決済には適用しません。

## 検証

両言語を同梱 BTC/TON 約定データと明示的な金利・財務・評価・文章・事前履歴データで検証します。カレンダー検証はアーカイブの OHLC・出来高の日付を移し、決まった約定気配を与えたもので、複数年の市場履歴ではありません。独立計算、注文方向・数量、実約定、残高、禁止判断日を確認します。2区分、K=1、AssetDecile=1 は明示的な機構検証設定であり、全本番銘柄群の証明ではありません。収益を保証しません。

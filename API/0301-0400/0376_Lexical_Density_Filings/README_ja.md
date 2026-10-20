# Lexical Density Filings

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

各銘柄の最新公表済みプレーンテキスト報告書を読みます。Unicode の文字・結合記号の連続を単語とし、.NET の不変カルチャで小文字化します。語彙密度 = 正規化後の異なる単語数 / 全単語数です。先頭銘柄の取引所カレンダーに従い、2・5・8・11月の最初の3取引日だけで配分します。その期間内に Quintile 件以上が揃わなければ次回まで待ちます。密度の最上位を買い、最下位を売り、各側 floor(有効数 / Quintile) 銘柄とします。期間中は保有を維持します。

全観測値はシグナル足の終値時刻までに公表されている必要があります。日中足では当日 UTC 日付より前の日足価格だけを使い、確定日足では自身の日付を使えます。将来の CSV 日付で判断を先取りしません。

公表情報の締切は、同期した確定足の実際の CloseTime の最大値です。取引セッションは設定足の全期間が経過する前に終了する場合があります。

## 入力データ

FilingsFile は必須：`SecurityId,PeriodEnd,PublishedAt,DocumentPath`。日付は `yyyy-MM-dd` と UTC `yyyy-MM-ddTHH:mm:ssZ`。DocumentPath は UTF-8 プレーンテキストへの絶対パスまたはマニフェスト基準の相対パスで、コンマは禁止です。空文書、重複改訂、PeriodEnd より前の公表を拒否します。終値時刻までの最新公表期間・改訂と MaxReportAgeDays を適用します。先頭銘柄には祝日を含む取引所カレンダーが必要です。

先頭の Unicode カテゴリーが文字または結合記号のテキスト要素を単語に含め、句読点と数字を境界とします。不変の大小文字変換のみを行い、語幹処理やアクセントの正規化は行いません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FilingsFile` | 必須 |

## 約定と保有数量

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

買い・売りの各バスケットに Capital × Leverage の半分を均等配分します。信号終値で数量に換算し、VolumeStep 単位でゼロ方向へ丸めます。実際の約定残高を使い、未完了注文を待ち、MinVolume/MaxVolume を守って大口調整を後続足へ分割します。MinTradeUsd は小口調整を除外しますが、全決済には適用しません。

## 検証

両言語を同梱 BTC/TON 約定データと明示的な金利・財務・評価・文章・事前履歴データで検証します。カレンダー検証はアーカイブの OHLC・出来高の日付を移し、決まった約定気配を与えたもので、複数年の市場履歴ではありません。独立計算、注文方向・数量、実約定、残高、禁止判断日を確認します。2区分、K=1、AssetDecile=1 は明示的な機構検証設定であり、全本番銘柄群の証明ではありません。収益を保証しません。

# Fed Model

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

Universe の順序は [株式 ETF、任意の現金 ETF] です。日足と先頭銘柄の取引所カレンダーを使います。毎月最終取引日の終値時刻までに公表された RegressionMonths 個の連続月次観測が必要です。切片付き回帰は y = EarningsYield − BondYield、x = 0..N−1、slope = Σ((x−mean(x))×(y−mean(y))) / Σ(x−mean(x))²、予測 = mean(y) + slope×(N−mean(x)) です。正なら株式、負なら現金 ETF または現金、ゼロなら前の状態を維持します。状態が変わるときだけ売買し、同じ状態で月次の数量調整は行いません。旧ポジションの全決済約定後に次を買います。

全観測値はシグナル足の終値時刻までに公表されている必要があります。日中足では当日 UTC 日付より前の日足価格だけを使い、確定日足では自身の日付を使えます。将来の CSV 日付で判断を先取りしません。

公表情報の締切は、同期した確定足の実際の CloseTime の最大値です。取引セッションは設定足の全期間が経過する前に終了する場合があります。

## 入力データ

YieldHistoryFile は必須：`Month,PublishedAt,EarningsYield,BondYield`。Month は `yyyy-MM`、PublishedAt は UTC `yyyy-MM-ddTHH:mm:ssZ`。両利回りは同じ単位です。判断足の終値までに公表された改訂だけを使います。判断月を含む連続月が必要で、欠損時は当月予測を待ちます。重複改訂と観測月より前の公表を拒否します。逆数価格で代用せず、観測された市場・財務系列を入力します。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `RegressionMonths` | `12` |
| `YieldHistoryFile` | 必須 |

## 約定と保有数量

Universe を明示し、価格と Capital は取引単位当たり米ドルです。買い保有額の上限は min(Capital, 報告された Portfolio.CurrentValue) で、価値が不明または非正なら購入予算はゼロです。レバレッジ、ストップロス、利食い注文はありません。

目標数量は VolumeStep に従いゼロ方向へ丸めます。新規建ての前に全決済の実約定を待ち、実保有数量と照合します。MinVolume/MaxVolume を守り、大きな調整を後続足へ分割します。MinTradeUsd は非ゼロ目標への調整を制限し、全決済には適用しません。

## 検証

両言語を同梱 BTC/TON 約定データと明示的な金利・財務・評価・文章・事前履歴データで検証します。カレンダー検証はアーカイブの OHLC・出来高の日付を移し、決まった約定気配を与えたもので、複数年の市場履歴ではありません。独立計算、注文方向・数量、実約定、残高、禁止判断日を確認します。2区分、K=1、AssetDecile=1 は明示的な機構検証設定であり、全本番銘柄群の証明ではありません。収益を保証しません。

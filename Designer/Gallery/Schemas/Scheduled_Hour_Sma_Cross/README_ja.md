# 指定時刻の SMA クロス
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、BTCUSDT の確定済み30分足で短期と長期の移動平均を評価し、足の開始時刻の Hour が 12 のときに予定エントリーを許可し、それ以外の時間にはトレンドに反するポジションを決済し、各成行アクションの間に8本のクールダウンを設けます。

![schema](schema.svg)

## 戦略の概要

- 確定済み30分足を、形成済みの値だけを出力する SMA(8) と SMA(21) に渡します。同じ足について両方の値がそろうまで、判断は出力されません。
- 上昇トレンドは `SMA(8) > SMA(21)`、下降トレンドは `SMA(8) < SMA(21)` です。両者が等しい場合はアクションを行いません。
- Time ブロックが判断時刻を出力し、Converter がその Hour を抽出します。確定足の処理単位では、この時刻はルールが使う足の `OpenTime` と一致します。1回限りの Flag により、同期的な注文イベントの処理中でも、その足の判断は厳密に1回だけです。
- 約定駆動の状態はネットポジションを `-1`、`0`、`1` として記録します。4本のアクション経路はそれぞれ注文送信前に専用の次状態を用意し、その経路が約定を通知した場合にだけ状態を反映します。
- 各アクションは8本のクールダウンを開始します。後続の1本目から7本目まではブロックされ、8本目の確定足は判断前にカウンターをゼロまで減らすため、再びアクション可能です。

## エントリーと決済のルール

- **予定された上昇アクション**: `OpenTime.Hour` が 12 の足で、上昇トレンドかつフラットまたはショートなら、Volume 1 の成行買いを送信します。ショートはゼロまで減少し、その場で反転しません。
- **予定された下降アクション**: 同じ Hour の足で、下降トレンドかつフラットまたはロングなら、Volume 1 の成行売りを送信します。ロングはゼロまで減少し、その場で反転しません。
- **指定 Hour 以外の決済**: 他の開始 Hour では、下降トレンドなら成行売り1回でロングを閉じ、上昇トレンドなら成行買い1回でショートを閉じます。Hour 12 以外で新しいポジションは建てません。
- 独立した終了 Hour はありません。4本の分岐はすべてクールダウンが利用可能であることを要求し、真になった分岐だけが専用の Modify position ブロックを起動します。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Candles と Strategy trades が使用する銘柄です。取引では Strategy Security に同じ銘柄を設定します。 |
| Candle Series | 00:30:00 | 確定足の間隔であり、指標更新とクールダウンの進行単位です。 |
| Fast SMA Length | 8 | 短期単純移動平均に含める確定足の本数です。 |
| Slow SMA Length | 21 | 長期単純移動平均に含める確定足の本数です。 |
| Trade Hour | 12 | 予定エントリーで受け入れる確定足の `OpenTime.Hour` の値です。 |
| Cooldown N | 8 | 次のアクションを再検討できる最も早い後続確定足の番号です。 |
| Volume | 1 | 各成行買いまたは成行売りの固定数量です。 |

## ダイアグラムの詳細

- BTC の [Variable](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) は、構築された確定済み [Candles](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) と [Strategy trades](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) に銘柄を渡します。
- 形成済みの値だけを扱う2個の [Indicator](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) ブロックが SMA(8) と SMA(21) を計算します。Formula ブロックは数値を上昇用と下降用の [Comparison](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) ブロックに渡します。
- Time は保存済みポジション、予定 Hour、ゼロ基準、クールダウン状態、Hour Converter へ渡す時刻、固定数量の順に出力します。最後に保留中の判断ラッチを起動するため、5入力の各論理ゲートは同じ足の一貫したスナップショットを受け取ります。
- 1回限りの Flag は同期的な取引処理中の再入を防ぎます。4個の独立した [Modify position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックにより、偽の分岐が真の分岐用に準備した数量を消費することを防ぎます。
- 予定買いと予定売りの候補状態は `ポジション + 1` と `ポジション - 1` で、2つの決済候補はゼロです。候補は対応する Modify position の `MyTrade` 出力を通った場合だけポジション状態へ入ります。
- [Delay](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) ブロックは、同じ足の判断より先に各足を受け取ります。真のアクションゲートはクールダウンを使用不可にして N = 8 を開始してから注文を送信します。チャートには足、2本の平均、約定済みポジション、4本のアクション約定ストリーム、全戦略約定を表示します。

## 使用方法

`.json` ファイルを Designer にインポートし、Strategy Security を BTCUSDT@BNBFT に設定してポートフォリオを選び、30分足履歴で実行します。収録されている3月データと上記設定での検証では、取引エラーなしで59件の完了した成行注文と59件の約定が得られました。ライブ取引の前に、足のタイムゾーン、Hour フィールド、数量、クールダウン動作を確認してください。

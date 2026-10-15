# ランチタイム逆張り戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定済み5分足を使い、11:00:00～14:59:59のランチタイム枠で短期的な値動きに逆張りします。ポジションがゼロのときだけエントリーし、終値と形成済み20期間SMAの位置関係で決済し、注文シグナルの後は次の確定済み30本についてすべてのエントリーと決済経路を停止します。

![schema](schema.svg)

## 戦略の概要

- 確定済み5分足だけが判定チェーンに入ります。SMAは20期間のウォームアップ後に値を出力し、2つのPrevious valueブロックが直前2本の終値を提供します。
- Working timeブロックは各足の開始時刻を読み取り、11:00:00から14:59:59までを両端を含めてエントリー可能にします。この時間枠は決済を制限しません。
- 直前2本の終値が上昇し、現在足が陰線なら、ポジションがゼロの状態からショートします。直前2本の終値が下降し、現在足が陽線ならロングします。
- ロングは終値がSMAを下回ると決済し、ショートは終値がSMAを上回ると決済します。2つのエントリーと2つの決済に対応する4本の独立経路が、固定数量の成行注文を送信します。
- 各エントリーまたは決済シグナルはクールダウンを開始し、次の確定済み30本の間は両方の操作を停止します。ポジション保護ブロックはなく、チャートにはローソク足、SMA、4本の注文経路の約定が表示されます。

## エントリーとエグジットの条件

- **ロングエントリー**: ランチタイム枠で`Close[-1] < Close[-2]`、現在足が陽線（`Close > Open`）、ポジションのスナップショットがゼロ、かつクールダウン完了の場合、Volume 1の成行買い注文を送信します。
- **ショートエントリー**: ランチタイム枠で`Close[-1] > Close[-2]`、現在足が陰線（`Close < Open`）、ポジションのスナップショットがゼロ、かつクールダウン完了の場合、Volume 1の成行売り注文を送信します。
- **エグジット**: クールダウン完了時、ロングは`Close < SMA`で成行売り、ショートは`Close > SMA`で成行買いを送信します。これらの水準判定はランチタイム枠の内外で実行されます。ストップロス、テイクプロフィット、その他の保護は接続されていません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | 5分足の時間枠。インジケーター、履歴、クールダウン、判定は確定済み足だけで進みます。 |
| SMA Period | 20 | 2つの決済水準判定で使用するSimpleMovingAverageの期間です。 |
| Cooldown Bars | 30 | エントリーと決済のシグナルを停止する、後続の確定済み足の本数です。 |
| Lunch Begin | 11:00:00 | ランチタイムのエントリーが有効になる足の開始時刻の境界で、この時刻を含みます。 |
| Lunch End | 14:59:59 | ランチタイムのエントリーが有効な足の開始時刻の上限で、この時刻を含みます。 |
| Volume | 1 | 4つのNoCondition成行注文ブロックに渡す固定数量です。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み5分足を出力します。形成済み値だけを出力する[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)がSimpleMovingAverage 20を計算し、[数式](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html)が数値を提供します。[取引許可](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html)ゲートが保存された足を判定チェーンへ送ります。
- [コンバーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)ブロックがCloseとOpenを取り出します。2つの[前の値](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)ブロックはシフト1と2を使用し、履歴準備ゲートは直前2本の終値がそろうまで判定を止めます。
- [稼働時間](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)ブロックは足ストリームを直接受け取り、開始時刻のメタデータをランチタイム枠の包含境界と比較します。その結果は2つのエントリー条件だけに使われます。
- 現在の[ポジション](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)は[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)に保存され、判定足ごとに1回出力されます。[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)と[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)ブロックが、時間枠、直前の方向、現在足の方向、ポジション、履歴準備、SMA水準、クールダウン状態を組み合わせます。
- [シグナル遅延](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)ブロックが後続の確定済み30本を数えます。準備状態の変数はカウント中に4つの操作条件を抑止し、その次の足で再び有効にします。
- 4つの[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックが、`NoCondition`と共通のVolume 1で成行注文を出します。内訳はロングエントリー、ショートエントリー、ロングの売り決済、ショートの買い決済です。保護要素はありません。
- [チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、確定済み足、形成済みSMAストリーム、4つの注文ブロックそれぞれのMyTrade出力を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

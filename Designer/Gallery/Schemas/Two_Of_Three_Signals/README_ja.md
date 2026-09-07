# 3つのうち2つが一致する方向シグナル戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定済み30分足ごとに3つの方向票を評価します。MACD Signal線の傾き、Stochastic %Kのゾーン、RSIのゾーンです。任意の2票が一致すると、その足について1回の多数イベントが生成されます。クールダウンが準備完了なら、フラットなポジションから数量1を建て、反対側のポジションはReduceOnly決済の完全約定を確認してから新方向へエントリーして反転します。新しいポジションの各約定が10本の足のクールダウンを開始します。

![schema](schema.svg)

## 戦略の概要

- 確定済み30分足が、形成済みの値だけを出力するMACD 12/26/9、Stochastic 14/3、RSI 14の各インジケーターへ入ります。Previous valueブロックが1本前のMACD Signal値を保持し、その値が存在するまで履歴準備ゲートが判定を禁止します。
- ロング票は`MACD Signal > Previous MACD Signal`、`Stochastic %K ≤ 20`、`RSI < 40`です。ショート票は`MACD Signal < Previous MACD Signal`、`Stochastic %K ≥ 80`、`RSI > 60`です。等しいMACD値と方向ゾーン外のオシレーター値は中立です。
- 各方向について、3つのペア別Logical conditionブロックが2票多数の全組み合わせを表します。足ごとのFlagは最初に成立したペアだけを通すため、3つのインジケーターがすべて一致しても、方向イベントは3回ではなく1回だけ生成されます。
- ポジションとクールダウンのスナップショットが各多数イベントの経路を決めます。クールダウンが準備完了でポジションがフラットなら、Order Volume 1のNoCondition成行エントリーを1件送信します。反対側のポジションでは、まず数量1のReduceOnly成行決済を送信し、完全約定した決済Orderだけが新方向の数量1のNoCondition成行エントリーを開始します。
- Combinationブロックが4つの新規ポジションアクションの約定を1本のクールダウンストリームにまとめます。約定するとエントリーが無効になり、次の確定済み10本の足がスキップされ、11本目の確定足が次に判定可能な最初の足になります。独立した決済、ストップロス、テイクプロフィット、ポジション保護の各ブロックはありません。

## エントリーとエグジットの条件

- **ロングエントリー**: 任意の2つのロング票が一致し、クールダウンが準備完了なら、フラットなポジションからOrder Volume 1のNoCondition成行買いを送信します。ショートポジションの場合、ダイアグラムはまず数量1のReduceOnly成行買いを送信し、その決済Orderが完全約定した場合にだけ数量1のNoCondition成行買いを起動します。既存のロングポジションは変更しません。
- **ショートエントリー**: 任意の2つのショート票が一致し、クールダウンが準備完了なら、フラットなポジションからOrder Volume 1のNoCondition成行売りを送信します。ロングポジションの場合、ダイアグラムはまず数量1のReduceOnly成行売りを送信し、その決済Orderが完全約定した場合にだけ数量1のNoCondition成行売りを起動します。既存のショートポジションは変更しません。
- **エグジット**: 独立した決済ルールはありません。反対方向の多数イベントが段階的なシーケンスを通じて現在の側を決済し、その後に新しい側を建てます。ReduceOnly決済はエクスポージャーを増やしませんが、両方の注文は固定のOrder Volume 1を使用します。このシーケンスはダイアグラムが建てた1単位のポジション向けであり、実際の数量が異なる場合は完全に決済できないか、シグナル方向のポジションで完了しない可能性があります。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles Series | 00:30:00 | 30分足シリーズです。確定済み足だけがインジケーターを更新し、足ごとの多数Flagをリセットし、クールダウンを進め、判定を開始します。 |
| MACD Fast Length | 12 | MACD Indicatorブロック内で固定されています。高速EMA期間を変更するには、そのブロックを編集してください。 |
| MACD Slow Length | 26 | MACD Indicatorブロック内で固定されています。低速EMA期間を変更するには、そのブロックを編集してください。 |
| MACD Signal Length | 9 | MACD Indicatorブロック内で固定されています。Signal EMA期間を変更するには、そのブロックを編集してください。この線の1本分の傾きがMACD票になります。 |
| Stochastic K Length | 14 | Stochastic Indicatorブロック内で固定されています。%K期間を変更するには、そのブロックを編集してください。ロングとショートのしきい値は20と80です。 |
| Stochastic D Length | 3 | Stochastic Indicatorブロック内で固定されています。%D期間を変更するには、そのブロックを編集してください。方向票は%Kを読み取り、インジケーター全体が形成済みである必要があります。 |
| RSI Length | 14 | RSI期間です。固定の方向しきい値は40未満と60超です。 |
| Cooldown Bars | 10 | 新規ポジションの約定後にエントリーを禁止する、その後の確定済み足の本数です。11本目の足で判定を再開します。 |
| Order Volume | 1 | フラット時のエントリー、ReduceOnly決済アクション、決済の完全約定によって起動されるエントリーで使う固定数量です。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み30分足だけを出力します。形成済みの値だけを出力する3つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックがMACD 12/26/9、Stochastic 14/3、RSI 14を計算します。
- [コンバーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/converter.html)ブロックがMACD Signal線を取り出します。[Previous value](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)ブロックがその1本前の値を保持し、厳密な比較が現在のSignalを上昇、下降、変化なしに分類します。
- 追加の[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)ブロックが、オシレーターの正確な固定境界を実装します。Stochastic %Kは`≤ 20`と`≥ 80`、RSIは`< 40`と`> 60`を使用します。これらのしきい値と2票要件は公開パラメーターではなく、ダイアグラム内の固定設定です。
- 6つのペア別[Logical condition](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)ブロックが、ロングの3通りのペアとショートの3通りのペアを網羅します。2つの[Flag](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/flag.html)ブロックは足ごとにリセットされ、成立した複数のペアを各方向1回の多数イベントにまとめます。
- 現在の[Position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)とクールダウン準備状態が足のサイクル用に取得されます。ポジション比較はフラット、ロング、ショートのエクスポージャーを区別し、エントリー条件は多数イベントと利用可能なクールダウン状態の両方を要求します。
- 6つの[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックが、2つのフラット時エントリーと2つの段階的な反転を実装します。フラット経路は外部の`Position = 0`条件で保護され、反転の決済ブロックはReduceOnlyを使い、その完全約定したOrder出力が反対方向の固定NoConditionエントリーを起動します。
- [Combination](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/combination.html)ブロックは、4つの新規ポジションブロックのMyTrade出力を数えたり変更したりせず、直ちにまとめます。まとめられた約定がエントリーを無効にして[N values](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)ブロックを起動し、このブロックは次の確定済み10本の足を数えて11本目の足で準備状態を戻します。[チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、ローソク足、3本の数値シグナルストリーム、まとめられた新規ポジション約定、2つの反転決済約定を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

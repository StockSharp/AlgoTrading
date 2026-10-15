# 異なる時間枠の移動平均クロス戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定済み4時間足から計算する期間10の単純移動平均と、確定済み1時間足から計算する期間40の単純移動平均を組み合わせます。どちらの設定も名目上40時間の参照範囲を表します。形成済みの上位時間枠平均の最新値を保持し、確定した基準足ごとに現在の基準平均と組み合わせます。クロス方向と現在ポジションにより、約定確認後の2段階反転を含む固定数量0.1の成行アクションを振り分けます。チャートには1時間足、同期した2本の平均、4つの約定ストリームが表示されます。

![schema](schema.svg)

## 戦略の概要

- 確定済み4時間足はHigher SMA 10へ、確定済み1時間足はBase SMA 40へ入力され、1時間足が判定サイクルを動かします。既定の設定では、どちらも名目上40時間を対象にします。10 × 4時間と40 × 1時間です。
- 形成済みHigher SMAの最新値が保持されます。1時間足が確定するたびに、ダイアグラムは保持値と現在のBase SMAを更新し、1時間の間隔と基準足アンカーを持つSyncブロックが整列した値をまとめて出力します。
- 1つのCrossingブロックは、Higher SMAがBase SMAを上抜けたときに`true`、下抜けたときに`false`を出力します。NOTブロックは下向きイベントをショート経路用の正のトリガーに変換します。
- 現在ポジションにより、各クロスをフラット、ロング、ショートの3ケースに分けます。フラットならシグナル方向へ0.1を建て、すでに同方向なら何もせず、反対方向なら段階的な反転を開始します。
- 段階的な反転では、最初に数量0.1のReduceOnly成行アクションを送信します。決済Orderが完全に約定した場合だけ、新方向へ数量0.1の固定NoCondition成行アクションを起動します。この手順はダイアグラムが同じOrder Volumeで作ったポジション向けです。実際の数量が異なる場合、最終エクスポージャーは目標どおりにならないことがあります。ストップロスとテイクプロフィットのブロックはありません。

## エントリーとエグジットの条件

- **ロングエントリー**: Crossingが上向きイベントを出力したとき、外部でフラットを確認した経路がOrder Volume 0.1のNoCondition成行買いを送信します。ショートなら、まず数量0.1のReduceOnly成行買いを送信し、そのOrderが完全に約定した場合だけ2回目の数量0.1のNoCondition買いを起動します。既存のロングは変更しません。
- **ショートエントリー**: Crossingが下向きイベントを出力すると、NOTがショート経路を有効にします。外部でフラットを確認した経路がOrder Volume 0.1のNoCondition成行売りを送信します。ロングなら、まず数量0.1のReduceOnly成行売りを送信し、そのOrderが完全に約定した場合だけ2回目の数量0.1のNoCondition売りを起動します。既存のショートは変更しません。
- **エグジット**: 独立したエグジット、ストップロス、テイクプロフィットのルールはありません。反対方向への有効なクロスは固定数量の決済・新規建て手順を実行します。最初のReduceOnlyはエクスポージャーを増加または反転できませんが、次のNoConditionアクションは外部ポジションに合わせて数量を変えません。実際の数量がOrder Volumeと異なる場合、目標ポジションは保証されません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Higher Candles Series | 04:00:00 | Higher SMAで使用する4時間足シリーズです。確定済みの足だけが保持中の上位時間枠平均を更新します。 |
| Base Candles Series | 01:00:00 | Base SMAで使用する1時間足シリーズです。確定済みの各足が1回の同期評価のアンカーになり、チャートにも表示されます。 |
| Higher SMA Length | 10 | 4時間足で計算する単純移動平均の期間です。10本で名目上40時間の参照範囲になります。 |
| Higher SMA Source | unset | 未設定のため、Higher SMAは確定済み4時間足ごとのClose価格を読み取ります。 |
| Base SMA Length | 40 | 1時間足で計算する単純移動平均の期間です。40本で同じく名目上40時間の参照範囲になります。 |
| Base SMA Source | unset | 未設定のため、Base SMAは確定済み1時間足ごとのClose価格を読み取ります。 |
| Order Volume | 0.1 | フラット時のエントリー、ReduceOnly決済、約定確認後の反転第2段階で使用する固定数量です。 |

## ダイアグラムの詳細

- 2つの[ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは、確定済み4時間足と1時間足だけを出力します。形成済み値だけに限定した個別の[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックが、Close価格からSimpleMovingAverage 10とSimpleMovingAverage 40を計算します。
- [変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)ブロックは、形成済みHigher SMAの最新値を保持します。確定済み基準足ごとにその値とBase SMAを更新してから[Sync](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/sync.html)へ渡します。Intervalは`01:00:00`、ClearSocketsは有効で、ローソク足入力が時間ごとのアンカーになります。
- 同期した数値出力は1つの[クロス](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)ブロックへ入ります。上向きの`true`イベントはロング経路へ進み、NOTの[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)が下向きの`false`イベントを正のショートトリガーに変換します。
- 現在の[ポジション](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html)は同じ基準足処理サイクルで更新されます。[比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)ブロックが`Position = 0`、`Position > 0`、`Position < 0`を区別するため、同方向のポジションに追加エントリーは行いません。
- 上向きイベントでは、外部でフラットを確認した経路がNoCondition設定の買い[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックを呼び出します。ショート経路は最初にReduceOnly買いを呼び出し、決済が完全に約定した場合だけOrder出力が発生して、固定NoCondition買いを開始します。
- 下向き経路は対称です。外部でフラットを確認した経路はNoConditionでショートを建て、ロングなら売りで減らした後、そのOrderが完全に約定してから固定NoCondition売りを開始します。4つのアクションはすべてMarketOrderとOrder Volume 0.1を使用します。ストップロス、テイクプロフィット、時間指定エグジットのブロックはありません。
- [チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、確定済み1時間足、同期したHigher SMAとBase SMAの値、ロング開始、ショート開始、ショート決済、ロング決済の各アクションからMyTrade出力を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

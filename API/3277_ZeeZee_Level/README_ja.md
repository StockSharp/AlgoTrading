# ZeeZee Level戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

ZeeZee Level戦略は、StockSharp の高レベル API を使って、元の MetaTrader "ZeeZee Level" エキスパートアドバイザーの動作を再現します。この戦略は選択した時間枠の ZigZag スイングを分析し、直近の極値の方向に取引します。保護用の stop loss、take profit、trailing stop 距離は pips で表され、ポジションサイズは損失取引後にマーチンゲール型の進行に従います。

## 取引ロジック

1. `CandleType` で定義された時間枠を使ってローソク足を購読します。
2. 設定可能な depth、deviation、backstep パラメーターを持つ `ZigZagIndicator` が、スイング高値と安値を追跡します。
3. ポジションが開いていない場合、戦略は `ZigZagIdInterval` ウィンドウ内で最後に確認された ZigZag 高値と安値の新しさを比較します。
   - 最新のスイング高値が最後のスイング安値より新しい場合、ショートポジションを開きます。
   - 最新のスイング安値が最後のスイング高値より新しい場合、ロングポジションを開きます。
4. 一度に 1 つのポジションだけを維持します。エントリー数量は商品の数量ステップに丸められます。
5. ポジションが開かれると、設定された pip 距離を使って stop loss、take profit、任意の trailing stop 水準を付与します。trailing stop は、取引が有利に動くにつれて極値価格に追随します。
6. stop loss または take profit のいずれかに触れると、ポジションは直ちに決済されます。同じローソク足で両方の水準に到達した場合、エントリー価格に近い水準が優先されます。
7. 各エグジット後、利益取引では数量を初期値にリセットし、損失取引ではマーチンゲール係数を掛けます。

## パラメーター

| パラメーター | 説明 |
|-----------|------|
| `ZigZagDepth` | 新しい ZigZag ピボットを探すときに考慮するローソク足数。 |
| `ZigZagDeviation` | 新しいピボット確認に必要な最小価格変動 (価格ステップ単位)。 |
| `ZigZagBackstep` | インジケーターが方向を切り替えられるようになるまでの最小バー数。 |
| `ZigZagIdInterval` | 最後の ZigZag 高値と安値を探すために遡る最大バー数。 |
| `StopLossPips` | pips 単位の stop loss 距離。無効にするにはゼロに設定します。 |
| `TakeProfitPips` | pips 単位の take profit 距離。無効にするにはゼロに設定します。 |
| `TrailingStopPips` | pips 単位の trailing stop 距離。無効にするにはゼロに設定します。 |
| `InitialVolume` | マーチンゲールサイクル開始時に使用する基本取引数量。 |
| `MartingaleMultiplier` | 損失ポジション後の次回取引数量に適用される係数。 |
| `CandleType` | 分析に使用するローソク足タイプと時間枠。 |

## 資金管理

- 数量は商品の数量ステップに合わせられ、取引所の最小および最大制限の間に制約されます。
- 勝ち取引では数量を `InitialVolume` にリセットし、負け取引では `MartingaleMultiplier` を掛けます。

## リスク管理

- stop loss、take profit、trailing stop の距離は、確定した各ローソク足で評価されます。
- trailing stop は取引方向にのみ動き、後退しません。
- 戦略がすでにポジションを保有している間、または ZigZag スイングが設定間隔内で利用できない間は、取引をスキップします。

## 注意事項

- 戦略は、元のエキスパートアドバイザーの動作に合わせるため、確定済みローソク足だけを使用します。
- pip 変換は商品の `PriceStep` に依存します。戦略開始前に商品のメタデータが読み込まれていることを確認してください。

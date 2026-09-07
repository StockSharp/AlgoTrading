# RSI アラート戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、RSI の極端な値を取引と読みやすいアラートに変換します。確定済みの5分足を処理し、ポジションがない場合に限り30以下で買い、70以上で売り、約定した各エントリーにパーセント指定の保護を適用します。受け入れられた各シグナルは数値型の RSI 値も取り込み、整形して通知に書き込みます。

![schema](schema.svg)

## 戦略の概要

- 確定済みデータだけを出力する単一の5分足ストリームが、指標、ポジションのスナップショット、エントリー判断、保護価格の確認、およびチャートを駆動します。
- RelativeStrengthIndex の期間は14です。形成済みの値だけを出力するフィルターは無効です（`IsFormed = false`）。そのため、指標がまだ形成されていないことだけを理由にウォームアップ中の値が抑制されることはありません。
- 式が `a` の Formula ブロックは、RSI の IndicatorValue を比較とメッセージで使用する数値に変換します。
- 数値型の RSI を売られ過ぎ水準および買われ過ぎ水準と比較します。各方向シグナルは現在のローソク足の評価中に取得したポジションのスナップショットと結合され、両方のエントリーブロックで Open position 条件を使用します。
- エントリーが約定すると、2%のテイクプロフィットと1%のストップロスによるポジション保護が有効になります。受け入れられたエントリーシグナルは、取得した RSI 値をフォーマッター経由で Log タイプの通知にも渡します。

## エントリーとエグジットの条件

- **ロングエントリー**：数値型の RSI が Oversold Level 以下で、ポジションのスナップショットがポジションなしを示している場合です。ダイアグラムは設定数量を成行で買い、シグナル値を含む買いアラートを書き込みます。
- **ショートエントリー**：数値型の RSI が Overbought Level 以上で、ポジションのスナップショットがポジションなしを示している場合です。ダイアグラムは設定数量を成行で売り、シグナル値を含む売りアラートを書き込みます。
- **エグジット**：確定済みローソク足の終値がエントリー価格に対する2%のテイクプロフィット水準または1%のストップロス水準に達すると、ポジション保護が取引を決済します。反対方向の RSI シグナルは未決済ポジションを反転せず、保護注文の約定によって同じローソク足で再びエントリーすることもありません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| RSI Period | 14 | RelativeStrengthIndex の計算に使用するローソク足の本数です。 |
| Oversold Level | 30 | ダイアグラムがポジションを持っていない場合、この水準以下の RSI 値でロングエントリーが可能になります。 |
| Overbought Level | 70 | ダイアグラムがポジションを持っていない場合、この水準以上の RSI 値でショートエントリーが可能になります。 |
| Take Profit | 2% | エントリー価格からの保護用テイクプロフィットの距離です。 |
| Stop Loss | 1% | エントリー価格からの保護用ストップロスの距離です。 |
| Volume | 0.01 | エントリー注文の数量（ロット単位）です。 |
| Candles | 00:05:00 | 5分足の時間枠です。確定済みローソク足だけを処理します。 |

## ダイアグラムの詳細

- [Candles](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) の出力は、最初に現在のポジションのスナップショットをトリガーし、次に [Indicator](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) を更新し、最後に終値の [Converter](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) を更新します。
- RSI の出力は、式が `a` の [Formula](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html) ブロックに入ります。その数値出力は、いずれかのしきい値比較が評価される前に両方のメッセージ値ラッチへ到達します。
- 2つの [Comparison](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) ブロックは、`<=` と `>=` を使って数値型の RSI を共有の Oversold Level および Overbought Level の値と比較します。
- [Position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html) の値は、現在のローソク足を評価している間、[Variable](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) ブロックによって保持され、ゼロと比較されます。2つの [Logical condition](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) ブロックは、そのポジションなしの結果をロングおよびショートの RSI シグナルと結合します。
- 両方のエントリー用 [Modify position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックは、Open position 条件の成行注文を使用し、1つの共有数量値から `0.01` を受け取ります。
- 両方のエントリーブロックの MyTrade 出力は [Position protection](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) に入ります。終値コンバーターがその Price 入力に値を供給し、このブロックは2%のテイクプロフィットと1%のストップロスを使用します。
- 結合された各エントリーシグナルは、それぞれの RSI 値ラッチをトリガーします。[String format](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) は `RSI {0:0.0} <= 30 — buy` または `RSI {0:0.0} >= 70 — sell` を生成し、Log タイプの [Notification](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) ブロックがそのメッセージを公開します。
- Chart panel は、確定済みローソク足、RSI 値、2つのエントリー取引ストリーム、および保護によるエグジット取引を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行してください。ログで整形された RSI 通知を確認し、ローソク足の終値に照らして保護によるエグジットを検証します。いずれかの RSI しきい値を変更した場合は、アラート文が正確に保たれるよう、対応するフォーマッターテンプレートも更新してください。

# Early Bird Range Latch ストラテジーダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、価格が EMA 20 の方向と一致するとき、直前の5分足の高値または安値を厳密にブレイクした場面を取引します。UTC 日次ラッチは1日につき新規ポジションを最大1回だけ許可し、現在の ATR 14 がストップと目標の境界を定めます。

![schema](schema.svg)

## 戦略の概要

- 確定した5分足から現在の終値、直前の足の高値と安値、EMA 20、ATR 14を取得します。Previous value ブロックは High と Low のストリームだけを1本ずらすため、判定で同じ足自身の極値を比較することはありません。
- ロング条件は `Close > previous High` かつ `Close > EMA 20`、ショート条件は `Close < previous Low` かつ `Close < EMA 20` です。比較はすべて厳密で、等値はシグナルになりません。
- Time ブロックが 00:00:00 から 00:04:59 UTC までの固定日次リセット区間を駆動します。足の時刻が 00:05:00 から 23:59:59 までの固定エントリー区間を駆動し、共有 Flag は各リセット後の最初の有効な方向シグナルだけを通します。
- 採用されたシグナルは現在の終値をエントリー価格として保存し、ポジションのスナップショットがゼロのときだけ成行で1単位を建てます。決済後もラッチは消費済みのままで、次の UTC 日次リセットまで再エントリーを防ぎます。
- 以後の確定足ごとに、保存したエントリーと現在の ATR から4つの境界を再計算します。ロングのストップと目標は `entry − 1.5×ATR` と `entry + 2.5×ATR`、ショートでは符号が逆です。どちらかの境界に到達すると、対応する ReduceOnly 成行アクションが決済します。

## エントリーとエグジットの条件

- **ロングエントリー**: EMA 20 の形成後、00:05:00 から 23:59:59 UTC の間に、ポジションがゼロで `Close > previous High`、`Close > EMA 20`、日次 Flag が利用可能なら、1単位の OpenPosition 成行買いを送信します。
- **ショートエントリー**: EMA 20 の形成後、00:05:00 から 23:59:59 UTC の間に、ポジションがゼロで `Close < previous Low`、`Close < EMA 20`、日次 Flag が利用可能なら、1単位の OpenPosition 成行売りを送信します。
- **エグジット**: ロングでは `Close ≤ entry − 1.5×current ATR` または `Close ≥ entry + 2.5×current ATR` で ReduceOnly 成行売りを実行します。ショートでは `Close ≥ entry + 1.5×current ATR` または `Close ≤ entry − 2.5×current ATR` で ReduceOnly 成行買いを実行します。時刻決済、トレーリング、反転、同日中の再エントリーはありません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | すべてのシグナル計算とリスク計算を駆動する確定足の時間枠です。 |
| EMA Length | 20 | 方向フィルターとして使う、形成後のみ出力する指数移動平均の期間です。 |
| ATR Length | 14 | 確定足ごとに再計算し、形成後のみ出力する Average True Range の期間です。 |
| Stop ATR Multiplier | 1.5 | 保存したエントリー価格に対して不利側の境界を置くための現在 ATR の倍率です。 |
| Target ATR Multiplier | 2.5 | 保存したエントリー価格に対して有利側の境界を置くための現在 ATR の倍率です。 |
| Order Volume | 1 | 2つの OpenPosition エントリーと2つの ReduceOnly 決済に渡す固定数量です。 |

## ダイアグラムの詳細

- [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは確定した5分足を出力し、同梱の1分履歴から構築できます。
- 3つの [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) が Close、High、Low を取り出します。2つの [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) ブロックは数値の High と Low ストリームに Shift 1 を適用します。
- 形成後のみ出力する [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) ブロックが方向用の EMA 20 とリスク距離用の ATR 14 を計算します。EMA の準備条件により、両方の指標に十分なデータがたまる前のエントリーも防ぎます。
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/time.html) ストリームはリセット用の [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) ブロックに入ります。別の Working time ブロックは足の時刻を読み、2つのエントリー条件に直接参加します。
- 共有 [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) が UTC 日内の最初のロングまたはショート候補を消費します。Variable ブロックはポジションと採用されたエントリー終値を保存し、2つ目のエントリー価格変数がリスク式のために各足で保存値を再出力します。
- [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)、[Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)、[Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) ブロックがブレイク条件と4つの ATR 境界を構成します。足ごとの決済 Flag は、1回の評価中に複数入力が更新されても重複決済を防ぎます。
- 2つの OpenPosition と2つの ReduceOnly [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックが成行のエントリーと決済を処理します。[Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) には足、直前の High と Low、EMA、ATR、全約定をまとめた Combination ストリームが入ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

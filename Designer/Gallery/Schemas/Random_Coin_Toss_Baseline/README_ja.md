# ランダム・コイントス・ベースライン戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、意図的に市場シグナルを使わずに取引します。ポジションがない状態で確定済みの4時間足が到着するたびに、ランダムな値によってロングまたはショートのエントリーを選びます。ポジションはその後の確定済みローソク足10本にわたって保持され、成行で決済されます。次のローソク足から再び同じサイクルを開始できます。これはルールベースのシステムと比較するための教育用ベースラインであり、実運用向けの取引戦略ではありません。

![schema](schema.svg)

## 戦略の概要

- 確定済みデータのみを出力する単一の4時間足ストリームが、ランダムな判断と保有期間カウンターの両方を進めます。
- Random ブロックは0から1までの値を生成します。0.5のしきい値によって、この範囲を互いに重ならない2つの方向に分けます。
- 確定済みローソク足が到着するたびに現在のポジションをサンプリングし、ゼロと比較します。両方のエントリーブロックで Open position 条件も使用するため、そのローソク足の開始時点でポジションがない場合にだけ取引を開始できます。
- エントリーの約定によって N values ブロックが作動し、その後の確定済みローソク足を10本数えてからポジションの決済を許可します。
- このダイアグラムは、指標、ストップロス、テイクプロフィットを使用しません。ランダム系列のシードはダイアグラム内で設定されていないため、実行ごとに結果が変わる場合があります。

## エントリーとエグジットの条件

- **ロングエントリー**：ランダム値がコインのしきい値を下回り、ポジションがない場合です。ダイアグラムは設定された数量を成行で買います。
- **ショートエントリー**：ランダム値がコインのしきい値以上で、ポジションがない場合です。ダイアグラムは設定された数量を成行で売ります。
- **エグジット**：エントリーが約定すると、その後の確定済み4時間足を10本数えます。その後、N values ブロックがポジション全体の成行決済をトリガーします。決済したローソク足では新たな取引を開始せず、次の確定済みローソク足が再エントリーの最初の機会になります。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Hold Bars | 10 | エントリー約定後、ポジションを決済するまでに数える確定済みローソク足の本数です。ゼロより大きい値でなければなりません。 |
| Volume | 1 | エントリー注文と決済注文の数量（ロット単位）です。ポジションの開始と縮小に同じ設定数量を使用します。 |
| Coin Threshold | 0.5 | この水準を下回るランダム値はロング、この水準以上の値はショートのエントリーを選択します。 |
| Candles | 04:00:00 | エントリー判断と保有期間の計数に使用する4時間足です。確定済みローソク足のみを処理します。 |

## ダイアグラムの詳細

- [Candles](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) の出力は、[Random](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/random.html) ブロックに入り、ポジションのスナップショットをトリガーし、[N values](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) ブロックの Input ソケットと Chart panel にも接続されます。
- [Comparison](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) は、ランダム値がコインのしきい値以上かどうかを確認します。そのシグナルがショート分岐を選び、NOT モードの [Logical condition](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) がロング分岐を生成します。
- [Position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/current.html) の出力は、ローソク足の開始時点のポジションを保存するトリガー付き Variable ブロックに入ります。そのスナップショットを共有のゼロ定数と比較し、ポジションなしのシグナルを各エントリーの AND ブロックで方向シグナルと結合します。このスナップショットにより、決済の約定直後に同じローソク足で新しいポジションが開かれることを防ぎます。
- 両方のエントリー用 [Modify position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックは、Open position 条件の成行注文を使用し、1つの共有定数から数量を受け取ります。
- ロングおよびショートのエントリーブロックの MyTrade 出力は、N values ブロックの Trigger ソケットに接続されます。10本のローソク足を数えている間、それ以降のトリガーは無視されます。
- N values の出力は、Reduce only モードの2つの Modify position ブロックをトリガーします。売りブロックはロングだけを縮小し、買いブロックはショートだけを縮小できます。両方が共有数量を受け取るため、該当する分岐だけが決済注文を出します。
- [Chart panel](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html) は、ローソク足ストリームと2つのエントリーブロックおよび2つの決済ブロックが生成した約定を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行してから、同じ銘柄と期間についてルールベースのダイアグラムと結果を比較してください。この例は教育用の基準として使用し、実運用の取引システムとしては使用しないでください。

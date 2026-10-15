# 参照銘柄に対する相対力ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、売買対象銘柄と参照銘柄の比率から合成銘柄を組み立て、その比率が自身の60期間単純移動平均からどれだけ離れたかを測り、その結果に基づいて売買対象銘柄を取引します。比率が平均より1パーセント高ければ、売買対象銘柄が参照銘柄を上回っているとみなして買いに入り、1パーセント低ければ出遅れているとみなして売りに入ります。注文は常に売買対象銘柄に出され、合成銘柄は判断にのみ関与します。

![schema](schema.svg)

## 戦略の概要

- 銘柄インデックス(Security index)ブロックが、式 `BTCUSDT@BNBFT/TONUSDT@BNBFT` から合成銘柄を組み立てます。そのローソク足は一方の銘柄の価格をもう一方の単位で表したものなので、系列が上昇していれば売買対象銘柄が参照銘柄に対して優勢であることを意味します。
- この合成銘柄の確定した5分足は、終値を読み取るコンバーター(Converter)と、期間60の形成済みのみのSimpleMovingAverageに送られます。60本は同じ系列の5時間分にあたります。
- 数式(Formula)ブロックが比率の終値を平均で割って1を引き、相対力を小数として算出します。`+0.01` は比率が5時間平均より1パーセント上にあること、`-0.01` は1パーセント下にあることを意味します。
- 判断サイクルを駆動するのは、売買対象銘柄の確定した5分足という別の系列です。入力を保存専用として使う変数(Variable)ブロックが最新の相対力を保持し、売買対象のローソク足のタイミングで放出します。これにより、すべての比較とすべての注文が、合成銘柄側ではなく売買対象足のタイムスタンプを持ちます。
- 2つの比較(Comparison)ブロックが、放出された値をしきい値およびその符号反転値と照合します。符号反転値は数式(Formula)`0 - a` で作られ、公開された1つの数値が両サイドを対称に制御します。
- 現在のポジション(Position)はゼロと2回比較され、`Position <= 0` と `Position >= 0` が得られます。2つの論理条件(Logical condition)ブロックが各相対力シグナルを対応するポジション判定と結合するため、すでに建っているサイドに追加のエントリーが入ることはありません。
- ノーポジションの状態からは、Open position に設定されたポジション変更(Position modify)ブロックが、固定の Order Volume を成行で買うか売ります。ストップロスのブロックもテイクプロフィットのブロックもありません。
- 新しいシグナルと反対方向に建っているポジションは、Close position に設定されたポジション変更(Position modify)ブロックがまず手仕舞いします。ドテンは次に条件を満たすローソク足に委ねられます。

## エントリーとエグジットの条件

- **ロングエントリー**: 売買対象の確定足で、放出された相対力が Strength Threshold を上回り、かつポジションが買いでないとき、買いのゲートが成立します。ノーポジションからは Open position ブロックが Order Volume を成行で買います。売りポジションからは、先に手仕舞い動作が走るため、そのバーではエントリーが見送られます。買いは、引き続き優勢を示す次のローソク足で建ちます。
- **ショートエントリー**: 売買対象の確定足で、放出された相対力が Strength Threshold の符号反転値を下回り、かつポジションが売りでないとき、売りのゲートが成立します。ノーポジションからは Open position ブロックが Order Volume を成行で売ります。買いポジションからは、先に手仕舞い動作が走るため、そのバーではエントリーが見送られます。売りは、引き続き劣勢を示す次のローソク足で建ちます。
- **エグジット**: 独立した手仕舞いルールはなく、ストップロスもテイクプロフィットもありません。ポジションは反対方向のシグナルによってのみ解消されます。優勢は売りポジションを閉じ、劣勢は買いポジションを閉じます。手仕舞い動作は建っているポジションから数量を取るため、その後の口座はノーポジションとなり、シグナルが続いていれば次のローソク足で反対サイドが建ちます。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | 合成銘柄を組み立てる式です。最初の銘柄が分子、2番目が参照側の分母となるため、分子が参照に対して優勢になると系列は上昇します。売買対象銘柄を別の参照と比べたい場合はここを変更します。 |
| Ratio Candles | 00:05:00 | 合成銘柄のローソク足の時間軸です。放出される相対力は売買対象の1本につき1つの値なので、売買対象の系列と一致させる必要があります。 |
| Traded Candles | 00:05:00 | 売買対象銘柄のローソク足の時間軸です。すべての比較、すべてのエントリー、すべての手仕舞いは、この系列の確定足ごとに1回評価されます。 |
| Reference Average Length | 60 | 比率の単純移動平均の本数です。5分足60本は直近5時間の相対力を測ります。平均を長くするほど、より緩やかで稀にしか現れない乖離を測ることになり、取引回数は減ります。 |
| Strength Threshold | 0.01 | サイドを取る前に比率が平均からどれだけ離れていなければならないかを、小数で指定します。`0.01` は1パーセントで、買いエントリーでは平均より上に、売りエントリーでは平均より下に適用されます。下げれば取引頻度が増え、上げればより大きな乖離を要求します。 |
| Order Volume | 1 | 各エントリーの固定数量です。手仕舞い動作はこれを無視し、代わりに建っているポジションから数量を取ります。 |

## ダイアグラムの詳細

- [銘柄インデックス(Security index)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html)ブロックは合成銘柄を組み立てる式を保持し、1つの[ローソク足(Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックの Security 入力に供給します。もう1つのローソク足(Candles)ブロックは戦略の銘柄のままにされ、売買対象の系列を供給します。どちらも確定足のみに設定されているため、形成中のバーが自身のバーの始値時刻で注文の日時を決めてしまうことはありません。
- [コンバーター(Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html)が合成足の終値を読み取り、形成済みのみの[インジケーター(Indicator)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)が同じ系列を60本で平均します。[数式(Formula)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)`a / b - 1` がこの2つを符号付きの小数1つにまとめ、2つ目の数式(Formula)`0 - a` がしきい値を弱いサイドに反転させます。
- 合成足は2つのフィードから組み立てられるため、同じ分の通常のローソク足より後に完成します。そこで[変数(Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)が相対力を入力で保存し、売買対象足からのトリガーでのみ出力します。これによって注文のタイムスタンプが売買対象側の時計に保たれます。しきい値、ゼロ、注文数量の定数も同じローソク足からトリガーされるため、すべての[比較(Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)が1回の評価の中で両方のオペランドを受け取ります。
- [ポジション(Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)は売買対象の各ローソク足でゼロと比較され、2つの[論理条件(Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)ブロックが相対力の結果とポジションの結果を結合します。取引ブロックに届くのは `true` の結果だけで、`false` の比較はトリガー入力で破棄されます。
- 4つの[ポジション変更(Position modify)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックが成行注文で動作します。エントリー用の2つは Open position 条件を使うため、ノーポジションの口座からのみ動作し、サイドが建っている間は繰り返されません。手仕舞い用の2つは Close position 条件を使い、建っているポジションから数量を決め、口座がすでにノーポジションであるか、すでに要求されたサイドにある場合は何もしません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

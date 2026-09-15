# 時刻基準ローソク足ブレイクアウト戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、時刻を基準に毎日1本の基準ローソク足を選び、その高値と安値を記憶して、続く3本の確定ローソク足の間にその2つの水準のブレイクを売買します。EMA 20 のフィルターがブレイクのどちら側を売買できるかを決め、ウィンドウが満了するとポジションを決済し、10本のクールダウンによって、エントリーが成立するたびにしばらくの間ダイアグラムを市場から遠ざけます。

![schema](schema.svg)

## 戦略の概要

- 30分足のローソク足は、形成中の状態と確定した状態の両方が配信されます。確定値（Final value）ブロックがそのストリームを分けます。基準水準、ウィンドウのカウンター、クールダウンのカウンターは確定ローソク足だけを受け取り、ブレイクアウトの判定は形成中のローソク足の終値を読み取ります。
- 稼働時間（Working time）が基準ローソク足を指定します。すなわち、開始時刻が 02:30:00 から 02:59:59 の間にある確定ローソク足です。30分足では1日にちょうど1本だけが該当し、この30分の幅があるおかげで、1日1回のパルスを失うことなく時間足を変更できます。
- 変数（Variable）ブロックの2つの組が、そのローソク足から水準を取り出します。各組の1つ目の変数はすべての確定ローソク足の高値（または安値）を保持し、稼働時間のパルスが届いたときだけそれを出力します。2つ目は出力された値を保持し、ローソク足が更新されるたびに再送信するため、基準ローソク足と基準ローソク足の間も水準が配線上に流れ続けます。
- 同じパルスが、3に設定されたN個の値（N values）カウンターを起動します。このカウンターは確定ローソク足を数え、基準ローソク足の後の3本目が確定した時点で発火し、これが売買ウィンドウを終了させます。
- ウィンドウの状態は、組み合わせ（Combination）を通して3つのソースから書き込まれる1つの数値変数です。基準ローソク足が取られたときは1、3本のカウンターが発火したときは0、エントリーが成立した時点でも0になります。0との比較（Comparison）がこの数値を、両方のエントリー分岐が参照するゲートに変えるため、1つのウィンドウから生まれるポジションは最大でも1つです。
- 買いには、終値が基準高値より上かつ EMA 20 より上であることが必要です。売りには、終値が基準安値より下かつ EMA 20 より下であることが必要です。いずれの側も論理条件（Logical condition）の AND であり、ウィンドウが開いていること、クールダウンが経過していること、ポジションがないことも同時に要求します。
- 成立したエントリーは、ポジション変更（Modify position）ブロックの「ポジションを開く」条件を通じて成行注文を送ります。そのため、同じローソク足の中でシグナルが繰り返されても、最初の注文の上に2つ目の注文が積み上がることはありません。同じシグナルが、確定ローソク足10本を数える2つ目のN個の値カウンターを起動します。独自の組み合わせブロックで結ばれた2つ目の変数の組が、そのカウントが終わるまでクールダウンのフラグを0に保ち、その後1に戻します。
- 3本のカウンターが発火すると、「ポジションを閉じる」条件を持つ2つのポジション変更ブロックがそれを受け取ります。保有ポジションと反対側のブロックが成行でポジションを解消し、もう一方は閉じるものがないためシグナルを拒否します。

## エントリーとエグジットの条件

- **ロングエントリー**: 基準ローソク足に続く3本の確定ローソク足の間に、クールダウンが経過しポジションがない状態で、終値が基準高値より上かつ EMA 20 より上になると、「ポジションを開く」を通じて Order Volume の成行買い注文を送ります。
- **ショートエントリー**: 同じ3本のローソク足の間に、クールダウンが経過しポジションがない状態で、終値が基準安値より下かつ EMA 20 より下になると、「ポジションを開く」を通じて Order Volume の成行売り注文を送ります。
- **エグジット**: ポジションは価格ではなく時間で決済されます。3本のウィンドウカウンターが発火すると、「ポジションを閉じる」ブロックが保有中のポジションを成行で解消します。ストップも目標値もトレーリングの規則もないため、保有時間がウィンドウを超えることはありません。また、成立したエントリーはウィンドウに0を書き込むため、同じウィンドウで2回売買することはできません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:30:00 | ローソク足系列の時間足。形成中と確定の両方のローソク足が配信されますが、基準水準を定め、2つのカウンターを進めるのは確定ローソク足だけです。 |
| EMA Length | 20 | ブレイクのどちら側を売買できるかを決める指数移動平均の期間。値は移動平均が形成された後にのみ出力されるため、それまではエントリーできません。 |
| Reference From | 02:30:00 | 基準ローソク足を探す1日の時間帯の開始時刻。各確定ローソク足の開始時刻から判定されます。 |
| Reference Until | 02:59:59 | その時間帯の終了時刻。開始時刻と合わせて、1日にちょうど1本のローソク足の開始時刻だけを含む必要があります。既定の組み合わせは30分足1本分の幅です。 |
| Window Bars | 3 | 基準ローソク足の後に売買ウィンドウが続く確定ローソク足の本数。同じカウンターが満了時にポジションを決済します。 |
| Cooldown Bars | 10 | エントリーが成立した後、ダイアグラムが再び売買できるようになるまでに数える確定ローソク足の本数。 |
| Order Volume | 1 | 2つの「ポジションを開く」アクションが使う固定数量。「ポジションを閉じる」アクションは保有中のポジションをそのまま解消するため、数量を必要としません。 |

## ダイアグラムの詳細

- [ローソク足（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは形成中のローソク足も確定したローソク足も同じように配信し、それらを分けるのが [確定値（Final value）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/final_value.html) です。3つの [コンバーター（Converters）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) が、確定値の後ろで高値と安値を、その手前で終値を読み取ります。そのため水準は常に完成したローソク足から得られ、ブレイクはリアルタイムの価格に対して判定されます。
- [稼働時間（Working time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) は、入力された確定ローソク足の開始時刻を読み取るため、その出力は実時間の一定区間ではなく1日1本のローソク足に対して真になります。ダイアグラム上では順序が重要です。高値と安値のコンバーターはその手前に接続されているため、パルスが値を解放する時点で、保持側の [変数（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) はすでに現在のローソク足の値を保持しています。
- 2つの [N個の値（N values）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) カウンターはそれぞれシグナルによって起動され、確定ローソク足を数えます。売買ウィンドウ用は3本、クールダウン用は10本です。その出力は2つの [組み合わせ（Combination）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) ブロックで、開放用と抑止用の値と合流します。各組み合わせは状態を保持する変数を1つ駆動し、その変数がローソク足の更新ごとに数値を再送信します。
- [インジケーター（Indicator）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) ブロックが EMA 20 を供給します。7つの [比較（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) ブロックが、2つのブレイクアウト判定、2つのトレンド判定、ウィンドウのゲート、クールダウンのゲート、そしてローソク足ごとに保持される [ポジション（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) のスナップショットに対するポジションなしの確認を構成します。2つの [論理条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) の AND ブロックがそれらを2つのエントリー分岐にまとめ、OR ブロックがどちらの分岐もクールダウンを開始する単一のシグナルに変えます。
- 4つの [ポジション変更（Modify position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックが動作します。エントリー用に「ポジションを開く」が2つ、時間による決済用に「ポジションを閉じる」が2つです。すべての約定は組み合わせブロックに集められ、ローソク足、EMA 20、2つの基準水準、4つの注文ストリームとともに [チャートパネル（Chart panel）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) に描画されます。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

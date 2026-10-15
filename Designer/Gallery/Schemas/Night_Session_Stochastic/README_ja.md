# ナイトセッション・ストキャスティクス戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

4時間足のキャンドルに適用するストキャスティクス・オシレーターだが、動作が許されるのは夜間だけである。興味深いのはオシレーターではなく時刻の扱いのほうだ。ナイトセッションは夕方に始まって翌朝に終わるため、開始が終了よりも遅い時刻になり、1つの稼働時間 (Working time) ブロックではそのような区間を表現できない。そこでこのダイアグラムは、午前0時の前と後という2つの半分から夜を組み立て、他のどのブロックがそれを見るよりも先に1つの答えへとまとめている。

![schema](schema.svg)

## 戦略の概要

- 確定した4時間足のキャンドルがダイアグラム全体を駆動するため、すべての判断は終了したバーの上で行われ、形成途中のバーに反応する部分はどこにもない。
- %K を14バー、%D を3バーに設定したストキャスティクス・オシレーターがそのキャンドル上で動作し、コンバーター (Converter) がその値から %K ラインを取り出す。%D は描画されるだけで、売買の判断には使われない。
- 2つの稼働時間 (Working time) ブロックが各キャンドルの開始時刻を読み取る。一方は 21:00 から 23:59:59 を、もう一方は 00:00 から 06:00 をカバーする。どちらか片方だけでは夜にはならない。
- Exclusive or に設定した論理条件 (Logical condition) が、2つの半分を1つの夜間シグナルへ統合する。2つの半分は重なり得ないため、開いているのは常にどちらか一方だけであり、ブロックは両方の値を手にしたうえでキャンドルごとに1回だけ答えを返す。
- 2つの比較 (Comparison) ブロックが %K を売られ過ぎ水準と買われ過ぎ水準に照らし、さらに2つがポジションをゼロと照らして、ノーポジションか買い持ちか売り持ちかをダイアグラムに伝える。
- 4つの論理条件 (Logical condition) の And ゲートが、夜間・オシレーター・ポジションという3つの事実を組み合わせて2つのエントリーと2つのエグジットを作るため、時刻が同意しない限りどのゲートも発火できない。
- エントリーは Open position 条件のもとで動作するポジション変更 (Position modify) ブロックである。Order Volume の成行注文はポジションがちょうどゼロのときにしか出て行かず、これが1つのシグナルを注文の連射に変えないための仕組みになっている。
- チャートパネル (Chart panel) にはキャンドル、オシレーター、そしてダイアグラムが生み出すすべての注文と約定が描かれるため、夜間の時間帯を図からそのまま読み取れる。

## エントリーとエグジットの条件

- **ロングエントリー**: 夜が開いていて、ポジションがノーポジションで、%K が売られ過ぎ水準を下回っているあいだ、買いのゲートが発火し、ポジション変更 (Position modify) が Order Volume を成行で買う。このブロックに付いた Open position 条件により、同じ状態が繰り返し現れても、ポジションが再びクローズされるまでは何も変わらない。
- **ショートエントリー**: その鏡像である。夜が開いていて、ポジションがノーポジションで、%K が買われ過ぎ水準を上回ると売りのゲートが発火し、同じ Open position 条件のもとでポジション変更 (Position modify) が Order Volume を成行で売る。
- **エグジット**: テイクプロフィットもストップロスも、時刻によるクローズもない。買いポジションは反対側の極値、すなわちポジションが買い持ちのあいだに %K が買われ過ぎ水準を上回ったときにクローズされ、売りポジションはポジションが売り持ちのあいだに %K が売られ過ぎ水準を下回ったときにクローズされる。いずれも Close position に設定したポジション変更 (Position modify) ブロックを通り、建玉の数量はブロック自身が読み取る。エグジットも夜間の時間帯の内側にあるため、夜に建てたポジションは日中を持ち越し、次の夜に解消される。クローズとドテンは別々の出来事である。買いをクローズする極値はポジションをゼロに戻すだけであり、売りを建てるのは後から現れる同じ種類の状態である。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 04:00:00 | 4時間の時間軸で、データにあるより小さいキャンドルから組み立てられる。処理されるのは確定したキャンドルだけであり、その一本一本は開始時刻で時刻付けされる。それが属するセッションを決めるのはこの時刻である。 |
| %K Length | 14 | %K ラインのルックバック。オシレーターが終値を照らし合わせるキャンドルの本数。 |
| %D Length | 3 | %D ラインの平滑化期間。パネルに描画されるだけで、どの条件にも関与しない。 |
| Evening Half From | 21:00:00 | 午前0時より前にある夜の半分の開始時刻。2つの半分は互いに離しておくこと。午前0時で接するようになっており、重ねてはならない。 |
| Evening Half Until | 23:59:59 | 夕方側の半分の終了時刻。午前0時の1秒前で閉じることで、次に続く半分に触れずに済む。 |
| Morning Half From | 00:00:00 | 午前0時より後にある夜の半分の開始時刻、すなわち午前0時そのもの。 |
| Morning Half Until | 06:00:00 | 朝側の半分の終了時刻であり、同時に夜の終わりでもある。この時刻を過ぎると、夕方側の半分が再び開くまでダイアグラムのどのゲートも発火できない。 |
| Oversold Level | 30 | %K がこれを下回ると売られ過ぎとみなされる水準。ポジションがノーポジションなら買いを建て、売り持ちがあればそれをクローズする。 |
| Overbought Level | 70 | %K がこれを上回ると買われ過ぎとみなされる水準。ポジションがノーポジションなら売りを建て、買い持ちがあればそれをクローズする。 |
| Order Volume | 1 | 両方のエントリーが送る数量。エグジットはこれを無視し、建っているものをそのままクローズする。 |

## ダイアグラムの詳細

- [キャンドル (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは、確定済みキャンドルのみを有効にした4時間足のシリーズを購読し、送出する値のすべてにキャンドルが開いた時刻を刻む。時刻ブロックが読むのはこの刻印であり、そのためキャンドルは、その開始時刻が入るセッションに属する。続く4時間のあいだに何が起きても変わらない。
- [インジケーター (Indicator)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) がストキャスティクス・オシレーターを保持し、形成が完了して初めて値を先へ渡すため、ヒストリーの最初のバーはシグナルを出さずに計算を助走させる。[コンバーター (Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) がオシレーターの値から %K のフィールドを読み出し、比較ブロックへただの数値として渡す。
- 2つの[稼働時間 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) ブロックこそがこのダイアグラムの眼目である。各ブロックは、時刻が自身の2つの境界のあいだにあるあいだ真となる。つまり1つのブロックでは午前0時をまたぐ時間帯を決して表現できない。開始が終了より遅くなり、判定が成立しなくなるからだ。夜を午前0時で分ければごく普通の時間帯が2つできあがり、その上にある[論理条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) が夜を元どおりに組み立てる。
- [ポジション (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) は、3つの[比較 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) ブロックによってゼロの[変数 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) と同時に比較され — 等しい、より大きい、より小さい — その3つの答えが2つのエントリーゲートと2つのエグジットゲートを分ける。売られ過ぎ水準と買われ過ぎ水準、そして注文数量も変数であり、ダイアグラムが判断に用いる数値はどれもブロックの中に埋もれた値ではなくパラメーターになっている。
- 4つの[ポジション変更 (Position modify)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ブロックがゲートを受けて動く。2つのエントリーは Open position 条件を持ち、数量を出来高の変数から取る。2つのエグジットは Close position 条件を持ち、数量を必要としない。クローズの注文は、それが解消するポジションから数量が決まるからである。これらの注文と約定は、キャンドルおよびオシレーターとともに[チャートパネル (Chart panel)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) へ送られる。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

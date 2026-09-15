# Monday Weakness 戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、固定された週次カレンダーに従って売買します。週の売り側と買い側にはそれぞれ専用の曜日があります。ショート日には終値が SMA 20 を下回っていれば売り、カバー日にはそのショートを買い戻します。ロング日には終値が SMA 20 を上回っていれば買い、エグジット日にはそのロングを売り切ります。曜日はローソク足から直接、数値として読み取られるため、4 つのカレンダー規則は 4 つの単純な比較で済みます。さらに時計に基づくエントリー時間帯によって、週次の判断は 1 日のうち活発な時間帯の内側に収まります。

![schema](schema.svg)

## 戦略の概要

- 5 分足は確定したものだけが供給されます。2 つのコンバーター（Converter）が同じローソク足を読み取り、一方は終値を、もう一方は開始時刻の曜日を取り出します。曜日は日曜が 0、土曜が 6 となる数値で届きます。
- 4 つの変数（Variable）がカレンダー上の 4 つの曜日——ショート、カバー、ロング、エグジット——を保持し、Equal に設定された 4 つの比較（Comparison）ブロックが曜日番号を 4 つのシグナルに変換します。任意のローソク足で真になり得るのはそのうち 1 つだけであり、これによって 4 つの分岐が互いに競合することはありません。
- SMA 20 は同じローソク足に対して計算され、形成が完了するまで値を出力しません。そのため実行開始から最初の 20 本のローソク足ではシグナルはまったく発生しません。2 つの比較（Comparison）ブロックがこれを読み取り、一方は終値が移動平均を下回っている間、もう一方は上回っている間に真となります。
- ポジション（Position）ブロックと、0 を保持する変数（Variable）と、Equal に設定された比較（Comparison）が、ノーポジション判定を作ります。両方のエントリー分岐がこれを必要とするため、すでに建玉のある週にもう 1 つポジションを積み増すことはできません。
- 現在時刻（Current time）が稼働時間（Working time）に入力され、08:00:00 から 20:00:00 の間だけ真になります。両方のエントリー分岐はこの時間帯も必要とするため、流動性の薄い夜間のローソク足で週次ポジションが建てられることはありません。2 つのエグジットは意図的にこの時間帯の外に置かれています。建っているものは、シグナルが何時に現れようとも、所定の曜日に決済しなければならないからです。
- 2 つの論理条件（Logical condition）AND ブロックがエントリー条件をまとめます。ショート分岐にはショート日、SMA 20 を下回る終値、ノーポジション、開いている時間帯が必要です。ロング分岐にはロング日、SMA 20 を上回る終値、そして同じ 2 つのゲートが必要です。それぞれが Open position 条件のポジション変更（Modify position）ブロックを駆動するため、同じ日の中でシグナルが繰り返されても 2 本目の注文が送られることはありません。
- 2 つのエグジットは、Close position 条件と明示的な売買方向を持つポジション変更（Modify position）ブロックです。カバー日のブロックは買いなので、決済できるのはショートだけです。エグジット日のブロックは売りなので、決済できるのはロングだけです。どちらも数量の入力を持ちません。Close position は建っているポジションそのものから注文数量を決めるからです。
- チャートパネル（Chart panel）には、ローソク足の系列、SMA 20、4 つの動作すべての注文とその約定が描画されます。そのため、週初のエントリー、週央のカバー、週後半のエントリー、週末のエグジットという週次のリズムを図から直接読み取れます。

## エントリーとエグジットの条件

- **ロングエントリー**: ロング日に、エントリー時間帯の内側で、ノーポジションかつ終値が SMA 20 を上回っている場合、Open position 条件のポジション変更（Modify position）を通じて Order volume の成行買いが送信されます。
- **ショートエントリー**: ショート日に、エントリー時間帯の内側で、ノーポジションかつ終値が SMA 20 を下回っている場合、Open position 条件のポジション変更（Modify position）を通じて Order volume の成行売りが送信されます。
- **エグジット**: 決済は価格ではなくカレンダーによって行われます。カバー日には Close position 条件のポジション変更（Modify position）の買いが建っているショートを決済し、エグジット日には同じ条件のポジション変更の売りが建っているロングを決済します。ストップロスも利食い目標もトレーリング規則もないため、ポジションは自分のエグジット日が来るまで持ち越されます。2 つのエグジットシグナルはその曜日のローソク足ごとに繰り返され、その繰り返しを数えたり抑制したりするものは何もありません。最初のローソク足でポジションが決済され、それ以降は Close position に処理する対象がなく、シグナルは黙って拒否されます。エントリーについても同じで、1 日あたりのカウンターも取引間のクールダウンもありません。1 つのカレンダー日を 1 本の注文に留めているのは、Open position 条件とノーポジション判定の組み合わせです。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | ローソク足系列の時間足。確定した足のみが供給され、曜日、移動平均、すべてのシグナルは確定足から読み取られます。 |
| MA Period | 20 | 両方のエントリーをフィルターする単純移動平均の期間。移動平均は形成が完了して初めて値を出力するため、実行開始直後のローソク足ではエントリーできません。 |
| Session From | 08:00:00 | エントリーが許可される 1 日の時間帯の開始時刻で、ストラテジーの時計から読み取られます。エグジットはこの時間帯を無視します。 |
| Session Until | 20:00:00 | その時間帯の終了時刻。両者を広げればカレンダー規則は任意の時刻に働き、狭めればエントリーは 1 日のうち数時間に集中します。 |
| Short day | 1 | 終値が移動平均を下回っているときにショートを建てる曜日番号。曜日は日曜の 0 から土曜の 6 まで番号が付けられます。 |
| Cover day | 3 | 建っているショートを買い戻す曜日番号。決済するのはショートだけで、その日にロングは手を付けられません。 |
| Long day | 4 | 終値が移動平均を上回っているときにロングを建てる曜日番号。日曜を 0 とする同じ体系で番号が付けられます。 |
| Exit day | 5 | 建っているロングを売り切る曜日番号。決済するのはロングだけで、その日にショートは手を付けられません。 |
| Order volume | 1 | 2 つの Open position 動作が使う固定数量。2 つの Close position 動作は数量を必要としません。建っているポジションから注文数量を決めるためです。 |

## ダイアグラムの詳細

- [ローソク足（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは確定足のみに設定されており、これは 2 つの意味で重要です。曜日はもう変化しないローソク足から取られ、ダイアグラムが送信するすべての注文には、形成途中の足の開始時刻ではなく、確定した足の終了時刻が刻まれます。
- カレンダーも価格も、その系列に対する 1 組の[コンバーター（Converter）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)から得られます。曜日を別の時計ではなくローソク足から読み取ることで、カレンダー判定はトレンド判定とまったく同じ拍子に保たれ、[論理条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND ブロックが両者をまとめるとき、常に同じローソク足を表すことになります。
- 4 つの曜日番号は通常の[変数（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)であり、[比較（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)ブロックで比較されます。だからこそ週次の計画全体をパラメーター一覧から組み替えられます。ショート日を別の番号に移せば、リンクを 1 本も触らずにスキーマはその曜日で売買するようになります。
- [現在時刻（Current time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html)と[稼働時間（Working time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)は、時刻のゲートを単純なレベルとして供給します。時間帯の内側では常に真、外側では偽です。これは 2 つのエントリー分岐にだけ接続されており、その隣にある[ポジション（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)のスナップショットも同じやり方でノーポジション判定を供給します。
- 売買はすべて 4 つの[ポジション変更（Modify position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックが行います。2 つは Open position と固定数量、残る 2 つは Close position と明示された売買方向を持ちます。決済側のブロックに方向を与えていることが、カレンダーによる決済を正確にしています。カバー日の買いはロングを単に受け付けず、エグジット日の売りはショートを単に受け付けません。これらが発したものはすべて、ローソク足および SMA 20 とともに[チャートパネル（Chart panel）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html)に描画されます。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。

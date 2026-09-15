# トレードレポート通知の戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、新しいシグナルを考案する例ではなく、レポート出力の例です。5分足の確定ローソク足における期間9と期間26の指数移動平均のごく単純なクロスが売買を生み、その周囲にあるすべての要素が、その売買を読みやすいテキストに変換します。自己約定が発生するたびに、その瞬間にログが1行書き出され、1日に1度、時計で駆動される分岐が戦略の実現損益を書き出します。レポート側は売買側を読み取るだけで、自ら注文を出すことも、訂正することも、妨げることもありません。

![schema](schema.svg)

## 戦略の概要

- 確定した5分足のローソク足が、期間9の速いExponentialMovingAverageと期間26の遅いExponentialMovingAverageに供給されます。クロス (Crossing) ブロックはこの2本を単一のイベントに集約します。速い線が遅い線を上抜けたときは `true`、下抜けたときは `false`、その間は何も出力しません。
- ポジションは、ローソク足をトリガーとするスナップショットによって1本につき1回だけ読み取られ、ゼロとの3つの比較によってノーポジション・買い持ち・売り持ちのいずれかとして表されます。すべての判断はこのスナップショットから組み立てられるため、バーの途中で約定が届いても、すでに下された判断が開き直されることはありません。
- ノーポジションの状態からは、上抜けのクロスが買いポジションを建て、下抜けのクロスが売りポジションを建てます。どちらのエントリーブロックにも Open-position 条件が付いているため、何らかのポジションを保有している間は沈黙し、注文を注文の上に積み増すことはできません。
- ポジションを保有している状態では、反対方向のクロスが決済 (Close-position) ブロックを通じてそれを閉じ、注文数量はポジション自体から決まります。その決済の注文イベントが新しい方向へのエントリーをトリガーするため、ドテンは1つの過大な注文ではなく、明示的な2つのステップとして記述されます。
- 公開されているVolumeの値1つが、4つのエントリーブロックすべてに供給されます。2つの決済ブロックは数量を受け取りません。Close-position ブロックは、すでにどれだけ建っているかを把握しているからです。
- 戦略約定 (Strategy trades) ブロックは戦略自身のすべての約定を拾い上げ、文字列フォーマッタ (String formatter) を経由してログ通知 (Log notification) に送ります。これにより、各執行ごとに売買方向・数量・銘柄・価格を含む行が1行残ります。
- 2つ目の分岐は、市場ではなく時計に基づいて報告します。現在時刻 (Current time) が2つの稼働時間 (Working time) ウィンドウ、すなわち正午前後のレポートウィンドウと午前0時直後のリセットウィンドウに供給され、その間に置かれたフラグ (Flag) が、レポートウィンドウ全体をちょうど1日1回のパルスに変換します。
- この単一のパルスが、最後に与えられた値を保持する変数から戦略の実現損益を解放し、それを整形してログに書き出します。変数はゼロから始まるため、約定が1件も発生しなかった日でもステータス行は出力されます。

## エントリーとエグジットの条件

- **ロングエントリー**: ローソク足時点のスナップショットがノーポジションを示している状態で、速い指数移動平均が遅い指数移動平均を上抜けると、Volume の数量で成行買いを送信します。代わりに売りポジションを保有している場合は、同じクロスがまずそれを全量決済し、生じた決済注文がただちに買いエントリーをトリガーするため、同一のローソク足の中で方向が切り替わります。
- **ショートエントリー**: ローソク足時点のスナップショットがノーポジションを示している状態で、速い指数移動平均が遅い指数移動平均を下抜けると、Volume の数量で成行売りを送信します。代わりに買いポジションを保有している場合は、同じクロスがまずそれを全量決済し、生じた決済注文がただちに売りエントリーをトリガーします。
- **エグジット**: ストップ、テイクプロフィット、ポジション保護 (Position protection) のブロックはありません。ポジションは反対方向のクロスまで持ち越され、そこで数量をポジションから導出する Close-position ブロックによって完全に決済されます。レポート分岐は約定と損益を観察するだけで、注文の発注・訂正・取消は一切行わないため、通知をオフにしても売買の挙動は変わりません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles | 00:05:00 | ローソク足シリーズの時間足。インジケーターと、その上に築かれるすべての判断を駆動するのは確定したローソク足だけです。 |
| Fast EMA Length | 9 | 速いExponentialMovingAverageの期間。クロスを構成する2本のうち速い側です。 |
| Slow EMA Length | 26 | 遅いExponentialMovingAverageの期間。クロスを構成する2本のうち遅い側です。 |
| Volume | 1 | 4つのエントリーブロックに与えられる数量。2つの決済ブロックはこれを無視し、建っているポジションから数量を取ります。 |
| Report Window Begin | 12:00:00 | 日次レポートウィンドウの開始。その中に入った最初の時点でステータスレポートが発行されます。 |
| Report Window End | 12:05:00 | 日次レポートウィンドウの終了。時計が一度その中に入る程度の幅があれば十分です。幅がどれだけであっても、フラグがレポートを1行に保ちます。 |
| Day Reset Begin | 00:00:00 | フラグを解除し、翌日に新しいレポートを可能にするリセットウィンドウの開始。 |
| Day Reset End | 00:05:00 | リセットウィンドウの終了。この時点からレポートウィンドウの開始までの間、この分岐は沈黙します。 |
| Fill Report Caption | Trade report | 各約定通知に書き込まれるキャプション。これによってログの中で取引ごとの行を識別できます。 |
| Status Report Caption | Strategy status | 日次ステータス通知に書き込まれるキャプション。これが取引ごとの行と区別します。 |

## ダイアグラムの詳細

- [ローソク足 (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) ブロックは確定した5分足のみを出力し、2つの[インジケーター (Indicator)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックがその上で期間9と26のExponentialMovingAverageの値を計算します。確定値のみのフィルタリングは無効なので、両方の線はリプレイの開始時点から利用でき、どちらもチャートに描画されます。
- [クロス (Crossing)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)ブロックはクロス時にのみ発火します。NOT の[論理条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)が、その下抜けイベントを正のトリガーに変えます。現在の[ポジション (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)は、ローソク足1本につき1回解放される[変数 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)に格納され、3つの[比較 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)ブロックがそれをノーポジション・買い持ち・売り持ちのフラグに変え、4つのAND条件がそれらをクロスと組み合わせます。
- 6つの[ポジション変更 (Modify position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックが、これら4つの条件に基づいて動作します。条件 `OpenPosition` を持つノーポジションからのエントリーが2つ、条件 `ClosePosition` を持つ決済が2つ、そして対応する決済の注文イベントによってトリガーされるさらに2つの `OpenPosition` エントリーで、これがドテンを2段階で起こさせています。6つすべてが成行注文を出し、いずれもオンライン接続を待ちません。
- [戦略約定 (Strategy trades)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html)は自己約定をすべて出力します。[文字列フォーマッタ (String Formatter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)がテンプレート `Fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}` でそれを整形し、種類 `Log` の[通知 (Notification)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)が、約定レポートのキャプションの下にそれを書き出します。
- [現在時刻 (Current time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html)は2つの[稼働時間 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)チェックに供給されます。レポートウィンドウが[フラグ (Flag)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html)を立て、リセットウィンドウがそれを解除します。これがこの分岐を1日1パルスに制限しています。このパルスが、ゼロに初期化された変数から[損益 (P&L)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html)ブロックの実現値を解放し、2つ目の文字列フォーマッタが `Daily status: realized result {0}` を書き出し、2つ目の `Log` 通知がそれを公開します。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
